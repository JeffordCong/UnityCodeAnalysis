Shader "GPUAnimShader/Lit/GPUAnimation-Lit"
{
    Properties
    {
        [NoScaleOffset]_MainTex ("Texture", 2D) = "white" {}
        [MainColor] _Color ("Color", Color) = (1, 1, 1, 1)
     
         _BumpScale("Bump Scale", Range(-1, 1)) = 1.0
        [NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}

        _Glossiness("_Glossiness", Range(0.0, 1.0)) = 0.5
        _Specular("Specular Color", Color) = (1, 1, 1, 1)
        // Rim
        _RimColor("Rim Color (A)Opacity", Color) = (1,1,1,1)
        _RimTime("Rim Time", Range(0, 1)) = 0

        [Header(GPU Animation)]
        [NoScaleOffset]_AnimTex("Animation Texture",2D) = "black"{}
        _AnimFrameBegin("Begin Frame",int)=0
        _AnimFrameEnd("End Frame",int)=0
        _AnimFrameInterpolate("Frame Interpolate",Range(0,1))=0
        _BoundsCenter ("Bounds Center", Vector) = (0,0,0,0)
        _BoundsRange("Bounds Range", Vector) = (1,1,1,1)
        _VertexCount("Vertex Count",int)=0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" "DisableBatching"="True" } //禁止合批，避免无法修改顶点

        CGINCLUDE
            
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "GPUAnimationIncludeCommon-CG.cginc"
    #ifdef _RGBM
        #include "GPUAnimationIncludeRGBM-CG.cginc"
    #else
        #include "GPUAnimationIncludeRGBHalf-CG.cginc"
    #endif

        #pragma multi_compile_fwdbase
        #pragma multi_compile_instancing
        #pragma shader_feature_local_vertex _ANIM_BONE _ANIM_VERTEX
		#pragma shader_feature_local_vertex _ALING_POW2
        #pragma shader_feature_local_vertex _RGBM _RGBAHALF
        #pragma shader_feature_local_vertex _SKIN_BONE_2 _SKIN_BONE_4

        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_ON
            //如果同一批次渲染中有的实例需要过渡，有的不需要，会导致批次分裂（DrawCall增加）
            //#pragma shader_feature_local_fragment ENABLE_RIM

       

            #include "AutoLight.cginc"


            fixed _BumpScale;
            fixed _Glossiness;
            fixed4 _Specular;
            fixed4 _Color;

            sampler2D _MainTex;
            sampler2D _NormalMap;
            float4 _MainTex_ST;
            float4 _NormalMap_ST;

            struct a2v
            {
                float3 vertex : POSITION;
                float3 normalOS:NORMAL;
                float4 tangentOS:TANGENT;
                float2 uv : TEXCOORD0;

			#if defined(_ANIM_VERTEX)
                uint vertexID:SV_VertexID;
            #elif defined(_ANIM_BONE)
                float4 transformIndexes:TEXCOORD1;
                float4 transformWeights:TEXCOORD2;
            #endif

            
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                
                float3 positionWS                  : TEXCOORD1;    // xyz: posWS

            #ifdef _NORMALMAP
                half3 normalWS                 : TEXCOORD2;    // xyz: normal, w: viewDir.x
                half3 tangentWS                : TEXCOORD3;    // xyz: tangent, w: viewDir.y
                half3 bitangentWS              : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
            #else
                half3  normalWS                : TEXCOORD2;
            #endif
                SHADOW_COORDS(5)

                UNITY_VERTEX_INPUT_INSTANCE_ID // use this to access instanced properties in the fragment shader.
            };



            v2f vert(a2v v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                #if defined(_ANIM_BONE)
                    SampleTransform(v.transformIndexes,v.transformWeights, v.vertex, v.normalOS);
                #elif defined(_ANIM_VERTEX)
                    SampleVertexAndNormal(v.vertexID,v.vertex,v.normalOS);
                #endif
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
               	o.uv.xy = v.uv;
    
                // 计算世界空间法线、切线和副切线
                o.normalWS = UnityObjectToWorldNormal(v.normalOS);
            #ifdef _NORMALMAP
                o.tangentWS = UnityObjectToWorldDir(v.tangentOS.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangentOS.w;
            #endif
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                half4 maincolor = _Color;
                // 采样主纹理
                half4 baseColor = tex2D(_MainTex, i.uv);

#ifdef _NORMALMAP
               // 构建TBN矩阵
                float3x3 TBN = float3x3(
                    normalize(i.tangentWS),
                    normalize(i.bitangentWS),
                    normalize(i.normalWS)
                );
                // 采样并解码法线贴图
                half4 normalMap = tex2D(_NormalMap, i.uv);
                half3 normalTS = UnpackNormal(normalMap);
                normalTS.xy *= _BumpScale;
                half3 normalWS = mul(normalTS, TBN);
#else
                half3 normalWS =normalize(i.normalWS);
 #endif
                // 获取光照方向
                half3 lightDir = _WorldSpaceLightPos0.xyz;
                if (_WorldSpaceLightPos0.w != 0) // 点光源
                    lightDir = normalize(_WorldSpaceLightPos0.xyz - i.positionWS);

                // 计算漫反射
                half ndotl = saturate(dot(normalWS, lightDir));
                //half phone
                //ndotl = (0.5*dot(normalWS,lightDir)+0.5);
                half3 diffuse = _LightColor0.rgb * baseColor.rgb * ndotl;

                #if _SPECULARHIGHLIGHTS_ON
                    // 计算高光 (Blinn-Phong模型)
                    half3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                    half3 halfDir = normalize(lightDir + viewDir);
                    half ndoth = saturate(dot(normalWS, halfDir));
                    half specularPower = exp2(10 * _Glossiness + 1); // 转换为类似Unity的光泽度
                    half specularTerm = pow(ndoth, specularPower);
                    half3 specular = _LightColor0.rgb * _Specular.rgb  * specularTerm ;
                #else
                    half3 specular = half3(0,0,0);
                #endif



                 // 添加环境光和顶点颜色
                half3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * baseColor.rgb * maincolor.rgb;
                //统一管理光照衰减和阴影
                UNITY_LIGHT_ATTENUATION(atten,i,i.positionWS);
                // 最终颜色
                half4 finalColor =half4( ambient + (diffuse + specular)*atten,maincolor.a);

            
                ApplyRim(finalColor);
            
                
                return finalColor;
            }
            ENDCG
        }

        Pass
        {
            NAME "SHADOWCASTER"
            Tags{"LightMode"="ShadowCaster"}
            CGPROGRAM
		    #pragma vertex ShadowVertex
			#pragma fragment ShadowFragment
            #pragma multi_compile_shadowcaster

            struct a2fs
            {
          		float3 vertex:POSITION; 
                float3 normal:NORMAL;
            #if defined(_ANIM_VERTEX)
                uint vertexID:SV_VertexID;
            #elif defined(_ANIM_BONE)
                float4 transformIndexes:TEXCOORD1;
                float4 transformWeights:TEXCOORD2;
            #endif
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

			struct v2fs
			{
                V2F_SHADOW_CASTER;
			};


			v2fs ShadowVertex(a2fs  v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2fs o;


            	#if defined(_ANIM_BONE)
                SampleTransform(v.transformIndexes,v.transformWeights, v.vertex, v.normal);
				#elif defined(_ANIM_VERTEX)
            	SampleVertexAndNormal(v.vertexID,v.vertex,v.normal);
            	#endif

                 TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 ShadowFragment(v2fs i) :SV_TARGET
			{
               SHADOW_CASTER_FRAGMENT(i)
			}

            ENDCG
        }
        
    }

    CustomEditor "GPUAnimationLitShaderGUI"
}

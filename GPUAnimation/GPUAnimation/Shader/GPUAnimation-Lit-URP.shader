Shader "GPUAnimShader/Lit/GPUAnimation-Lit-URP"
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
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalRenderPipeline" "DisableBatching"="True" } //禁止合批，避免无法修复顶点

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

        #include "GPUAnimationIncludeCommon-HLSL.hlsl"
    #ifdef _RGBM
        #include "GPUAnimationIncludeRGBM-HLSL.hlsl"
    #else
        #include "GPUAnimationIncludeRGBHalf-HLSL.hlsl"
    #endif

        #pragma multi_compile_instancing
        #pragma shader_feature_local_vertex _ANIM_BONE _ANIM_VERTEX
		#pragma shader_feature_local_vertex _ALING_POW2
        #pragma shader_feature_local_vertex _RGBM _RGBAHALF
        #pragma shader_feature_local_vertex  _SKIN_BONE_2 _SKIN_BONE_4

        ENDHLSL
        Pass
        {
            Tags {"LightMode"="UniversalForward" } //禁止合批，避免无法修复顶点

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_ON

            //接收阴影 URP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            // Defines
          //  #define BUMP_SCALE_NOT_SUPPORTED 1

//             CBUFFER：存放所有实例共享的全局属性（如纹理、光照参数）。
// INSTANCING_BUFFER：仅存放需要差异化的实例属性（如颜色、缩放）。
            //全局常量缓冲区，所有实例共享同一内存区域
            //可以开启SRB兼容优化
          // CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _NormalMap_ST;
            float4 _Color;
            float4 _Specular;

            half _BumpScale;
            half _Glossiness;
          
        //   CBUFFER_END

            TEXTURE2D( _MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D( _NormalMap);
            SAMPLER(sampler_NormalMap);
 
            struct a2v
            {
                float3 positionOS : POSITION;
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
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                
                float3 positionWS              : TEXCOORD1;    // xyz: posWS

            #ifdef _NORMALMAP
                half3 normalWS                 : TEXCOORD2;    // xyz: normal, w: viewDir.x
                half3 tangentWS                : TEXCOORD3;    // xyz: tangent, w: viewDir.y
                half3 bitangentWS              : TEXCOORD4;    // xyz: bitangent, w: viewDir.z              
                float4 shadowCoord             : TEXCOORD5; // 接收的阴影坐标 URP
            #else
                half3  normalWS                : TEXCOORD2;
                float4 shadowCoord             : TEXCOORD3; // 接收的阴影坐标 URP
            #endif

                UNITY_VERTEX_INPUT_INSTANCE_ID // use this to access instanced properties in the fragment shader.
            };


            half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
            {
            #ifdef _NORMALMAP
                half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
                #if BUMP_SCALE_NOT_SUPPORTED
                    return UnpackNormal(n);
                #else
                    return UnpackNormalScale(n, scale);
                #endif
            #else
                return half3(0.0h, 0.0h, 1.0h);
            #endif
            }


            v2f vert(a2v v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
          	#if defined(_ANIM_BONE)
                SampleTransform(v.transformIndexes,v.transformWeights, v.positionOS, v.normalOS);
			#elif defined(_ANIM_VERTEX)
            	SampleVertexAndNormal(v.vertexID,v.positionOS,v.normalOS);
            #endif
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, v.tangentOS);
               
                o.positionWS.xyz = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformObjectToHClip(v.positionOS);
               	o.uv.xy = v.uv;
       
                // 计算世界空间法线、切线和副切线
                o.normalWS = normalInput.normalWS;
            #ifdef _NORMALMAP
                o.tangentWS = normalInput.tangentWS;
                o.bitangentWS = normalInput.bitangentWS;
            #endif
                o.shadowCoord = TransformWorldToShadowCoord(o.positionWS);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 albedo = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv).rgb * _Color.rgb;

                Light light = GetMainLight(i.shadowCoord);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS);

#ifdef _NORMALMAP
               // 构建TBN矩阵
                float3x3 TBN = float3x3(
                    normalize(i.tangentWS),
                    normalize(i.bitangentWS),
                    normalize(i.normalWS)
                );
                // 采样并解码法线贴图
                half4 normalMap = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap,  i.uv);
                half3 normalTS = UnpackNormal(normalMap);
                normalTS.xy *= _BumpScale;
                half3 normalWS = mul(normalTS, TBN);
#else
                half3 normalWS =normalize(i.normalWS);
 #endif

                //接受阴影，光照衰减
                half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                half3 lightDiffuseColor = LightingLambert(attenuatedLightColor, light.direction, normalWS);
                half3 lightSpecularColor = half3(0,0,0);
            #if _SPECULARHIGHLIGHTS_ON
                half smoothness = exp2(10 * _Glossiness + 1);
                lightSpecularColor += LightingSpecular(lightDiffuseColor, light.direction, normalize(normalWS), normalize(viewDirWS), _Specular, smoothness);
            #endif

                // 最终颜色
                half4 finalColor =half4( lightDiffuseColor * albedo+  lightSpecularColor,_Color.a);
            
                ApplyRim(finalColor);
            
                
                return finalColor;
            }
            ENDHLSL
        }

        Pass
        {
            NAME "SHADOWCASTER"
            Tags{"LightMode"="ShadowCaster"}
            HLSLPROGRAM
            
		    #pragma vertex ShadowVertex
			#pragma fragment ShadowFragment

            struct a2fs
            {
          		float3 positionOS:POSITION; 
                float3 normalOS:NORMAL;
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
				float4 positionCS:SV_POSITION;
			};

            //Caster

            #define SHADOW_CASTER_VERTEX(v,positionWS) o.positionCS= ShadowCasterCS(positionWS,TransformObjectToWorldNormal(v.normalOS))
            float3 _LightDirection;
            float4 ShadowCasterCS(float3 positionWS, float3 normalWS)
            {
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

			v2fs ShadowVertex(a2fs v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2fs o;
            	#if defined(_ANIM_BONE)
                SampleTransform(v.transformIndexes,v.transformWeights, v.positionOS, v.normalOS);
				#elif defined(_ANIM_VERTEX)
            	SampleVertexAndNormal(v.vertexID,v.positionOS,v.normalOS);
            	#endif
                SHADOW_CASTER_VERTEX(v,TransformObjectToWorld(v.positionOS));
                
				return o;
			}
			float4 ShadowFragment(v2fs i) :SV_TARGET
			{
               return 1;
			}

            ENDHLSL
        }
        

    }
    CustomEditor "GPUAnimationLitShaderGUI"
}

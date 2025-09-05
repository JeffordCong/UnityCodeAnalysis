Shader "GPUAnimShader/Unlit/GPUAnimation-Unlit"
{
   Properties
    {
        [NoScaleOffset]_MainTex ("Texture", 2D) = "white" {}
        [MainColor] _Color ("Color", Color) = (1, 1, 1, 1)
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
        Tags { "RenderType"="Opaque" "DisableBatching"="True" } //禁止合批，避免无法修改顶点

        CGINCLUDE
            
        #include "UnityCG.cginc"
        #include "GPUAnimationIncludeCommon-CG.cginc"
    #ifdef _RGBM
        #include "GPUAnimationIncludeRGBM-CG.cginc"
    #else
        #include "GPUAnimationIncludeRGBHalf-CG.cginc"
    #endif
        #pragma multi_compile_instancing
        #pragma shader_feature_local_vertex _ANIM_BONE _ANIM_VERTEX
		#pragma shader_feature_local_vertex _ALING_POW2
        #pragma shader_feature_local_vertex _RGBM _RGBAHALF
        #pragma shader_feature_local_vertex  _SKIN_BONE_2 _SKIN_BONE_4
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
           // #pragma shader_feature_local _NORMALMAP

            fixed4 _Color;

            sampler2D _MainTex;
           // sampler2D _NormalMap;
            float4 _MainTex_ST;
          //  float4 _NormalMap_ST;

            struct a2v
            {
                float3 vertex : POSITION;
                float3 normalOS:NORMAL;
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
                
                // float3 positionWS                  : TEXCOORD1;    // xyz: posWS
                // half3  normalWS                : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID // use this to access instanced properties in the fragment shader.
            };



            v2f vert(a2v v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                //GPU动画
          	#if defined(_ANIM_BONE)
                SampleTransform(v.transformIndexes,v.transformWeights, v.vertex, v.normalOS);
			#elif defined(_ANIM_VERTEX)
            	SampleVertex(v.vertexID,v.vertex);
            #endif

                // o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
               	o.uv.xy = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                half4 maincolor = _Color;
                // 采样主纹理
                half4 baseColor = tex2D(_MainTex, i.uv);

                // 最终颜色
                half4 finalColor = baseColor*maincolor ;
       
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
            	SampleVertex(v.vertexID,v.vertex);
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
  CustomEditor "GPUAnimationUnlitShaderGUI"
}

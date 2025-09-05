Shader "GPUAnimShader/Unlit/GPUAnimation-Unlit-URP"
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
        Tags { "RenderType"="Opaque" "DisableBatching"="True" } //禁止合批，避免无法修复顶点

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
        #pragma shader_feature_local_vertex _SKIN_BONE_2 _SKIN_BONE_4

        ENDHLSL
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            

            struct a2v
            {
                float3 positionOS : POSITION;
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
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID // use this to access instanced properties in the fragment shader.
            };

            half4 _Color;

            TEXTURE2D( _MainTex);
            SAMPLER(sampler_MainTex);
           

            v2f vert(a2v v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

          	    #if defined(_ANIM_BONE)
                SampleTransform(v.transformIndexes,v.transformWeights, v.positionOS, v.normalOS);
				#elif defined(_ANIM_VERTEX)
            	SampleVertex(v.vertexID,v.positionOS);
            	#endif

                o.positionCS = TransformObjectToHClip(v.positionOS);
               	o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv);
                // 最终颜色
                half4 finalColor = baseColor*_Color ;
           
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
            	SampleVertex(v.vertexID,v.positionOS);
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
    CustomEditor "GPUAnimationUnlitShaderGUI"
}

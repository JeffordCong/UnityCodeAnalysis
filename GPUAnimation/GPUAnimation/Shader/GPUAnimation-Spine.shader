Shader "GPUAnimShader/Spine/Skeleton" {
	Properties {
		_Cutoff ("Shadow alpha cutoff", Range(0,1)) = 0.1
		[NoScaleOffset] _MainTex ("Main Texture", 2D) = "black" {}
		[Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput("Straight Alpha Texture", Int) = 0
		[HideInInspector] _StencilRef("Stencil Reference", Float) = 1.0
		[HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Float) = 8 // Set to Always as default

		// Outline properties are drawn via custom editor.
		[HideInInspector] _OutlineWidth("Outline Width", Range(0,8)) = 3.0
		[HideInInspector][MaterialToggle(_USE_SCREENSPACE_OUTLINE_WIDTH)] _UseScreenSpaceOutlineWidth("Width in Screen Space", Float) = 0
		[HideInInspector] _OutlineColor("Outline Color", Color) = (1,1,0,1)
		[HideInInspector][MaterialToggle(_OUTLINE_FILL_INSIDE)]_Fill("Fill", Float) = 0
		[HideInInspector] _OutlineReferenceTexWidth("Reference Texture Width", Int) = 1024
		[HideInInspector] _ThresholdEnd("Outline Threshold", Range(0,1)) = 0.25
		[HideInInspector] _OutlineSmoothness("Outline Smoothness", Range(0,1)) = 1.0
		[HideInInspector][MaterialToggle(_USE8NEIGHBOURHOOD_ON)] _Use8Neighbourhood("Sample 8 Neighbours", Float) = 1
		[HideInInspector] _OutlineOpaqueAlpha("Opaque Alpha", Range(0,1)) = 1.0
		[HideInInspector] _OutlineMipLevel("Outline Mip Level", Range(0,3)) = 0


		[Header(Rim Color)]
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

	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "DisableBatching"="True" "PreviewType"="Plane" }

		Fog { Mode Off }
		Cull Off
		ZWrite Off
		Blend One OneMinusSrcAlpha
		Lighting Off

		Stencil {
			Ref[_StencilRef]
			Comp[_StencilComp]
			Pass Keep
		}

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
		 #pragma shader_feature_local_vertex _RGBM
         #pragma shader_feature_local_vertex _SKIN_BONE_2 _SKIN_BONE_4

        ENDCG

		Pass {
			Name "Normal"

			CGPROGRAM
			#pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
		

			#pragma vertex vert
			#pragma fragment frag

			#include "Spine-Common.cginc"
			sampler2D _MainTex;

			struct VertexInput {
				float3 vertex : POSITION;
				float3 normalOS:NORMAL;
				float2 uv : TEXCOORD0;
				float4 vertexColor : COLOR;

							
			#if defined(_ANIM_VERTEX)
                uint vertexID:SV_VertexID;
            #elif defined(_ANIM_BONE)
                float4 transformIndexes:TEXCOORD1;
                float4 transformWeights:TEXCOORD2;
            #endif

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 vertexColor : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID // use this to access instanced properties in the fragment shader.
			};


			VertexOutput vert (VertexInput v) {
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

				//GPU动画
          	#if defined(_ANIM_BONE)
                SampleTransform(v.transformIndexes,v.transformWeights, v.vertex);
			#elif defined(_ANIM_VERTEX)
            	SampleVertex(v.vertexID,v.vertex);
            #endif

				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.vertexColor = PMAGammaToTargetSpace(v.vertexColor);
				return o;
			}

			float4 frag (VertexOutput i) : SV_Target {

				UNITY_SETUP_INSTANCE_ID(i);

				float4 texColor = tex2D(_MainTex, i.uv);

				#if defined(_STRAIGHT_ALPHA_INPUT)
				texColor.rgb *= texColor.a;
				#endif
				 
				// 最终颜色
                half4 finalColor = (texColor * i.vertexColor) ;
                ApplyRim(finalColor);
				
				return finalColor;
			}
			ENDCG
		}

	}
	CustomEditor "SpineShaderWithOutlineGUI"
}

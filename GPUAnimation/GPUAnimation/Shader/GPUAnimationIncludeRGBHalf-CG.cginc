//------------------------------------------------------------------------------
// This file is part of MistLand project in xhy.
// Copyright © 2025 xhy Technology Co., Ltd.
// All Right Reserved.
//------------------------------------------------------------------------------

#ifndef GPUANIMATIONCOMMON_RGBHALF_CG
#define GPUANIMATIONCOMMON_RGBHALF_CG

#include "GPUAnimationIncludeCommon-CG.cginc"


float4x4 SampleTransformMatrix(uint sampleFrame,uint transformIndex)
{
    float4 index=float4(.5h+transformIndex*3,sampleFrame,0,0);
    return float4x4(tex2Dlod(_AnimTex, index * _AnimTex_TexelSize)
    , tex2Dlod(_AnimTex, (index + float4(1, 0,0,0)) * _AnimTex_TexelSize)
    , tex2Dlod(_AnimTex,  (index + float4(2, 0,0,0)) * _AnimTex_TexelSize)
    ,float4(0,0,0,1));
}

// float4x4 SampleTransformMatrix(uint sampleFrame,uint4 transformIndex,float4 transformWeights)
// {
//     return SampleTransformMatrix(sampleFrame, transformIndex.x) * transformWeights.x
//             + SampleTransformMatrix(sampleFrame, transformIndex.y) * transformWeights.y
//             + SampleTransformMatrix(sampleFrame, transformIndex.z) * transformWeights.z
//             + SampleTransformMatrix(sampleFrame, transformIndex.w) * transformWeights.w;
// }


float4x4 SampleTransformMatrix(uint sampleFrame,uint4 transformIndex,float4 transformWeights)
{

    #if defined(_SKIN_BONE_2)
        float4x4 blendSkin0 = SampleTransformMatrix(sampleFrame, transformIndex.x) * transformWeights.x;
        float4x4 blendSkin1 = SampleTransformMatrix(sampleFrame, transformIndex.y) * transformWeights.y;
        return blendSkin0 + blendSkin1;
    #elif defined(_SKIN_BONE_4) 
        float4x4 blendSkin0 = SampleTransformMatrix(sampleFrame, transformIndex.x) * transformWeights.x;
        float4x4 blendSkin1 = SampleTransformMatrix(sampleFrame, transformIndex.y) * transformWeights.y;
        float4x4 blendSkin2 = SampleTransformMatrix(sampleFrame, transformIndex.z) * transformWeights.z;
        float4x4 blendSkin3 = SampleTransformMatrix(sampleFrame, transformIndex.w) * transformWeights.w;
        return blendSkin0 + blendSkin1 + blendSkin2 + blendSkin3;
    #else
        float4x4 blendSkin0 = SampleTransformMatrix(sampleFrame, transformIndex.x) * transformWeights.x;
         return blendSkin0;
    #endif

}

void SampleTransform(uint4 transformIndexes,float4 transformWeights,inout float3 positionOS,inout float3 normalOS)
{    

    float4x4 sampleMatrix = lerp(SampleTransformMatrix(_FrameBegin, transformIndexes, transformWeights), SampleTransformMatrix(_FrameEnd, transformIndexes, transformWeights), _FrameInterpolate);
    normalOS = mul((float3x3)sampleMatrix,normalOS);
    positionOS=mul(sampleMatrix,float4(positionOS,1)).xyz;
}

void SampleTransform(uint4 transformIndexes, float4 transformWeights, inout float3 positionOS)
{

    float4x4 sampleMatrix = lerp(SampleTransformMatrix(_FrameBegin, transformIndexes, transformWeights), SampleTransformMatrix(_FrameEnd, transformIndexes, transformWeights), _FrameInterpolate);

    positionOS = mul(sampleMatrix, float4(positionOS, 1)).xyz;
}

//以2的幂次方对齐宽度纹理，采样顶点
float3 SamplePositionPowOf2(uint vertexID,uint frame,uint offset)
{

    // 计算当前顶点在动画纹理中的总索引
	// y: 当前动画帧索引
	// _VertexCount: 模型顶点总数
	// vid: 当前顶点ID
	// total = 当前帧 * 顶点数 + 当前顶点ID
	float total = frame * _VertexCount*offset + vertexID * offset;
				
	// 将总索引转换为UV坐标
	// newUv.x: 纹理的x坐标（列）
	// newUv.y: 纹理的y坐标（行）
	float2 newUv = uvConvert(total);
				
	// 将UV坐标转换为纹理采样坐标
	// _AnimTex_TexelSize.x: 纹理单个像素的宽度
	// _AnimTex_TexelSize.y: 纹理单个像素的高度
	// 加0.5是为了采样像素中心点
	float2 animUv = float2((newUv.x + 0.5) * _AnimTex_TexelSize.x, (newUv.y + 0.5) * _AnimTex_TexelSize.y);
				
	// 从动画纹理中采样顶点位置
	// _AnimTex: 存储顶点动画数据的纹理
	// tex2Dlod: 使用lod级别0进行纹理采样
	// modelPos: 采样得到的顶点位置（x,y,z,w）
	float4 modelPos = tex2Dlod(_AnimTex, float4(animUv, 0, 0));
    return modelPos.xyz;
}

float3 SampleNormalPowOf2(uint vertexID,uint frame,uint offset)
{

	float total = frame * _VertexCount * offset + vertexID * offset +1;

	float2 newUv = uvConvert(total);

	float2 animUv = float2((newUv.x + 0.5) * _AnimTex_TexelSize.x, (newUv.y + 0.5) * _AnimTex_TexelSize.y);

	float4 modelPos = tex2Dlod(_AnimTex, float4(animUv, 0, 0));
    return modelPos.xyz;
}

//以顶点数为对齐宽度纹理，采样顶点
float3 SamplePosition(uint vertexID, uint frame, uint offset)
{
    return tex2Dlod(_AnimTex, float4((vertexID * offset + .5) * _AnimTex_TexelSize.x, frame * _AnimTex_TexelSize.y, 0, 0)).xyz;
}

float3 SampleNormal(uint vertexID, uint frame, uint offset)
{
    return tex2Dlod(_AnimTex, float4((vertexID * offset + 1 + .5) * _AnimTex_TexelSize.x, frame * _AnimTex_TexelSize.y, 0, 0)).xyz;
}

void SampleVertexAndNormal(uint vertexID,inout float3 positionOS,inout float3 normalOS)
{
    uint offset = 2;
#if defined(_ALING_POW2)
    positionOS = lerp(SamplePositionPowOf2(vertexID, _FrameBegin,offset), SamplePositionPowOf2(vertexID, _FrameEnd,offset), _FrameInterpolate);
    normalOS = lerp(SampleNormalPowOf2(vertexID, _FrameBegin,offset), SampleNormalPowOf2(vertexID, _FrameEnd,offset), _FrameInterpolate);
#else
    positionOS = lerp(SamplePosition(vertexID, _FrameBegin, offset), SamplePosition(vertexID, _FrameEnd, offset), _FrameInterpolate);
    normalOS = lerp(SampleNormal(vertexID, _FrameBegin, offset), SampleNormal(vertexID, _FrameEnd, offset), _FrameInterpolate);
#endif
    
    // positionOS = SamplePosition(vertexID, _FrameBegin,offset);
    // normalOS = SampleNormal(vertexID, _FrameBegin,offset);
}

/*
void SampleVertexAndNormal11(uint vertexID,inout float3 positionOS,inout float3 normalOS)
{
    uint offset = 2;

    float3 posA = lerp(SamplePosition(vertexID, _FrameBegin,offset), SamplePosition(vertexID, _FrameEnd,offset), _FrameInterpolate);
    float3 normalOSA = lerp(SampleNormal(vertexID, _FrameBegin,offset), SampleNormal(vertexID, _FrameEnd,offset), _FrameInterpolate);


    float3 posB = lerp(SamplePosition(vertexID, _CurFrameB,offset), SamplePosition(vertexID, _NextFrameB,offset), _LerpB);
    float3 normalOSB = lerp(SampleNormal(vertexID, _CurFrameB,offset), SampleNormal(vertexID, _NextFrameB,offset), _LerpB);

    // float3 posA = SamplePosition(vertexID, _FrameBegin,offset);
    // float3 normalOSA = SampleNormal(vertexID, _FrameBegin,offset);

        
    // float3 posB =SamplePosition(vertexID, _CurFrameB,offset);
    // float3 normalOSB =SampleNormal(vertexID, _CurFrameB,offset);
    // 动画过渡
    positionOS = lerp(posA, posB, _CrossWeight);
    normalOS =  lerp(normalOSA, normalOSB, _CrossWeight);
    

}  
*/
void SampleVertex(uint vertexID,inout float3 positionOS)
{
    uint offset = 1;
#if defined(_ALING_POW2)
    positionOS = lerp(SamplePositionPowOf2(vertexID, _FrameBegin,offset), SamplePositionPowOf2(vertexID, _FrameEnd,offset), _FrameInterpolate);
#else
    positionOS = lerp(SamplePosition(vertexID, _FrameBegin, offset), SamplePosition(vertexID, _FrameEnd, offset), _FrameInterpolate);
#endif

}

#endif
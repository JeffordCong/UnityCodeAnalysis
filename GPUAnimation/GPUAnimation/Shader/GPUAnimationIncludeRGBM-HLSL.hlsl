
#ifndef GPUANIMATION_RGBM_HLSL
#define GPUANIMATION_RGBM_HLSL

#include "GPUAnimationIncludeCommon-HLSL.hlsl"


// RGBM解码函数
inline float3 DecodeRGBM(float4 rgba)         
{           
    return rgba.rgb * rgba.a;       
}  


float DecodeFloatRGBA32(float4 color, float minValue, float maxValue)
{
    // color分量范围0~1，转uint
    uint r = (uint)(color.r * 255.0);
    uint g = (uint)(color.g * 255.0);
    uint b = (uint)(color.b * 255.0);
    uint a = (uint)(color.a * 255.0);
    uint enc = (r << 24) | (g << 16) | (b << 8) | a;
    float normalized = enc / 4294967295.0;
    return lerp(minValue, maxValue, normalized);
}


// float4x4 SampleTransformMatrix(uint sampleFrame,uint transformIndex)
// {
//     float2 index=float2(.5h+transformIndex*3,.5h+sampleFrame);
//     return float4x4(SAMPLE_TEXTURE2D_LOD(_AnimTex,sampler_AnimTex, index * _AnimTex_TexelSize.xy, 0)
//     , SAMPLE_TEXTURE2D_LOD(_AnimTex,sampler_AnimTex, (index + float2(1, 0)) * _AnimTex_TexelSize.xy, 0)
//     , SAMPLE_TEXTURE2D_LOD(_AnimTex,sampler_AnimTex,  (index + float2(2, 0)) * _AnimTex_TexelSize.xy, 0)
//     ,float4(0,0,0,1));
// }

float4x4 SampleTransformMatrix(uint sampleFrame, uint transformIndex)
{
    float4x4 m = float4x4(0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,1);
    
    // 展开循环，消除for
    int basePixelX = transformIndex * 12;
    
    // 第一行
    float2 uv00 = float2(basePixelX + 0.5, sampleFrame + 0.5);
    float2 uv01 = float2(basePixelX + 1.5, sampleFrame + 0.5);
    float2 uv02 = float2(basePixelX + 2.5, sampleFrame + 0.5);
    float2 uv03 = float2(basePixelX + 3.5, sampleFrame + 0.5);
    m[0][0] = DecodeFloatRGBA32(SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, uv00 * _AnimTex_TexelSize.xy,0), _BoundsRange.x, _BoundsRange.y);
    m[0][1] = DecodeFloatRGBA32(SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, uv01 * _AnimTex_TexelSize.xy,0), _BoundsRange.x, _BoundsRange.y);
    m[0][2] = DecodeFloatRGBA32(SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, uv02 * _AnimTex_TexelSize.xy,0), _BoundsRange.x, _BoundsRange.y);
    m[0][3] = DecodeFloatRGBA32(SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, uv03 * _AnimTex_TexelSize.xy,0), _BoundsRange.x, _BoundsRange.y);
    
    // 第二行
    float2 uv10 = float2(basePixelX + 4.5, sampleFrame + 0.5);
    float2 uv11 = float2(basePixelX + 5.5, sampleFrame + 0.5);
    float2 uv12 = float2(basePixelX + 6.5, sampleFrame + 0.5);
    float2 uv13 = float2(basePixelX + 7.5, sampleFrame + 0.5);
    m[1][0] = DecodeFloatRGBA32(SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, uv10 * _AnimTex_TexelSize.xy,0), _BoundsRange.x, _BoundsRange.y);
    m[1][1] = DecodeFloatRGBA32(SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, uv11 * _AnimTex_TexelSize.xy,0), _BoundsRange.x, _BoundsRange.y);
    m[1][2] = DecodeFloatRGBA32(SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, uv12 * _AnimTex_TexelSize.xy,0), _BoundsRange.x, _BoundsRange.y);
    m[1][3] = DecodeFloatRGBA32(SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, uv13 * _AnimTex_TexelSize.xy,0), _BoundsRange.x, _BoundsRange.y);
    
    // 第三行
    float2 uv20 = float2(basePixelX + 8.5, sampleFrame + 0.5);
    float2 uv21 = float2(basePixelX + 9.5, sampleFrame + 0.5);
    float2 uv22 = float2(basePixelX + 10.5, sampleFrame + 0.5);
    float2 uv23 = float2(basePixelX + 11.5, sampleFrame + 0.5);
    m[2][0] = DecodeFloatRGBA32(SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, uv20 * _AnimTex_TexelSize.xy,0), _BoundsRange.x, _BoundsRange.y);
    m[2][1] = DecodeFloatRGBA32(SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, uv21 * _AnimTex_TexelSize.xy,0), _BoundsRange.x, _BoundsRange.y);
    m[2][2] = DecodeFloatRGBA32(SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, uv22 * _AnimTex_TexelSize.xy,0), _BoundsRange.x, _BoundsRange.y);
    m[2][3] = DecodeFloatRGBA32(SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, uv23 * _AnimTex_TexelSize.xy,0), _BoundsRange.x, _BoundsRange.y);
    
    return m;
}




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

            //    return SampleTransformMatrix(sampleFrame, transformIndex.x) * transformWeights.x
            // + SampleTransformMatrix(sampleFrame, transformIndex.y) * transformWeights.y
            // + SampleTransformMatrix(sampleFrame, transformIndex.z) * transformWeights.z
            // + SampleTransformMatrix(sampleFrame, transformIndex.w) * transformWeights.w;

}

void SampleTransform(uint4 transformIndexes,float4 transformWeights,inout float3 positionOS,inout float3 normalOS)
{    

    float4x4 sampleMatrix = lerp(SampleTransformMatrix(_FrameBegin, transformIndexes, transformWeights), SampleTransformMatrix(_FrameEnd, transformIndexes, transformWeights), _FrameInterpolate);
    normalOS = mul((float3x3)sampleMatrix,normalOS);
    positionOS=mul(sampleMatrix,float4(positionOS,1)).xyz;
}



float3 SamplePositionPowOf2(uint vertexID, uint frame, uint offset)
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
	// encodedData: 采样得到的顶点位置（x,y,z,w）
	float4 encodedData = SAMPLE_TEXTURE2D_LOD(_AnimTex,sampler_AnimTex, animUv, 0);


    float3 normalizedPos = DecodeRGBM(encodedData);
          
    float3 worldPos = (normalizedPos - 0.5)*_BoundsRange.y*2.0 + _BoundsCenter;
    return worldPos;


}

float3 SampleNormalPowOf2(uint vertexID, uint frame, uint offset)
{

	float total = frame * _VertexCount * offset + vertexID * offset +1;

	float2 newUv = uvConvert(total);

	float2 animUv = float2((newUv.x + 0.5) * _AnimTex_TexelSize.x, (newUv.y + 0.5) * _AnimTex_TexelSize.y);

	float4 encodedData =SAMPLE_TEXTURE2D_LOD(_AnimTex,sampler_AnimTex, animUv, 0);

    float3 normalizedPos = DecodeRGBM(encodedData);
    float3 worldPos = normalizedPos * 2.0 - 1.0; // 从[0,1]恢复到[-1,1]    
    return worldPos;

}


//以顶点数为对齐宽度纹理，采样顶点
float3 SamplePosition(uint vertexID, uint frame, uint offset)
{

    float4 encodedData = SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, float2((vertexID * offset + .5) * _AnimTex_TexelSize.x, frame * _AnimTex_TexelSize.y), 0);
    float3 normalizedPos = DecodeRGBM(encodedData);
          
    float3 worldPos = (normalizedPos - 0.5) * _BoundsRange.y * 2.0 + _BoundsCenter;
    return worldPos;

}

float3 SampleNormal(uint vertexID, uint frame, uint offset)
{

    float4 encodedData = SAMPLE_TEXTURE2D_LOD(_AnimTex, sampler_AnimTex, float2((vertexID * offset + 1 + .5) * _AnimTex_TexelSize.x, frame * _AnimTex_TexelSize.y), 0);
    float3 normalizedPos = DecodeRGBM(encodedData);
    float3 worldPos = normalizedPos * 2.0 - 1.0; // 从[0,1]恢复到[-1,1]    
    return worldPos;
}


void SampleVertex(uint vertexID,inout float3 positionOS)
{
    uint offset = 1;
#if defined(_ALING_POW2)
    positionOS = lerp(SamplePositionPowOf2(vertexID, _FrameBegin,offset), SamplePositionPowOf2(vertexID, _FrameEnd,offset), _FrameInterpolate);
#else
    positionOS = lerp(SamplePosition(vertexID, _FrameBegin, offset), SamplePosition(vertexID, _FrameEnd, offset), _FrameInterpolate);
#endif
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
    
}
#endif
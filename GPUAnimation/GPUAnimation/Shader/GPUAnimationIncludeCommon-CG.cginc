//------------------------------------------------------------------------------
// This file is part of MistLand project in xhy.
// Copyright © 2025 xhy Technology Co., Ltd.
// All Right Reserved.
//------------------------------------------------------------------------------

#ifndef GPUANIMATIONCOMMON_CG
#define GPUANIMATIONCOMMON_CG
//#error "define for first.."

sampler2D _AnimTex;
float4 _AnimTex_ST;

float4 _AnimTex_TexelSize;
            
float3 _BoundsCenter;
float3 _BoundsRange;
int _VertexCount;

    // int _CurFrameB, _NextFrameB;
    // float _LerpB;
    // float _CrossWeight;


UNITY_INSTANCING_BUFFER_START(PropsGPUAnim)
    UNITY_DEFINE_INSTANCED_PROP(half4, _RimColor)
    UNITY_DEFINE_INSTANCED_PROP(half, _RimTime)

    UNITY_DEFINE_INSTANCED_PROP(int, _AnimFrameBegin)
    UNITY_DEFINE_INSTANCED_PROP(int, _AnimFrameEnd)
    UNITY_DEFINE_INSTANCED_PROP(float, _AnimFrameInterpolate)


//    UNITY_DEFINE_INSTANCED_PROP(int, _AnimCurFrameB)
//     UNITY_DEFINE_INSTANCED_PROP(int, _AnimNextFrameB)
//     UNITY_DEFINE_INSTANCED_PROP(float, _AnimLerpB)
//     UNITY_DEFINE_INSTANCED_PROP(float, _AnimCrossWeight)
UNITY_INSTANCING_BUFFER_END(PropsGPUAnim)


#define _mainRimColor UNITY_ACCESS_INSTANCED_PROP(PropsGPUAnim,_RimColor)
#define _mainRimTime UNITY_ACCESS_INSTANCED_PROP(PropsGPUAnim,_RimTime)

#define _FrameBegin UNITY_ACCESS_INSTANCED_PROP(PropsGPUAnim,_AnimFrameBegin)
#define _FrameEnd UNITY_ACCESS_INSTANCED_PROP(PropsGPUAnim,_AnimFrameEnd)
#define _FrameInterpolate UNITY_ACCESS_INSTANCED_PROP(PropsGPUAnim,_AnimFrameInterpolate)

// #define _CurFrameB UNITY_ACCESS_INSTANCED_PROP(PropsGPUAnim,_AnimCurFrameB)
// #define _NextFrameB UNITY_ACCESS_INSTANCED_PROP(PropsGPUAnim,_AnimNextFrameB)
// #define _LerpB UNITY_ACCESS_INSTANCED_PROP(PropsGPUAnim,_AnimLerpB)
// #define _CrossWeight UNITY_ACCESS_INSTANCED_PROP(PropsGPUAnim,_AnimCrossWeight)
//闪白
inline void ApplyRim(inout half4 finalColor)
{
    finalColor +=_mainRimColor * _mainRimTime;
}

// 将一维索引转换为二维UV坐标
// total: 一维索引值，表示在动画纹理中的线性位置
// _AnimTex_TexelSize.z: 动画纹理的宽度（以像素为单位）
// 返回值: 二维UV坐标，x表示列索引，y表示行索引
inline float2 uvConvert(float total)
{
	// 计算行索引：总索引除以纹理宽度，取整得到行号
	float new_y = total / _AnimTex_TexelSize.z;			
	// 计算列索引：
	// 1. 使用fmod(new_y, 1.0)获取行内的小数部分
	// 2. 乘以纹理宽度得到列索引
	// 3. 使用floor取整得到确切的列号
	float new_x = floor(fmod(new_y, 1.0) * _AnimTex_TexelSize.z);			
    // 对行索引取整，得到确切的整数行号		
    new_y = floor(new_y);
    // 返回二维UV坐标			
    return float2(new_x, new_y);		
}

#endif
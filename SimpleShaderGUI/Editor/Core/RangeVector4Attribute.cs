using UnityEngine;

namespace Scarecrow
{
    /// <summary>
    /// 向量范围滑块特性
    /// 用于标记 Vector4 类型的属性，自动使用 RangeDrawer 绘制最小-最大范围滑块
    /// 参数格式：Vector4(currentMin, currentMax, minLimit, maxLimit)
    /// </summary>
    public class RangeVector4Attribute : PropertyAttribute
    {
    }
}

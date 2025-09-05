
using UnityEngine;

public static  class TransformExtensions
{
    // 递归计算所有子节点数量（包括嵌套子节点）
    public static int GetTotalChildCount(this Transform parent)
    {
        int count = 0;

        // 遍历直接子节点
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            count++; // 增加直接子节点计数

            // 递归计算子节点的子节点数量
            count += child.GetTotalChildCount();
        }

        return count;
    }
}

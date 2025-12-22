using UnityEngine;

namespace Scarecrow
{
    /// <summary>
    /// 折叠页状态管理类
    /// 封装折叠页的所有状态信息，提高代码可读性
    /// </summary>
    public class FoldoutState
    {
        /// <summary>当前折叠页等级（描述属性绘制在哪级折叠页中）</summary>
        public int Level { get; set; }

        /// <summary>折叠页编辑等级（控制折叠页权限的等级）</summary>
        public int EditorLevel { get; set; }

        /// <summary>折叠页展开状态：true=展开，false=折叠</summary>
        public bool IsOpen { get; set; }

        /// <summary>折叠页内属性是否可编辑</summary>
        public bool IsEditable { get; set; }

        public FoldoutState()
        {
            Reset();
        }

        /// <summary>重置为初始状态</summary>
        public void Reset()
        {
            Level = 0;
            EditorLevel = 0;
            IsOpen = true;
            IsEditable = true;
        }

        /// <summary>设置所有状态</summary>
        public void Set(int level, int editorLevel, bool isOpen, bool isEditable)
        {
            Level = level;
            EditorLevel = editorLevel;
            IsOpen = isOpen;
            IsEditable = isEditable;
        }

        /// <summary>复制另一个状态</summary>
        public void CopyFrom(FoldoutState other)
        {
            if (other != null)
            {
                Level = other.Level;
                EditorLevel = other.EditorLevel;
                IsOpen = other.IsOpen;
                IsEditable = other.IsEditable;
            }
        }

        public override string ToString()
        {
            return $"FoldoutState(Level={Level}, EditorLevel={EditorLevel}, IsOpen={IsOpen}, IsEditable={IsEditable})";
        }
    }
}

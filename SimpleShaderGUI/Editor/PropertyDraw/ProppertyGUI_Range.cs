using UnityEditor;
using UnityEngine;

namespace Scarecrow
{
    /// <summary>
    /// 范围滑块绘制器
    /// 绘制最小-最大范围滑块，支持输入框直接修改值
    /// 参数格式：Vector4(currentMin, currentMax, minLimit, maxLimit)
    /// </summary>
    [CustomPropertyDrawer(typeof(RangeVector4Attribute))]
    public class RangeDrawer : PropertyDrawerBase
    {
        /// <summary>该控件会在以下选项中显示</summary>
        private string[] _showList = new string[0];

        /// <summary>是否总是显示</summary>
        private bool _isAlwaysShow = true;

        public RangeDrawer()
        {
            _isAlwaysShow = true;
        }

        public RangeDrawer(params string[] showList)
        {
            _showList = showList ?? new string[0];
            _isAlwaysShow = _showList.Length == 0;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            CacheShaderGUI(editor);

            // 检查属性是否要被绘制
            if (!(_isAlwaysShow || ShaderGUI.GetShowState(_showList)))
                return -2f;

            return base.GetPropertyHeight(prop, label, editor);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            CacheShaderGUI(editor);

            // 验证 ShaderGUI
            if (ShaderGUI == null)
            {
                ShowUsageError(prop, "Please use SimpleShaderGUI in your shader");
                return;
            }

            // 验证属性类型
            if (!ValidatePropertyType(prop, MaterialProperty.PropType.Vector, "Property must be of type vector"))
                return;

            // 检查属性是否要被绘制
            if (!(_isAlwaysShow || ShaderGUI.GetShowState(_showList)))
                return;

            // 获取并修正范围值
            Vector4 value = prop.vectorValue;
            value = NormalizeRangeValues(value);

            // 计算布局
            Rect rangeRect = new Rect(position) { width = position.width - 110 };
            Rect minRect = new Rect(position) { x = position.xMax - 105f, width = 50 };
            Rect maxRect = new Rect(position) { x = position.xMax - 50f, width = 50 };

            // 绘制范围滑块
            SetMixedValueDisplay(HasMixedValue(prop));
            EditorGUI.BeginChangeCheck();
            EditorGUI.MinMaxSlider(rangeRect, label, ref value.x, ref value.y, value.z, value.w);

            // 绘制最小值输入框
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            value.x = EditorGUI.FloatField(minRect, value.x);
            value.y = EditorGUI.FloatField(maxRect, value.y);
            EditorGUI.indentLevel = indentLevel;

            ClearMixedValueDisplay();

            if (EditorGUI.EndChangeCheck())
            {
                prop.vectorValue = value;
            }
        }

        /// <summary>
        /// 修正和验证范围值
        /// 确保 min <= max 和限制值在合法范围内
        /// </summary>
        private Vector4 NormalizeRangeValues(Vector4 value)
        {
            // 如果最小限制等于最大限制，调整最大限制以避免错误
            if (value.z == value.w)
                value.w += 0.1f;

            // 确保最小限制不大于最大限制
            if (value.z > value.w)
            {
                float temp = value.z;
                value.z = value.w;
                value.w = temp;
            }

            // 确保当前最小值不大于当前最大值
            if (value.x > value.y)
            {
                float temp = value.x;
                value.x = value.y;
                value.y = temp;
            }

            // 将当前值限制在限制范围内
            value.x = Mathf.Max(value.x, value.z);
            value.y = Mathf.Min(value.y, value.w);

            return value;
        }
    }
}

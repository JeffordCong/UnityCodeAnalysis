using UnityEditor;
using UnityEngine;

namespace Scarecrow
{
    /// <summary>
    /// Toggle 切换控制
    /// 通过 Toggle 开关控制该属性是否显示，并生成相应的 Shader 关键字
    /// </summary>
    public class Toggle_SwitchDrawer : PropertyDrawerBase
    {
        /// <summary>当前 Toggle 状态</summary>
        private bool _isEnabled = true;

        public override void Apply(MaterialProperty prop)
        {
            base.Apply(prop);

            // 初始化关键字和 SwitchList
            if (prop.type == MaterialProperty.PropType.Float)
            {
                _isEnabled = (int)prop.floatValue != 0;
                KeywordManager.SetToggleKeyword(prop, _isEnabled);
                UpdateSwitchList(prop, _isEnabled);
            }
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
            if (!ValidatePropertyType(prop, MaterialProperty.PropType.Float, "Property must be of type float"))
                return;

            // 绘制 Toggle 控件
            SetMixedValueDisplay(HasMixedValue(prop));
            EditorGUI.BeginChangeCheck();
            _isEnabled = EditorGUI.Toggle(position, label, (int)prop.floatValue == 0 ? false : true);

            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = _isEnabled ? 1 : 0;
                KeywordManager.SetToggleKeyword(prop, _isEnabled);
            }

            ClearMixedValueDisplay();

            // 更新状态
            if (!HasMixedValue(prop))
                KeywordManager.SetToggleKeyword(prop, _isEnabled);

            UpdateSwitchList(prop, _isEnabled);
        }

        /// <summary>更新 SwitchList</summary>
        private void UpdateSwitchList(MaterialProperty prop, bool isEnabled)
        {
            if (ShaderGUI == null)
                return;

            if (isEnabled)
            {
                if (!ShaderGUI.Context.SwitchList.Contains(prop.name))
                    ShaderGUI.Context.SwitchList.Add(prop.name);
            }
            else
            {
                ShaderGUI.Context.SwitchList.Remove(prop.name);
            }
        }
    }

    /// <summary>
    /// Enum 选项切换控制
    /// 通过 Enum 下拉菜单选择不同选项，只有选中的选项才会被激活
    /// </summary>
    public class Enum_SwitchDrawer : PropertyDrawerBase
    {
        /// <summary>枚举选项列表</summary>
        private string[] _enumList = new string[0];

        /// <summary>枚举选项值</summary>
        private int[] _enumValues = new int[0];

        /// <summary>当前选中的选项索引</summary>
        private int _selectedIndex;

        /// <summary>
        /// 枚举控制器构造函数
        /// </summary>
        public Enum_SwitchDrawer(params string[] enumList)
        {
            _enumList = enumList;
            _enumValues = new int[_enumList.Length];
            for (int i = 0; i < _enumValues.Length; i++)
                _enumValues[i] = i;
        }

        public override void Apply(MaterialProperty prop)
        {
            base.Apply(prop);

            // 初始化关键字和 SwitchList
            if (prop.type == MaterialProperty.PropType.Float)
            {
                InitializeEnumData(prop);
                KeywordManager.SetEnumKeyword(prop, _enumList, _selectedIndex);
                UpdateSwitchList(prop);
            }
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
            if (!ValidatePropertyType(prop, MaterialProperty.PropType.Float, "Property must be of type float"))
                return;

            // 初始化枚举数据
            InitializeEnumData(prop);

            // 绘制下拉菜单
            SetMixedValueDisplay(HasMixedValue(prop));
            EditorGUI.BeginChangeCheck();
            _selectedIndex = EditorGUI.IntPopup(position, label, _selectedIndex, _enumList, _enumValues);

            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = _selectedIndex;
                KeywordManager.SetEnumKeyword(prop, _enumList, _selectedIndex);
            }

            ClearMixedValueDisplay();

            // 更新状态
            if (!HasMixedValue(prop))
                KeywordManager.SetEnumKeyword(prop, _enumList, _selectedIndex);

            UpdateSwitchList(prop);
        }

        /// <summary>初始化枚举数据</summary>
        private void InitializeEnumData(MaterialProperty prop)
        {
            int index = (int)prop.floatValue;
            _selectedIndex = Mathf.Clamp(index, 0, _enumList.Length - 1);
        }

        /// <summary>更新 SwitchList</summary>
        private void UpdateSwitchList(MaterialProperty prop)
        {
            if (ShaderGUI == null)
                return;

            // 清除旧的选项
            foreach (string option in _enumList)
            {
                ShaderGUI.Context.SwitchList.Remove(option);
            }

            // 只添加当前选中的选项
            if (_selectedIndex >= 0 && _selectedIndex < _enumList.Length)
            {
                if (!ShaderGUI.Context.SwitchList.Contains(_enumList[_selectedIndex]))
                    ShaderGUI.Context.SwitchList.Add(_enumList[_selectedIndex]);
            }
        }
    }

    /// <summary>
    /// 条件显示控制
    /// 该属性仅在指定条件被激活时才显示
    /// 通常与 Toggle_SwitchDrawer 或 Enum_SwitchDrawer 配合使用
    /// </summary>
    public class SwitchDrawer : PropertyDrawerBase
    {
        /// <summary>该属性显示的条件列表</summary>
        private string[] _showList = new string[0];

        /// <summary>
        /// 条件显示控制器构造函数
        /// </summary>
        public SwitchDrawer() { }

        public SwitchDrawer(params string[] showList)
        {
            _showList = showList;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return -1.5f;
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

            // 只在满足条件时显示属性
            if (ShouldShowProperty(_showList))
            {
                editor.DefaultShaderProperty(prop, label);
            }
        }
    }
}

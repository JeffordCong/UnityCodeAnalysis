using UnityEditor;
using UnityEngine;

namespace Scarecrow
{
    /// <summary>
    /// 折叠页样式枚举
    /// </summary>
    public enum FoldoutStyle
    {
        /// <summary>大型折叠页（用于一级）</summary>
        Big = 1,

        /// <summary>中型折叠页（用于二级）</summary>
        Median = 2,

        /// <summary>小型折叠页（用于三级+）</summary>
        Small = 3,
    }

    /// <summary>
    /// 折叠页绘制器
    /// 实现分级嵌套的折叠页，支持多种样式、权限控制和 Pass 切换
    /// 参考 Word 文档的多级标题设计
    ///
    /// Pass 切换支持：
    /// - 属性名以 "_Enable" 开头时自动识别为 Pass 切换
    /// - 格式：_EnableOutlinePass 或 _EnableOutline
    /// - 自动生成关键字：_ENABLE_OUTLINE（大写）
    /// </summary>
    public class FoldoutDrawer : PropertyDrawerBase
    {
        /// <summary>折叠页等级</summary>
        private int _level = 1;

        /// <summary>折叠页自身缩进大小</summary>
        private const float FoldoutIndentSize = 15f;

        /// <summary>折叠页初始展开状态</summary>
        private bool _isOpen = true;

        /// <summary>折叠页样式</summary>
        private FoldoutStyle _style = FoldoutStyle.Big;

        /// <summary>是否绘制复选框控制编辑权限</summary>
        private bool _drawToggle = false;

        /// <summary>折叠页内容是否可编辑</summary>
        private bool _isEditable = true;

        /// <summary>该控件会在以下选项中显示</summary>
        private string[] _showList = new string[0];

        /// <summary>是否总是显示</summary>
        private bool _isAlwaysShow = true;

        /// <summary>当前处理的属性</summary>
        private MaterialProperty _property;

        /// <summary>是否为 Pass 切换模式（属性名以 "_Enable" 开头）</summary>
        private bool _isPassToggleMode = false;

        /// <summary>Pass 名称（用于生成关键字）</summary>
        private string _passName = "";

        /// <summary>Pass 切换的展开状态</summary>
        private bool _passToggleOpen = true;

        /// <summary>
        /// FoldoutDrawer 构造函数
        /// 参数说明：
        /// - foldoutLevel: 折叠页等级，默认 1（最高级）
        /// - foldoutStyle: 折叠页样式，1=大，2=中，3=小
        /// - foldoutToggleDraw: 是否绘制复选框，0=否，1=是
        /// - foldoutOpen: 初始展开状态，0=折叠，1=展开
        /// - showList: 条件显示列表，当任意选项被激活时显示该折叠页
        /// </summary>
        public FoldoutDrawer()
            : this(1) { }

        public FoldoutDrawer(float level)
            : this(level, 1) { }

        public FoldoutDrawer(float level, float style)
            : this(level, style, 0) { }

        public FoldoutDrawer(float level, float style, float drawToggle)
            : this(level, style, drawToggle, 1) { }

        public FoldoutDrawer(
            float level,
            float style,
            float drawToggle,
            float isOpen,
            params string[] showList
        )
        {
            _level = Mathf.Max((int)level, 1);

            // 设置样式
            switch ((int)style)
            {
                case 2:
                    _style = FoldoutStyle.Median;
                    break;
                case 3:
                    _style = FoldoutStyle.Small;
                    _drawToggle = false; // 小样式不绘制复选框
                    break;
                default:
                    _style = FoldoutStyle.Big;
                    break;
            }

            // 设置其他选项
            _drawToggle = (int)drawToggle != 0 && _style != FoldoutStyle.Small;
            _isOpen = (int)isOpen != 0;
            _showList = showList ?? new string[0];
            _isAlwaysShow = _showList.Length == 0;
        }

        public override void Apply(MaterialProperty prop)
        {
            base.Apply(prop);

            // 根据初始值设置关键字
            if (prop.type == MaterialProperty.PropType.Float)
            {
                _isEditable = prop.floatValue != 0;
                KeywordManager.SetFoldoutEditorKeyword(prop, _isEditable);
            }
        }

        public override float GetPropertyHeight(
            MaterialProperty prop,
            string label,
            MaterialEditor editor
        )
        {
            return -2;
        }

        public override void OnGUI(
            Rect position,
            MaterialProperty prop,
            GUIContent label,
            MaterialEditor editor
        )
        {
            CacheShaderGUI(editor);

            // 验证 ShaderGUI
            if (ShaderGUI == null)
            {
                ShowUsageError(prop, "Please use SimpleShaderGUI in your shader");
                return;
            }

            // 验证属性类型
            if (
                !ValidatePropertyType(
                    prop,
                    MaterialProperty.PropType.Float,
                    "Property must be of type float"
                )
            )
                return;

            // 验证是否是折叠页
            if (!PropertyDrawHelper.IsFoldout(prop))
            {
                ShowUsageError(
                    prop,
                    $"Please add {PropertyDrawHelper.FoldoutSign} after displayName"
                );
                return;
            }

            // 检测是否是 Pass 切换模式
            DetectPassToggleMode(prop);

            // 如果属于上级折叠页的内容且上级折叠，则不显示
            if (
                _level > ShaderGUI.Context.FoldoutState.Level
                && !ShaderGUI.Context.FoldoutState.IsOpen
            )
                return;

            // 检查条件显示
            if (!(_isAlwaysShow || ShaderGUI.GetShowState(_showList)))
            {
                ShaderGUI.SetFoldout(
                    _level,
                    ShaderGUI.Context.FoldoutState.EditorLevel,
                    false,
                    ShaderGUI.Context.FoldoutState.IsEditable
                );
                return;
            }

            // 读取当前值
            _isEditable = prop.floatValue != 0;
            _property = prop;

            // 开始检测UI变化
            EditorGUI.BeginChangeCheck();

            // 绘制折叠页
            DrawFoldout();

            // 处理状态变化
            // 处理状态变化
            if (_isPassToggleMode)
            {
                // Pass 切换模式：处理复选框变化
                if (EditorGUI.EndChangeCheck())
                {
                    _property.floatValue = _isEditable ? 1 : 0;

                    foreach (
                        Material material in System.Array.ConvertAll(
                            _property.targets,
                            obj => (Material)obj
                        )
                    )
                    {
                        if (_isEditable)
                            material.SetShaderPassEnabled(_passName, true);
                        else
                            material.SetShaderPassEnabled(_passName, false);
                    }

                    // 更新 SwitchList 用于条件显示
                    if (_isEditable)
                    {
                        if (!ShaderGUI.Context.SwitchList.Contains(_property.name))
                            ShaderGUI.Context.SwitchList.Add(_property.name);
                    }
                    else
                    {
                        ShaderGUI.Context.SwitchList.Remove(_property.name);
                    }
                }

                // 使用 PassToggle 的展开状态
                _isOpen = _passToggleOpen;
            }
            else
            {
                // 普通折叠页模式
                if (EditorGUI.EndChangeCheck())
                {
                    _property.floatValue = _isEditable ? 1 : 0;
                    KeywordManager.SetFoldoutEditorKeyword(_property, _isEditable);
                }
            }

            // 计算实际的权限状态
            int actualEditorLevel = ShaderGUI.Context.FoldoutState.EditorLevel;
            bool actualIsEditable = ShaderGUI.Context.FoldoutState.IsEditable;

            // 处理权限状态的转换
            bool needsUpdateEditor = CheckEditorStateTransition(actualEditorLevel);
            if (needsUpdateEditor)
            {
                actualEditorLevel = _level;
                actualIsEditable = _isEditable;
            }

            // 更新 ShaderGUI 的折叠页状态
            ShaderGUI.SetFoldout(_level, actualEditorLevel, _isOpen, actualIsEditable);
        }

        /// <summary>绘制折叠页 UI</summary>
        private void DrawFoldout()
        {
            switch (_style)
            {
                case FoldoutStyle.Big:
                    DrawFoldoutShuriken(30, 3);
                    break;
                case FoldoutStyle.Median:
                    DrawFoldoutShuriken(25, 2);
                    break;
                case FoldoutStyle.Small:
                    DrawFoldoutSmall();
                    break;
            }
        }

        /// <summary>绘制大中型折叠页（使用 Shuriken 样式）</summary>
        private void DrawFoldoutShuriken(float height, int fontSizeOffset)
        {
            // 如果上级权限禁用且当前属于上级，则禁用当前折叠页
            bool isDisabled = _level > ShaderGUI.Context.FoldoutState.EditorLevel && !ShaderGUI.Context.FoldoutState.IsEditable;
            if (isDisabled)
                EditorGUI.BeginDisabledGroup(true);

            // 创建 Shuriken 样式背景
            GUIStyle style = new GUIStyle("ShurikenModuleTitle");
            style.border = new RectOffset(15, 7, 4, 4);
            style.font = EditorStyles.boldLabel.font;
            style.fontStyle = EditorStyles.boldLabel.fontStyle;
            style.fontSize = EditorStyles.boldLabel.fontSize + fontSizeOffset;
            style.fixedHeight = height;
            style.contentOffset = new Vector2(20f, -1);
            if (_drawToggle || _isPassToggleMode)
                style.contentOffset += new Vector2(18f, 0);

            // 绘制折叠页背景和文本
            Rect rect = GUILayoutUtility.GetRect(0, height, style);
            rect.xMin += (_level - 1) * FoldoutIndentSize;

            /*
                        // Pass 切换模式：使用绿/灰背景色
                        if (_isPassToggleMode)
                        {
                            Color backgroundColor = _isEditable
                                ? new Color(0.2f, 0.6f, 0.2f, 0.5f)
                                : new Color(0.5f, 0.5f, 0.5f, 0.3f);
                            EditorGUI.DrawRect(rect, backgroundColor);
                        }
                        */

            GUI.Box(rect, PropertyDrawHelper.GetFoldoutDisplayName(_property), style);

            // 绘制展开/折叠三角形
            DrawFoldoutTriangle(rect);

            // 绘制复选框（编辑权限或 Pass 启用状态）
            if (_drawToggle || _isPassToggleMode)
            {
                Rect toggleRect = new Rect(rect.x + 20, rect.y + rect.height / 2 - 7, 14f, 14f);
                if (_property.hasMixedValue)
                    _isEditable = GUI.Toggle(toggleRect, false, "", new GUIStyle("ToggleMixed"));
                else
                    _isEditable = GUI.Toggle(toggleRect, _isEditable, "");
            }

            if (isDisabled)
                EditorGUI.EndDisabledGroup();

            // 处理鼠标点击事件
            if (_isPassToggleMode)
                HandlePassToggleClick(rect);
            else
                HandleFoldoutClick(rect);
        }

        /// <summary>绘制小型折叠页</summary>
        private void DrawFoldoutSmall()
        {
            bool isDisabled = _level > ShaderGUI.Context.FoldoutState.EditorLevel && !ShaderGUI.Context.FoldoutState.IsEditable;
            if (isDisabled)
                EditorGUI.BeginDisabledGroup(true);

            Rect rect = GUILayoutUtility.GetRect(0, 25, EditorStyles.foldout);
            rect.xMin += (_level - 1) * FoldoutIndentSize;

            Event e = Event.current;
            if (e.type == EventType.Repaint)
            {
                // 根据是否为 Pass 切换模式选择正确的展开状态
                bool isOpen = _isPassToggleMode ? _passToggleOpen : _isOpen;
                EditorStyles.foldout.Draw(
                    rect,
                    PropertyDrawHelper.GetFoldoutDisplayName(_property),
                    false,
                    false,
                    isOpen,
                    false
                );
            }

            if (isDisabled)
                EditorGUI.EndDisabledGroup();

            // 处理鼠标点击
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                if (_isPassToggleMode)
                    _passToggleOpen = !_passToggleOpen;
                else
                    _isOpen = !_isOpen;
                e.Use();
            }
        }

        /// <summary>绘制展开/折叠三角形</summary>
        private void DrawFoldoutTriangle(Rect rect)
        {
            Rect triangleRect = new Rect(rect.x + 4, rect.y + rect.height / 2 - 7, 14f, 14f);
            Event e = Event.current;

            if (e.type == EventType.Repaint)
            {
                // 根据是否为 Pass 切换模式选择正确的展开状态
                bool isOpen = _isPassToggleMode ? _passToggleOpen : _isOpen;
                EditorStyles.foldout.Draw(triangleRect, false, false, isOpen, false);
            }
        }

        /// <summary>处理折叠页点击事件</summary>
        private void HandleFoldoutClick(Rect rect)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                _isOpen = !_isOpen;
                e.Use();
            }
        }

        /// <summary>处理 Pass 切换点击事件</summary>
        private void HandlePassToggleClick(Rect rect)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                _passToggleOpen = !_passToggleOpen;
                e.Use();
            }
        }

        /// <summary>
        /// 检查编辑权限状态是否需要转换
        /// </summary>
        private bool CheckEditorStateTransition(int currentEditorLevel)
        {
            // 如果当前权限已启用但此折叠页禁用，需要更新
            bool state1 = ShaderGUI.Context.FoldoutState.IsEditable && !_isEditable;

            // 如果当前权限禁用但此折叠页启用，且此折叠页不属于已记录的权限范围，需要更新
            bool state2 =
                !ShaderGUI.Context.FoldoutState.IsEditable
                && _isEditable
                && _level <= currentEditorLevel;

            // 如果当前折叠页启用，且等级深于当前编辑等级，需要更新（确立新的编辑等级）
            bool state3 =
                ShaderGUI.Context.FoldoutState.IsEditable
                && _isEditable
                && _level > currentEditorLevel;

            return state1 || state2 || state3;
        }

        /// <summary>
        /// 检测是否是 Pass 切换模式
        /// Pass 切换通过属性名以 "_Enable" 开头且以 "Pass" 结尾识别
        /// Pass 名称为属性名去掉 "_Enable" 前缀和 "Pass" 后缀
        /// 例如：_EnableOutlinePass -> Outline
        /// </summary>
        private void DetectPassToggleMode(MaterialProperty prop)
        {
            _isPassToggleMode = prop.name.StartsWith("_Enable") && prop.name.EndsWith("Pass");

            if (_isPassToggleMode)
            {
                // Extract pass name by removing "_Enable" prefix and "Pass" suffix
                // "_EnableOutlinePass" -> "Outline"
                _passName = prop.name[7..^4];
            }
        }
    }

    /// <summary>
    /// 折叠页跳出器
    /// 用于在嵌套的折叠页中跳出到指定等级
    /// 不进行绘制，只改变 SimpleShaderGUI 的折叠页等级状态
    /// </summary>
    public class Foldout_Out : PropertyDrawerBase
    {
        /// <summary>退出到哪个等级</summary>
        private int _level = 1;

        /// <summary>默认跳出到 1 级</summary>
        public Foldout_Out()
            : this(1) { }

        public Foldout_Out(float level)
        {
            _level = Mathf.Max((int)level - 1, 0);
        }

        public override float GetPropertyHeight(
            MaterialProperty prop,
            string label,
            MaterialEditor editor
        )
        {
            return -2;
        }

        public override void OnGUI(
            Rect position,
            MaterialProperty prop,
            string label,
            MaterialEditor editor
        )
        {
            CacheShaderGUI(editor);

            if (ShaderGUI == null)
            {
                ShowUsageError(prop, "Please use SimpleShaderGUI in your shader");
                return;
            }

            if (!PropertyDrawHelper.IsFoldout(prop))
            {
                ShowUsageError(
                    prop,
                    $"Please add {PropertyDrawHelper.FoldoutSign} after displayName"
                );
                return;
            }

            // 如果属于上级折叠页的内容且上级折叠，则不处理
            if (
                _level >= ShaderGUI.Context.FoldoutState.Level
                && !ShaderGUI.Context.FoldoutState.IsOpen
            )
                return;

            // 计算权限状态
            int actualEditorLevel = ShaderGUI.Context.FoldoutState.EditorLevel;
            bool actualIsEditable = ShaderGUI.Context.FoldoutState.IsEditable;

            // 如果上级权限禁用且当前不属于权限范围，需要更新
            if (
                !ShaderGUI.Context.FoldoutState.IsEditable
                && _level < ShaderGUI.Context.FoldoutState.EditorLevel
            )
            {
                actualEditorLevel = _level;
                actualIsEditable = true;
            }

            // 跳出到指定等级
            ShaderGUI.SetFoldout(_level, actualEditorLevel, true, actualIsEditable);
        }
    }
}

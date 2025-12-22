using UnityEditor;
using UnityEngine;

namespace Scarecrow
{
    /// <summary>
    /// Vector3 属性绘制器
    /// 支持在属性面板绘制 Vector3 输入框，
    /// 并通过 Set 按钮在场景中显示旋转手柄进行直观编辑
    /// </summary>
    public class Vector3Drawer : PropertyDrawerBase
    {
        /// <summary>该控件会在以下选项中显示</summary>
        private string[] _showList = new string[0];

        /// <summary>是否总是显示</summary>
        private bool _isAlwaysShow = true;

        /// <summary>当前处理的属性</summary>
        private MaterialProperty _property;

        /// <summary>是否启用手柄编辑</summary>
        private bool _isHandleActive = false;

        /// <summary>是否首次启用手柄</summary>
        private bool _isFirstHandle = true;

        /// <summary>当前的旋转信息</summary>
        private Quaternion _rotation = Quaternion.identity;

        /// <summary>当前选中的游戏对象</summary>
        private GameObject _selectedGameObject;

        public Vector3Drawer()
        {
            _isAlwaysShow = true;
        }

        public Vector3Drawer(params string[] showList)
        {
            _showList = showList ?? new string[0];
            _isAlwaysShow = _showList.Length == 0;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            CacheShaderGUI(editor);

            if (ShaderGUI == null)
                return base.GetPropertyHeight(prop, label, editor);

            // 检查属性是否要被绘制
            if (!(_isAlwaysShow || ShaderGUI.GetShowState(_showList)))
                return -2f;

            return EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, new GUIContent(label));
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

            _property = prop;

            // 检查属性是否要被绘制
            if (!(_isAlwaysShow || ShaderGUI.GetShowState(_showList)))
                return;

            // 绘制 Vector3 输入框
            DrawVector3Control(position, label);

            // 管理场景中的旋转手柄
            ManageSceneHandle();
        }

        /// <summary>绘制 Vector3 输入框和 Set 按钮</summary>
        private void DrawVector3Control(Rect position, string label)
        {
            EditorGUI.BeginChangeCheck();
            SetMixedValueDisplay(HasMixedValue(_property));

            // 调整颜色以显示编辑状态
            Color originalColor = GUI.color;
            if (_isHandleActive)
                GUI.color = Color.green;

            // 绘制 Vector3 输入框
            Rect vectorRect = new Rect(position)
            {
                width = position.width - 68f
            };

            Vector3 value = new Vector3(_property.vectorValue.x, _property.vectorValue.y, _property.vectorValue.z);
            value = EditorGUI.Vector3Field(vectorRect, label, value);

            // 绘制 Set 按钮
            Rect buttonRect = new Rect(position)
            {
                x = position.xMax - 63f,
                y = (position.height > 20) ? position.y + 20 : position.y,
                width = 60f,
                height = 18f
            };
            _isHandleActive = GUI.Toggle(buttonRect, _isHandleActive, "Set", "Button");

            GUI.color = originalColor;
            ClearMixedValueDisplay();

            if (EditorGUI.EndChangeCheck())
            {
                _property.vectorValue = new Vector4(value.x, value.y, value.z, _property.vectorValue.w);
            }
        }

        /// <summary>管理场景中的旋转手柄的创建和销毁</summary>
        private void ManageSceneHandle()
        {
            Vector3 direction = new Vector3(_property.vectorValue.x, _property.vectorValue.y, _property.vectorValue.z);

            // 启用手柄编辑
            if (_isHandleActive && _isFirstHandle)
            {
                CreateSceneHandle(direction);
            }
            // 禁用手柄编辑
            else if (!_isHandleActive && !_isFirstHandle)
            {
                DestroySceneHandle();
            }
        }

        /// <summary>创建场景中的旋转手柄</summary>
        private void CreateSceneHandle(Vector3 direction)
        {
            Tools.current = Tool.None;
            _selectedGameObject = Selection.activeGameObject;
            _rotation = Quaternion.FromToRotation(Vector3.forward, direction);
            SceneView.duringSceneGui += OnSceneGUI;
            SceneView.RepaintAll();
            _isFirstHandle = false;
        }

        /// <summary>销毁场景中的旋转手柄</summary>
        private void DestroySceneHandle()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.RepaintAll();
            _selectedGameObject = null;
            _isFirstHandle = true;
        }

        /// <summary>场景 GUI 回调，绘制旋转手柄</summary>
        private void OnSceneGUI(SceneView sceneView)
        {
            // 检查选中对象是否改变，如果改变则停止编辑
            if (_selectedGameObject == null || _selectedGameObject != Selection.activeGameObject)
            {
                DestroySceneHandle();
                _isHandleActive = false;
                return;
            }

            Vector3 position = _selectedGameObject.transform.position;

            // 绘制旋转手柄
            _rotation = Handles.RotationHandle(_rotation, position);

            // 计算新的方向向量
            Vector3 newDirection = _rotation * Vector3.forward;

            // 更新属性值
            _property.vectorValue = new Vector4(newDirection.x, newDirection.y, newDirection.z, _property.vectorValue.w);

            // 绘制手柄中心的方向指示器
            Handles.color = Color.green;
            float handleSize = HandleUtility.GetHandleSize(position);
            Handles.ConeHandleCap(0, position, _rotation.normalized, handleSize, EventType.Repaint);
        }
    }
}

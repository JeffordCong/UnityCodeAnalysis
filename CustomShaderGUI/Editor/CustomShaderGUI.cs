
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;


namespace Rendering.Editor
{

    public class CustomShaderGUI : ShaderGUI
    {
        private ShaderGUIViewModel _viewModel;
        private Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        private Dictionary<Type, Action<MaterialEditor, BasePropertyViewModel>> _propertyDrawers;

        #region Styles
        // private static GUIStyle s_groupHeaderStyle;
        private static GUIStyle s_foldoutStyle;
        private static GUIStyle s_groupToggleStyle;
        private static GUIStyle s_groupButtonStyle;
        private static GUIStyle s_helpBoxStyle;
        private static bool s_stylesInitialized = false;
        #endregion

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Material material = materialEditor.target as Material;
            Initialize(material);
            InitStyles();

            DrawRenderStateUI(materialEditor);

            foreach (var group in _viewModel.PropertyGroups)
            {
                bool isExpanded = DrawGroupHeader(materialEditor, group);
                if (isExpanded)
                {
                    DrawGroupContent(materialEditor, group.Properties, !group.IsHeaderlessGroup);
                }
            }
            EditorGUILayout.Space(20);
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
        }

        private void Initialize(Material material)
        {
            if (_viewModel == null)
            {
                _viewModel = new ShaderGUIViewModel(material);

                _propertyDrawers = new Dictionary<Type, Action<MaterialEditor, BasePropertyViewModel>>
              {
                {
                    typeof(TexPropertyViewModel), (editor, vm) =>
                    editor.TexturePropertySingleLine(new GUIContent(vm.Property.displayName), vm.Property)
                },
              };
            }
        }



        private void DrawDefaultProperty(MaterialEditor editor, BasePropertyViewModel vm)
        {
            editor.ShaderProperty(vm.Property, vm.Property.displayName);
        }



        private bool DrawGroupHeader(MaterialEditor editor, PropertyGroup group)
        {
            if (group.IsHeaderlessGroup)
                return true;

            EditorGUILayout.Space(2);
            if (group.IsToggleGroup)
            {
                EditorGUI.BeginChangeCheck();
                bool currentState = group.HeaderProperty.floatValue > 0.5f;

                bool newState = EditorGUILayout.ToggleLeft(group.HeaderProperty.displayName, currentState, s_groupToggleStyle);

                if (EditorGUI.EndChangeCheck())
                {
                    editor.RegisterPropertyChangeUndo("Toggle " + group.HeaderProperty.displayName);
                    group.HeaderProperty.floatValue = newState ? 1.0f : 0.0f;
                    _viewModel.ApplyToggleKeyword(group.GroupToggleKeyword, newState);
                    _viewModel = null;
                }
                return newState;
            }
            else
            {
                string groupName = group.HeaderProperty.displayName;
                if (!_foldoutStates.ContainsKey(groupName)) _foldoutStates[groupName] = true;

                if (GUILayout.Button(groupName, s_groupButtonStyle))
                {
                    // 每次点击，翻转状态
                    _foldoutStates[groupName] = !_foldoutStates[groupName];
                }
                return _foldoutStates[groupName];
            }
        }

        private void DrawGroupContent(MaterialEditor editor, List<BasePropertyViewModel> props, bool useHelpBox)
        {
            if (useHelpBox) EditorGUILayout.BeginVertical(s_helpBoxStyle);
            EditorGUI.indentLevel++;

            foreach (var propVM in props)
            {
                if (_propertyDrawers.TryGetValue(propVM.GetType(), out var drawer))
                {
                    drawer(editor, propVM);
                }
                else
                {
                    DrawDefaultProperty(editor, propVM);
                }
            }

            EditorGUI.indentLevel--;
            if (useHelpBox) EditorGUILayout.EndVertical();
        }

        private void DrawRenderStateUI(MaterialEditor materialEditor)
        {
            var rsVM = _viewModel.RenderStates;

            bool hasEnableToggle = (materialEditor.target as Material).HasProperty(ShaderPropertyIDs.S_EnableRenderStates);

            if (!hasEnableToggle)
            {
                return;
            }

            string buttonText = rsVM.IsEnabled ? "Disable Render States" : "Enable Render States";


            if (GUILayout.Button(buttonText))
            {

                materialEditor.RegisterPropertyChangeUndo("Toggle Render States");
                rsVM.IsEnabled = !rsVM.IsEnabled;
                rsVM.ApplyToMaterial();
            }

            if (!rsVM.IsEnabled) return;

            // if (!EditorGUILayout.Foldout(true, "Render States Settings", true, s_foldoutStyle))
            //     return;

            EditorGUILayout.BeginVertical(s_helpBoxStyle);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();

            rsVM.Blend = (RenderStateViewModel.BlendMode)EditorGUILayout.EnumPopup("Blend Mode", rsVM.Blend);
            rsVM.Cull = (RenderStateViewModel.RenderFace)EditorGUILayout.EnumPopup("Render Face", rsVM.Cull);
            rsVM.QueueOffset = EditorGUILayout.IntField("Queue Offset", rsVM.QueueOffset);

            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Change Render State");
                rsVM.ApplyToMaterial();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private static void InitStyles()
        {

            if (s_stylesInitialized)
                return;

            var backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0.35f, 0.35f, 0.35f, 1f));
            backgroundTexture.Apply();

            var textColor = new Color(0.85f, 0.85f, 0.85f);

            const int fontSize = 12;
            const FontStyle fontStyle = FontStyle.Bold;

            var padding = new RectOffset(5, 5, 2, 2);
            const TextAnchor alignment = TextAnchor.MiddleLeft;


            s_helpBoxStyle = new GUIStyle(EditorStyles.helpBox);

            s_groupToggleStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = fontStyle,
                fontSize = fontSize,
                alignment = alignment,
                padding = padding
            };
            s_groupToggleStyle.normal.background = backgroundTexture;
            s_groupToggleStyle.normal.textColor = textColor;

            // GroupBase
            s_groupButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = fontStyle,
                fontSize = fontSize,
                alignment = alignment,
                padding = padding
            };
            s_groupButtonStyle.normal.background = backgroundTexture;
            s_groupButtonStyle.normal.textColor = textColor;

            // 原生 Foldout 样式 (仅统一字体，以防未来使用)
            s_foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = fontStyle,
                fontSize = fontSize
            };

            // 标记为已初始化
            s_stylesInitialized = true;
        }
    }
}

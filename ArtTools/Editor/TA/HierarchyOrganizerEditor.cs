

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

namespace CustomEditorTools.TA
{

    /// <summary>
    /// Hierarchy整理器编辑器窗口 (带操作后自动选择功能)
    /// 底层原理：在整理操作成功执行后，利用`Selection.activeGameObject` API，
    /// 将目标父对象的GameObject赋值给它，从而实现程序化地在Hierarchy面板中选中该对象，
    /// 为用户提供即时的位置反馈。
    /// </summary>
    [FunctionCategory("TA", "Hierarchy 整理器")]
    public class HierarchyOrganizerEditor : FunctionImplementation
    {
        private enum TargetMode { FromSelection, CreateNew }
        private TargetMode _targetMode = TargetMode.FromSelection;
        private string[] _toolbarOptions = { "来自选择", "创建新对象" };

        private string _newGroupName = "New Group";

        private List<HierarchyGroupingRule> _rules;
        private Vector2 _scrollPosition;
        private GameObject _selectedTarget;

        private Dictionary<HierarchyGroupingRule, List<GameObject>> _ruleMatchCache;
        private Dictionary<HierarchyGroupingRule, bool> _foldoutStates;
        private bool _isCacheDirty = true;

        private Dictionary<string, Transform> _groupTransformCache = new Dictionary<string, Transform>();

        private GUIStyle _boxStyle;

        /*
                [MenuItem("Tools/Hierarchy 整理器")]
                public static void ShowWindow()
                {
                    var window = GetWindow<HierarchyOrganizerEditor>("Hierarchy 整理器");
                    window.minSize = new Vector2(370, 600);
                    window.maxSize = new Vector2(370, 600);
                }
        */
        public override void Initialize()
        {
            if (_rules == null) _rules = new List<HierarchyGroupingRule>();
            if (_ruleMatchCache == null) _ruleMatchCache = new Dictionary<HierarchyGroupingRule, List<GameObject>>();
            if (_foldoutStates == null) _foldoutStates = new Dictionary<HierarchyGroupingRule, bool>();

            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            MarkCacheAsDirty();
        }

        public override void Dispose()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnHierarchyChanged()
        {
            _groupTransformCache.Clear();
            MarkCacheAsDirty();
            Repaint();
        }

        private void MarkCacheAsDirty()
        {
            _isCacheDirty = true;
        }

        private void InitStyles()
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }

        public override void DrawGUI()
        {
            InitStyles();

            if (_isCacheDirty)
            {
                RefreshRuleMatchCache();
            }

            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });

            EditorGUILayout.BeginVertical(_boxStyle);
            _targetMode = (TargetMode)GUILayout.Toolbar((int)_targetMode, _toolbarOptions);
            EditorGUILayout.Space(5);

            switch (_targetMode)
            {
                case TargetMode.FromSelection:
                    _selectedTarget = Selection.activeGameObject;
                    EditorGUILayout.ObjectField("当前目标", _selectedTarget, typeof(GameObject), true);
                    if (_selectedTarget == null)
                    {
                        EditorGUILayout.HelpBox("请在Hierarchy面板中选择一个对象作为父级。", MessageType.Info);
                    }
                    break;
                case TargetMode.CreateNew:
                    _newGroupName = EditorGUILayout.TextField("新对象命名", _newGroupName);
                    if (string.IsNullOrWhiteSpace(_newGroupName))
                    {
                        EditorGUILayout.HelpBox("请输入一个有效的对象名称。", MessageType.Warning);
                    }
                    break;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("整理规则", EditorStyles.boldLabel);
            if (GUILayout.Button("刷新预览", GUILayout.Width(80))) MarkCacheAsDirty();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
            EditorGUI.BeginChangeCheck();
            if (_rules.Count == 0) EditorGUILayout.LabelField("点击下方按钮添加第一条规则。", EditorStyles.centeredGreyMiniLabel);
            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                rule.IsEnabled = EditorGUILayout.Toggle(rule.IsEnabled, GUILayout.Width(20));
                EditorGUIUtility.labelWidth = 80;
                rule.ObjectName = EditorGUILayout.TextField("对象名包含", rule.ObjectName);
                EditorGUIUtility.labelWidth = 0;
                if (GUILayout.Button("-", new GUIStyle("ToolbarButton") { normal = { textColor = Color.red } }, GUILayout.Width(25)))
                {
                    _rules.RemoveAt(i);
                    i--;
                    MarkCacheAsDirty();
                    continue;
                }
                EditorGUILayout.EndHorizontal();
                DrawMatchesPreview(rule);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }
            if (EditorGUI.EndChangeCheck()) MarkCacheAsDirty();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(5);
            if (GUILayout.Button("添加新规则", GUILayout.Height(25)))
            {
                var newRule = new HierarchyGroupingRule("NewObjectName");
                _rules.Add(newRule);
                _foldoutStates[newRule] = false;
                MarkCacheAsDirty();
            }
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical(_boxStyle);
            bool isActionReady = (_targetMode == TargetMode.FromSelection && _selectedTarget != null) ||
                                 (_targetMode == TargetMode.CreateNew && !string.IsNullOrWhiteSpace(_newGroupName));
            GUI.enabled = isActionReady;
            if (GUILayout.Button("执行整理", GUILayout.Height(40)))
            {
                OrganizeHierarchy();
            }
            GUI.enabled = true;
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 核心整理逻辑，现在会在整理完毕后自动选中父对象
        /// </summary>
        private void OrganizeHierarchy()
        {
            Transform targetParent = null;

            switch (_targetMode)
            {
                case TargetMode.FromSelection:
                    if (_selectedTarget != null)
                    {
                        targetParent = _selectedTarget.transform;
                    }
                    break;
                case TargetMode.CreateNew:
                    if (!string.IsNullOrWhiteSpace(_newGroupName))
                    {
                        targetParent = GetOrCreateGroup(_newGroupName);
                    }
                    break;
            }

            if (targetParent == null)
            {
                EditorUtility.DisplayDialog("操作失败", "未能确定有效的父对象。请检查您的选择或输入。", "确定");
                return;
            }

            RefreshRuleMatchCache();
            foreach (var rule in _rules)
            {
                if (!_ruleMatchCache.TryGetValue(rule, out var objectsToMove) || objectsToMove.Count == 0) continue;

                foreach (var obj in objectsToMove)
                {
                    if (obj == null) continue;
                    var trans = obj.transform;
                    if (trans.parent == targetParent || trans == targetParent || targetParent.IsChildOf(trans))
                    {
                        continue;
                    }
                    Undo.SetTransformParent(trans, targetParent, "Organize Hierarchy");
                }
            }
            Debug.Log($"Hierarchy 整理完成！对象已移动到 '{targetParent.name}' 之下。");

            // --- 核心修改点: 整理完毕后，自动选中父对象 ---
            Selection.activeGameObject = targetParent.gameObject;
        }

        private Transform GetOrCreateGroup(string name)
        {
            if (_groupTransformCache.TryGetValue(name, out Transform parent) && parent != null)
            {
                return parent;
            }

            GameObject groupGo = GameObject.Find(name);
            if (groupGo == null)
            {
                groupGo = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(groupGo, $"Create Group: {name}");
            }

            _groupTransformCache[name] = groupGo.transform;
            return groupGo.transform;
        }

        private void DrawMatchesPreview(HierarchyGroupingRule rule)
        {
            if (_ruleMatchCache.TryGetValue(rule, out var matches) && matches.Count > 0)
            {
                if (!_foldoutStates.ContainsKey(rule)) _foldoutStates[rule] = false;
                _foldoutStates[rule] = EditorGUILayout.Foldout(_foldoutStates[rule], $"匹配到 {matches.Count} 个对象", true);
                if (_foldoutStates[rule])
                {
                    EditorGUI.indentLevel++;
                    int displayCount = Mathf.Min(matches.Count, 50);
                    for (int i = 0; i < displayCount; i++) EditorGUILayout.ObjectField(matches[i], typeof(GameObject), true);
                    if (matches.Count > 50) EditorGUILayout.LabelField($"...等另外 {matches.Count - 50} 个对象", EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }
            }
            else if (rule.IsEnabled && !string.IsNullOrEmpty(rule.ObjectName))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("未匹配到任何对象", EditorStyles.centeredGreyMiniLabel);
                EditorGUI.indentLevel--;
            }
        }
        private void RefreshRuleMatchCache()
        {
            _ruleMatchCache.Clear();
            var allTransforms = GetAllSceneTransforms();
            foreach (var rule in _rules)
            {
                if (!rule.IsEnabled || string.IsNullOrEmpty(rule.ObjectName))
                {
                    _ruleMatchCache[rule] = new List<GameObject>();
                    continue;
                }
                var foundObjects = allTransforms
                    .Where(t => t != null && t.name.IndexOf(rule.ObjectName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(t => t.gameObject).ToList();
                _ruleMatchCache[rule] = foundObjects;
            }
            _isCacheDirty = false;
        }
        private List<Transform> GetAllSceneTransforms()
        {
            var allTransforms = new List<Transform>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects()) allTransforms.AddRange(root.GetComponentsInChildren<Transform>(true));
            }
            return allTransforms;
        }
    }

    /// <summary>
    /// 定义单条Hierarchy整理规则 (选择驱动版)
    /// 底层原理：数据模型被简化，只包含匹配对象的名称，因为目标父对象将由用户的实时选择决定。
    /// </summary>
    [Serializable]
    public class HierarchyGroupingRule
    {
        [Tooltip("要查找的对象名称（支持部分匹配）")]
        public string ObjectName;

        [Tooltip("是否启用此规则")]
        public bool IsEnabled = true;

        public HierarchyGroupingRule(string objectName)
        {
            ObjectName = objectName;
            IsEnabled = true;
        }
    }
}
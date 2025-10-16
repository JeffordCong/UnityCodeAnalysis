
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Reflection;

namespace EditorTools
{
    public class FastOpenWin : EditorWindow
    {
        private int _selectedTab = 0;
        private readonly string[] _tabLabels = { "Scenes", "Folders" };

        private AssetView<SceneAsset, SceneAssetItem> _sceneView;
        private AssetView<DefaultAsset, FolderAssetItem> _folderView;

        [MenuItem("Tools/FastOpenWin &S")]
        public static void OpenWindow()
        {
            var window = GetWindow<FastOpenWin>();
            window.titleContent = new GUIContent("Fast Open Win");
            window.Show();
        }

        private void OnEnable()
        {
            _sceneView = new AssetView<SceneAsset, SceneAssetItem>();
            _folderView = new AssetView<DefaultAsset, FolderAssetItem>();
            AssetCollectionManager.OnCollectionChanged += OnCollectionChanged;
        }

        private void OnDisable()
        {
            AssetCollectionManager.OnCollectionChanged -= OnCollectionChanged;
        }

        private void OnCollectionChanged()
        {
            _sceneView.ReloadItems();
            _folderView.ReloadItems();
            Repaint();
        }

        private void OnGUI()
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabLabels);
            EditorGUILayout.Space(5);

            switch (_selectedTab)
            {
                case 0:
                    _sceneView.Draw();
                    break;
                case 1:
                    _folderView.Draw();
                    break;
            }
        }
    }

    public class AssetView<TAsset, TItem> where TAsset : UnityEngine.Object where TItem : IAssetItem
    {
        private string _searchContent = "";
        private AssetItemListView _listView = new AssetItemListView();
        private List<TItem> _allItems = new List<TItem>();
        private List<IAssetItem> _filteredItems = new List<IAssetItem>();
        private int _pickerControlId = -1;

        public AssetView()
        {
            ReloadItems();
        }

        public void ReloadItems()
        {
            _allItems = AssetCollectionManager.GetAssets<TAsset>()
                .Where(asset => asset != null)
                .Select(asset => (TItem)Activator.CreateInstance(typeof(TItem), new object[] { asset }))
                .ToList();
            FilterItems();
        }

        public void Draw()
        {
            DrawSearchBar();
            _listView.Draw(_filteredItems);
            DrawBottomButtons();
            HandleObjectPicker();
        }

        private void DrawSearchBar()
        {
            EditorGUI.BeginChangeCheck();
            _searchContent = EditorGUILayout.TextField("Search", _searchContent);
            if (EditorGUI.EndChangeCheck())
            {
                FilterItems();
            }
        }

        private void FilterItems()
        {
            string searchLower = _searchContent.ToLower();
            if (string.IsNullOrEmpty(searchLower))
            {
                _filteredItems = _allItems.Cast<IAssetItem>().ToList();
            }
            else
            {
                _filteredItems = _allItems.Where(item => item.SearchableName.Contains(searchLower))
                                         .Cast<IAssetItem>()
                                         .ToList();
            }
        }

        private void DrawBottomButtons()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            {
                ReloadItems();
            }

            if (typeof(TAsset) == typeof(DefaultAsset))
            {
                if (GUILayout.Button("Add Selected Folder", GUILayout.Width(140)))
                {
                    AddSelectedFolder();
                }
            }
            else
            {
                if (GUILayout.Button($"Add {typeof(TAsset).Name}", GUILayout.Width(120)))
                {
                    _pickerControlId = EditorGUIUtility.GetControlID(FocusType.Passive) + 100;
                    EditorGUIUtility.ShowObjectPicker<TAsset>(null, false, "", _pickerControlId);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void HandleObjectPicker()
        {
            if (Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == _pickerControlId)
            {
                var pickedObject = EditorGUIUtility.GetObjectPickerObject();
                if (pickedObject is TAsset asset)
                {
                    AssetCollectionManager.AddAsset(asset);
                }
                _pickerControlId = -1;
                Event.current.Use();
            }
        }

        private void AddSelectedFolder()
        {
            var selection = Selection.activeObject;
            if (selection == null)
            {
                Debug.LogWarning("No folder selected in the Project window.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(selection);
            if (AssetDatabase.IsValidFolder(path))
            {
                var folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                if (folderAsset != null)
                {
                    AssetCollectionManager.AddAsset(folderAsset);
                }
            }
            else
            {
                Debug.LogWarning("The selected item is not a folder.");
            }
        }
    }

    public class AssetItemListView
    {
        private Vector2 _scrollPosition;
        public void Draw(List<IAssetItem> items)
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                if (items == null) return;
                foreach (var item in items)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    EditorGUILayout.LabelField(item.DisplayName);
                    GUILayout.FlexibleSpace();
                    item.DrawActionButtons();
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }

    public interface IAssetItem
    {
        string DisplayName { get; }
        string SearchableName { get; }
        UnityEngine.Object AssetObject { get; }
        void DrawActionButtons();
    }

    public class SceneAssetItem : IAssetItem
    {
        private readonly SceneAsset _sceneAsset;
        public string DisplayName => _sceneAsset.name;
        public string SearchableName { get; }
        public UnityEngine.Object AssetObject => _sceneAsset;
        public SceneAssetItem(SceneAsset sceneAsset) { _sceneAsset = sceneAsset; SearchableName = _sceneAsset.name.ToLower(); }
        public void DrawActionButtons()
        {
            if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(50))) { EditorGUIUtility.PingObject(_sceneAsset); }
            if (GUILayout.Button("Open", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(_sceneAsset), OpenSceneMode.Single);
                }
            }
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f, 1f);
            if (GUILayout.Button("Remove", EditorStyles.toolbarButton, GUILayout.Width(60))) { AssetCollectionManager.RemoveAsset(_sceneAsset); }
            GUI.backgroundColor = originalColor;
        }
    }

    public class FolderAssetItem : IAssetItem
    {
        private readonly DefaultAsset _folderAsset;
        private readonly string _assetPath;
        public string DisplayName { get; }
        public string SearchableName { get; }
        public UnityEngine.Object AssetObject => _folderAsset;
        public FolderAssetItem(DefaultAsset folderAsset)
        {
            _folderAsset = folderAsset;
            _assetPath = AssetDatabase.GetAssetPath(folderAsset);
            DisplayName = Path.GetFileName(_assetPath);
            SearchableName = DisplayName.ToLower();
        }
        public void DrawActionButtons()
        {
            // --- vvvvvvvvvvvvvvvvvv 修改点 vvvvvvvvvvvvvvvvvv ---
            if (GUILayout.Button("Enter", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                // 调用新的辅助方法来进入文件夹
                ProjectWindowUtil.EnterFolderInProjectWindow(_folderAsset);
            }
            // --- ^^^^^^^^^^^^^^^^^^ 修改点 ^^^^^^^^^^^^^^^^^^ ---

            if (GUILayout.Button("Reveal", EditorStyles.toolbarButton, GUILayout.Width(60))) { EditorUtility.RevealInFinder(_assetPath); }
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f, 1f);
            if (GUILayout.Button("Remove", EditorStyles.toolbarButton, GUILayout.Width(60))) { AssetCollectionManager.RemoveAsset(_folderAsset); }
            GUI.backgroundColor = originalColor;
        }
    }

    // --- vvvvvvvvvvvvvvvvvv 新增的辅助类 vvvvvvvvvvvvvvvvvv ---
    public static class ProjectWindowUtil
    {
        public static void EnterFolderInProjectWindow(DefaultAsset folderAsset)
        {
            if (folderAsset == null) return;

            // 步骤 1: 确保Project窗口是打开且聚焦的，并高亮文件夹
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(folderAsset);

            try
            {
                // 步骤 2: 使用反射来调用内部方法进入文件夹
                var projectBrowserType = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor.dll");
                if (projectBrowserType == null) return;

                var projectBrowserWindow = EditorWindow.GetWindow(projectBrowserType);
                if (projectBrowserWindow == null) return;

                var showFolderContents = projectBrowserType.GetMethod(
                    "ShowFolderContents",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new[] { typeof(int), typeof(bool) },
                    null);

                if (showFolderContents != null)
                {
                    showFolderContents.Invoke(projectBrowserWindow, new object[] { folderAsset.GetInstanceID(), true });
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to enter folder using reflection. It might be due to a Unity version update.\n" + e);
            }
        }
    }
    // --- ^^^^^^^^^^^^^^^^^^ 新增的辅助类 ^^^^^^^^^^^^^^^^^^ ---
}
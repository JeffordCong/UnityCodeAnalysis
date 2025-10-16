// File: AssetCollectionManager.cs

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

namespace AssetBrowser
{
    [InitializeOnLoad]
    public static class AssetCollectionManager
    {
        public static event Action OnCollectionChanged;

        private static AssetCollection collection;
        public static AssetCollection Collection
        {
            get
            {
                if (collection == null)
                {
                    LoadCollection();
                }
                return collection;
            }
        }

        static AssetCollectionManager()
        {
            LoadCollection();
        }

        private static void LoadCollection()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(AssetCollection)}");
            if (guids.Length == 0)
            {
                // --- vvvvvvvvvvvvvvvvvv 修改点 vvvvvvvvvvvvvvvvvv ---
                Debug.LogWarning("AssetCollection.asset not found. Attempting to create it automatically.");
                collection = ScriptableObject.CreateInstance<AssetCollection>();

                string[] managerGuids = AssetDatabase.FindAssets($"t:Script {nameof(AssetCollectionManager)}");
                string saveDirectory = "Assets";

                if (managerGuids.Length > 0)
                {
                    string scriptPath = AssetDatabase.GUIDToAssetPath(managerGuids[0]);
                    saveDirectory = Path.GetDirectoryName(scriptPath);
                }
                else
                {
                    Debug.LogError($"Could not find the script path for {nameof(AssetCollectionManager)}. The .asset file will be created in the root Assets directory.");
                }

                string assetPath = Path.Combine(saveDirectory, "AssetCollection.asset");
                AssetDatabase.CreateAsset(collection, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Successfully created AssetCollection.asset at: {assetPath}");
                // --- ^^^^^^^^^^^^^^^^^^ 修改点 ^^^^^^^^^^^^^^^^^^ ---
            }
            else
            {
                if (guids.Length > 1)
                {
                    Debug.LogWarning("Multiple AssetCollection assets found. Using the first one. Please consider deleting duplicates.");
                }
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                collection = AssetDatabase.LoadAssetAtPath<AssetCollection>(path);
            }
        }

        public static List<T> GetAssets<T>() where T : UnityEngine.Object
        {
            return Collection.assets.OfType<T>().ToList();
        }

        public static void AddAsset(UnityEngine.Object asset)
        {
            if (asset != null && !Collection.assets.Contains(asset))
            {
                Collection.assets.Add(asset);
                EditorUtility.SetDirty(Collection);
                OnCollectionChanged?.Invoke();
            }
        }

        public static void RemoveAsset(UnityEngine.Object asset)
        {
            if (asset != null && Collection.assets.Contains(asset))
            {
                Collection.assets.Remove(asset);
                EditorUtility.SetDirty(Collection);
                OnCollectionChanged?.Invoke();
            }
        }
    }
}
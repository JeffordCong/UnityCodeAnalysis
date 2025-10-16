// File: AssetCollection.cs

using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace AssetBrowser
{
    [CreateAssetMenu(fileName = "AssetCollection.asset", menuName = "Tools/AssetBrowser/Asset Collection")]
    public class AssetCollection : ScriptableObject
    {
        [SerializeField]
        public List<Object> assets = new List<Object>();
    }
}
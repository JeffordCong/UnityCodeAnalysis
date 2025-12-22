using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Scarecrow
{
    /// <summary>
    /// Context holding the state of the ShaderGUI.
    /// </summary>
    public class ShaderGUIContext
    {
        public MaterialEditor MaterialEditor { get; private set; }
        public MaterialProperty[] Properties { get; private set; }
        public Material Material { get; private set; }

        public FoldoutState FoldoutState { get; private set; } = new FoldoutState();
        public List<string> SwitchList { get; private set; } = new List<string>();
        public RenderStateManager RenderStateManager { get; private set; } = new RenderStateManager();

        public void Initialize(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialEditor = materialEditor;
            Properties = properties;
            Material = materialEditor.target as Material;

            FoldoutState.Reset();
            SwitchList.Clear();
            EditorGUI.indentLevel = 0;
        }

        public MaterialProperty FindProperty(string name)
        {
            return PropertyDrawHelper.FindPropertyByName(name, Properties);
        }
    }
}

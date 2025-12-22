using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Scarecrow
{
    /// <summary>
    /// Interface for SimpleShaderGUI to decouple drawers from the concrete class.
    /// </summary>
    public interface ISimpleShaderGUI
    {
        /// <summary>
        /// The context holding the state of the ShaderGUI.
        /// </summary>
        ShaderGUIContext Context { get; }

        /// <summary>
        /// Set the foldout state.
        /// </summary>
        void SetFoldout(int level, int editorLevel, bool isOpen, bool isEditable = true);

        /// <summary>
        /// Check if a property should be shown based on the switch list.
        /// </summary>
        bool GetShowState(string[] showList);

        /// <summary>
        /// Find a material property by name.
        /// </summary>
        MaterialProperty FindProperty(string name);
    }
}

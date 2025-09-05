using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GPUAnimationLitShaderGUI : GPUAnimationUnlitShaderGUI
{


    //法线贴图
    private MaterialProperty normalMap;
    private MaterialProperty bumpScale;

    private MaterialProperty _Glossiness;
    private MaterialProperty _Specular;


    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        this.FindProperties(properties);
        Material[] array = new Material[materialEditor.targets.Length];
        for (int i = 0; i < materialEditor.targets.Length; i++)
        {
            array[i] = (Material)materialEditor.targets[i];
        }

        this.OnShaderGUI(materialEditor, array);

    }


    protected override void FindProperties(MaterialProperty[] props)
    {

        base.FindProperties(props);
        this.normalMap = ShaderGUI.FindProperty("_NormalMap", props);
        this.bumpScale = ShaderGUI.FindProperty("_BumpScale", props);

        this._Glossiness = ShaderGUI.FindProperty("_Glossiness", props);
        this._Specular = ShaderGUI.FindProperty("_Specular", props);


    

    }
    protected override void OnShaderGUI(MaterialEditor materialEditor, Material[] materials)
    {
        this.DoMainGUI(materialEditor, materials);
        this.DoNormalsGUI(materialEditor, materials);
        this.DoRimGUI(materialEditor, materials);
        this.OnSpecularLightGUI(materialEditor, materials);
        this.DoGPUAnimationGUI(materialEditor, materials);

        GUILayout.Space(10);
        GUILayout.Label("Advanced Options", EditorStyles.boldLabel);
        materialEditor.EnableInstancingField();

    }


    private void DoNormalsGUI(MaterialEditor materialEditor, Material[] materials)
    {
        GUILayout.Space(2);
        EditorGUI.BeginChangeCheck();
        materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), this.normalMap, this.normalMap.textureValue ? this.bumpScale : null);
        if (EditorGUI.EndChangeCheck())
        {
            SetKeyword((materialEditor.target as Material), "_NORMALMAP", this.normalMap.textureValue);
        }
    }

    private void OnSpecularLightGUI(MaterialEditor materialEditor, Material[] materials)
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("高光", EditorStyles.boldLabel);
        if (this.CheckOption(
                materials,
                "Specular Highlights",
                "_SPECULARHIGHLIGHTS_ON"))
        {
            EditorGUI.indentLevel = 1;

            materialEditor.ShaderProperty(_Glossiness,"Glass int");
            materialEditor.ShaderProperty(_Specular,"Specular Color");
            EditorGUI.indentLevel = 0;
        }
    }


}

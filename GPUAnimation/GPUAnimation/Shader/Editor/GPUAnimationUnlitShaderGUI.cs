using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GPUAnimationUnlitShaderGUI : ShaderGUI
{
    private MaterialProperty mainTex;
    private MaterialProperty mainColor;

    //闪白
    private MaterialProperty rimColor;
    private MaterialProperty rimTime;
    private MaterialProperty animTex;

    private MaterialProperty _AnimFrameBegin,_AnimFrameEnd;

    private MaterialProperty animFrameInterpolate,_VertexCount;


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


    protected virtual void FindProperties(MaterialProperty[] props)
    {
       
        this.mainTex = ShaderGUI.FindProperty("_MainTex", props);
        this.mainColor = ShaderGUI.FindProperty("_Color", props);

        //闪白
        this.rimColor = ShaderGUI.FindProperty("_RimColor", props);
        this.rimTime = ShaderGUI.FindProperty("_RimTime", props);

        this.animTex = ShaderGUI.FindProperty("_AnimTex", props);
        this._AnimFrameBegin = ShaderGUI.FindProperty("_AnimFrameBegin", props);
        this._AnimFrameEnd = ShaderGUI.FindProperty("_AnimFrameEnd", props);
        this.animFrameInterpolate = ShaderGUI.FindProperty("_AnimFrameInterpolate", props);
        this._VertexCount = ShaderGUI.FindProperty("_VertexCount", props);

    }
    protected virtual void OnShaderGUI(MaterialEditor materialEditor, Material[] materials)
    {

        this.DoMainGUI(materialEditor, materials);
        this.DoGPUAnimationGUI(materialEditor, materials);
        this.DoRimGUI(materialEditor, materials);



        GUILayout.Space(10);
        GUILayout.Label("Advanced Options", EditorStyles.boldLabel);
        materialEditor.EnableInstancingField();
    }

    protected void DoMainGUI(MaterialEditor materialEditor, Material[] materials)
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Main Map", EditorStyles.boldLabel);

        materialEditor.TexturePropertySingleLine(new GUIContent("Main Texture", "Albedo (RGB) and Alpha"), this.mainTex, this.mainColor);
        GUILayout.Space(2);
    }

    protected void DoGPUAnimationGUI(MaterialEditor materialEditor, Material[] materials)
    {
        GUILayout.Space(10);
        GUI.color = Color.yellow;
        EditorGUILayout.LabelField("GPU Animation", EditorStyles.boldLabel);
        GUI.color = Color.white;
        materialEditor.TexturePropertySingleLine(new GUIContent("Animation Texture "), this.animTex);
        GUILayout.Space(2);
        materialEditor.ShaderProperty(this._VertexCount, "Anim VertexCount");
        materialEditor.ShaderProperty(this._AnimFrameBegin, "AnimFrame Begin");
        materialEditor.ShaderProperty(this._AnimFrameEnd, "AnimFrame End");
        materialEditor.ShaderProperty(this.animFrameInterpolate, "Frame Interpolate");
    }



    protected void DoRimGUI(MaterialEditor materialEditor, Material[] materials)
    {
        GUILayout.Space(10);
        GUI.color = Color.yellow;
        EditorGUILayout.LabelField("闪白", EditorStyles.boldLabel);
        GUI.color = Color.white;
        materialEditor.ColorProperty(this.rimColor, "Color");
        materialEditor.RangeProperty(this.rimTime, "TimeSpeed");
    }

    protected void SetKeyword(Material mat, string keyword, bool state)
    {
        if (state) mat.EnableKeyword(keyword);
        else mat.DisableKeyword(keyword);
    }
    protected bool CheckOption(Material[] materials, string content, string key)
    {
        return this.CheckOption(materials, new GUIContent(content), key);
    }

    protected bool CheckOption(Material[] materials, GUIContent content, string key)
    {
        bool flag = this.HasKeyword(materials, key);
        EditorGUI.BeginChangeCheck();
        flag = EditorGUILayout.ToggleLeft(content, flag, new GUILayoutOption[0]);
        if (EditorGUI.EndChangeCheck())
        {
            if (flag)
            {
                foreach (Material material in materials)
                {
                    material.EnableKeyword(key);
                }
            }
            else
            {
                foreach (Material material2 in materials)
                {
                    material2.DisableKeyword(key);
                }
            }
        }
        return flag;
    }

    protected bool HasKeyword(Material[] materials, string key)
    {
        return Array.IndexOf<string>(materials[0].shaderKeywords, key) != -1;
    }
}

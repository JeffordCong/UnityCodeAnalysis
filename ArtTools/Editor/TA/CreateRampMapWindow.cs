using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
namespace CustomEditorTools.TA
{
[FunctionCategory("TA", "RampMap创建工具")]
    public class CreateRampMapWindow : FunctionImplementation
    {
        private enum SizeType
        {
            _32X32 = 32,
            _64X64 = 64,
            _128X128 = 128,
            _256X256 = 256,
            _512X512 = 512,
            _1024X1024 = 1024,
        }
        private List<Gradient> gradientList;
        private SizeType sType = SizeType._128X128;

/*
        [MenuItem("TA/CreateRampMapTool", false)]
        public static void ShowWindow()
        {
            var win = EditorWindow.GetWindow<CreateRampMapWindow>();
            win.titleContent = new GUIContent("CreateRampMap");
            win.maxSize = new Vector2(300, 230);
            win.Show();
        }
*/
        public override void Initialize()
        {
            if (gradientList == null)
            {
                gradientList = new List<Gradient>();
                gradientList.Add(new Gradient());
            }
        }

         public override void DrawGUI()
        {
            GUILayout.BeginVertical();
            for (int i = 0; i < gradientList.Count; i++)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(10));
                var itemGradient = gradientList[i];
                EditorGUILayout.GradientField(itemGradient, GUILayout.Width(240));
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    gradientList.Insert(i + 1, new Gradient());
                }

                if (gradientList.Count > 1 && GUILayout.Button("-", GUILayout.Width(20)))
                {
                    gradientList.Remove(itemGradient);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            sType = (SizeType)EditorGUILayout.EnumPopup("Size", sType);

            GUILayout.Space(20);
            if (GUILayout.Button("滴滴！学生卡")) //, GUILayout.Width(80)
            {
                int length = (int)sType;
                int height = (int)length / gradientList.Count;
                int eachTotal = length * height;
                List<Color> colorList = new List<Color>();
                for (int i = gradientList.Count - 1; i >= 0; i--)
                {
                    var gradientItem = gradientList[i];
                    gradientItem.mode = GradientMode.Blend;

                    Color[] tempList = new Color[eachTotal];
                    for (int j = 0; j < eachTotal; j++)
                    {
                        float val = j * 1.0f / (eachTotal * 1.0f);
                        int col = j / height;
                        int row = j % height;
                        int index = row * length + col;
                        // Debug.LogError(index);
                        tempList[index] = (gradientItem.Evaluate(val));
                    }
                    colorList.AddRange(tempList);
                }
                CreateTextrue(length, colorList);
            }
            GUILayout.EndVertical();
        }

        private void CreateTextrue(int width, List<Color> colorList)
        {
            var RampTex = new Texture2D(width, width);
            RampTex.wrapMode = TextureWrapMode.Clamp;
            RampTex.filterMode = FilterMode.Bilinear;
            RampTex.SetPixels(colorList.ToArray());
            RampTex.Apply(); //将上面设置的像素都写入到图片中

            //保存路径，通过内置API（EditorUtility）可以打开一个对话框  标题，路径，名字,扩展名
            string path = EditorUtility.SaveFilePanel("保存纹理", "", "RampMap", "png");
            if (!string.IsNullOrEmpty(path))
            {
                //进行保存，  写入所有字节,参数：路径，字节数组编译成png
                System.IO.File.WriteAllBytes(path, RampTex.EncodeToPNG());

                //新建的 需要导入生成meta文件才能获取 assetImporter【一定要刷新 Refresh】
                AssetDatabase.ImportAsset(path);
                AssetDatabase.Refresh();

                //导入设置
                path = path.Replace(Application.dataPath, "Assets");
                TextureImporter importer = TextureImporter.GetAtPath(path) as TextureImporter;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();

                //进行编辑器刷新
                AssetDatabase.Refresh();
            }
        }
    }

}

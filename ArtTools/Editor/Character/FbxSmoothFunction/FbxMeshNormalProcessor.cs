// 文件：Assets/3rd/SmoothOutline/Editor/FbxMeshNormalProcessor.cs
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Autodesk.Fbx;
using Mikktspace.NET;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CustomEditorTools
{
    /// <summary>
    ///  FBX 网格法线平滑工具类
    /// </summary>
    public static class FbxMeshNormalProcessor
    {
        struct VertexInfo
        {
            public int vertexIndex;    // 多边形顶点在全局序列中的索引
            public FbxVector4 normal;  // 原始归一化法线
            public double weight;      // 夹角权重
        }

        /// <summary>
        /// 主入口：对选中的 FBX 资源执行法线平滑并覆盖原文件  
        /// selectionObjects：编辑器中选中的资源数组  
        /// storeUvChannel：要写入的 UV 通道索引（从 1 开始）  
        /// </summary>
        public static void FbxModelNormalSmoothTool(Object[] selectionObjects, int storeUvChannel)
        {
            if (selectionObjects == null || selectionObjects.Length < 1)
                return;

            // 创建 FBX 管理器并初始化 I/O 设置
            FbxManager fbxManager = FbxManager.Create();
            FbxIOSettings fbxIOSettings = FbxIOSettings.Create(fbxManager, Globals.IOSROOT);
            fbxManager.SetIOSettings(fbxIOSettings);

            int fbxFileCount = 0;
            int smoothedCount = 0;

            foreach (Object asset in selectionObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(assetPath) || Path.GetExtension(assetPath).ToLower() != ".fbx")
                    continue;

                // 导入 FBX
                FbxImporter fbxImporter = FbxImporter.Create(fbxManager, "");
                if (!fbxImporter.Initialize(assetPath, -1, fbxIOSettings))
                {
                    Debug.LogError(fbxImporter.GetStatus().GetErrorString());
                    fbxImporter.Destroy();
                    continue;
                }

                FbxScene fbxScene = FbxScene.Create(fbxManager, "ImportedScene");
                fbxImporter.Import(fbxScene);
                fbxImporter.Destroy();

                FbxNode rootNode = fbxScene.GetRootNode();
                if (rootNode == null) continue;

                fbxFileCount++;

                // 遍历所有 Mesh 节点并执行法线平滑
                SearchMeshNode(rootNode, storeUvChannel);

                // 导出到临时文件，避免直接覆盖失败
                string fullPath = Path.GetFullPath(assetPath);
                string tempFullPath = fullPath + ".tmp.fbx";

                FbxExporter fbxExporter = FbxExporter.Create(fbxManager, "");
                if (!fbxExporter.Initialize(tempFullPath, -1, fbxIOSettings))
                {
                    Debug.LogError(fbxExporter.GetStatus().GetErrorString());
                    fbxExporter.Destroy();
                    continue;
                }

                smoothedCount++;
                fbxExporter.Export(fbxScene);
                fbxExporter.Destroy();

                // 销毁 Manager 并刷新，让文件不被锁定
                fbxManager.Destroy();
                AssetDatabase.Refresh();

                // 删除原文件，重命名临时文件为原文件名
                try
                {
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                    File.Move(tempFullPath, fullPath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"替换 FBX 文件失败：{e.Message}");
                }

                AssetDatabase.Refresh();

                // 重新创建 FBXManager 以处理下一个文件
                fbxManager = FbxManager.Create();
                fbxIOSettings = FbxIOSettings.Create(fbxManager, Globals.IOSROOT);
                fbxManager.SetIOSettings(fbxIOSettings);
            }

            fbxManager.Destroy();
            AssetDatabase.Refresh();

            Debug.Log($"Fbx 法线平滑并覆盖完成：共处理 {fbxFileCount} 个 FBX，成功覆盖 {smoothedCount} 个。");
        }

        /// <summary>
        /// 递归查找并处理所有 Mesh 节点
        /// </summary>
        static void SearchMeshNode(FbxNode node, int storeUvChannel)
        {
            if (node == null) return;

            var attr = node.GetNodeAttribute();
            if (attr != null && attr.GetAttributeType() == FbxNodeAttribute.EType.eMesh)
            {
                SmoothMeshNode(node.GetMesh(), storeUvChannel);
            }

            for (int i = 0; i < node.GetChildCount(); i++)
                SearchMeshNode(node.GetChild(i), storeUvChannel);
        }

        /// <summary>
        /// 对单个 FbxMesh 执行法线平滑计算，并写入到指定 UV 通道
        /// </summary>
        static void SmoothMeshNode(FbxMesh fbxMesh, int storeUvChannel)
        {
            if (fbxMesh == null) return;

            int controlCount = fbxMesh.GetControlPointsCount();
            var controlLists = new List<List<VertexInfo>>(controlCount);
            for (int i = 0; i < controlCount; i++)
                controlLists.Add(new List<VertexInfo>());

            // 计算切线数组
            FbxVector4[] meshTangents = GetMeshTangents(fbxMesh);

            // 收集每个多边形顶点的法线与权重
            int globalIdx = 0;
            int polyCount = fbxMesh.GetPolygonCount();
            for (int p = 0; p < polyCount; p++)
            {
                int vs = fbxMesh.GetPolygonSize(p);
                for (int v = 0; v < vs; v++)
                {
                    int last = (v - 1 + vs) % vs;
                    int next = (v + 1) % vs;

                    int ci = fbxMesh.GetPolygonVertex(p, v);
                    int ciLast = fbxMesh.GetPolygonVertex(p, last);
                    int ciNext = fbxMesh.GetPolygonVertex(p, next);

                    var cp = fbxMesh.GetControlPointAt(ci);
                    var cpLast = fbxMesh.GetControlPointAt(ciLast);
                    var cpNext = fbxMesh.GetControlPointAt(ciNext);

                    fbxMesh.GetPolygonVertexNormal(p, v, out var nrm);
                    nrm /= nrm.Length();

                    var e0 = cpLast - cp; e0 /= e0.Length();
                    var e1 = cpNext - cp; e1 /= e1.Length();
                    double w = Math.Acos(e0.DotProduct(e1));

                    controlLists[ci].Add(new VertexInfo
                    {
                        vertexIndex = globalIdx++,
                        normal = nrm,
                        weight = w
                    });
                }
            }

            // 确保 UV 通道存在
            int exist = fbxMesh.GetLayerCount();
            for (int i = 0; i < storeUvChannel - exist + 1; i++)
            {
                int li = fbxMesh.CreateLayer();
                fbxMesh.GetLayer(li).SetUVs(FbxLayerElementUV.Create(fbxMesh, ""));
            }

            // 配置目标 UV
            var layerUV = fbxMesh.GetLayer(storeUvChannel).GetUVs();
            layerUV.SetMappingMode(FbxLayerElement.EMappingMode.eByPolygonVertex);
            layerUV.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
            var uvArr = layerUV.GetDirectArray();
            uvArr.SetCount(globalIdx);

            // 写入平滑法线（八面体编码）
            for (int ci = 0; ci < controlCount; ci++)
            {
                var list = controlLists[ci];
                if (list.Count == 0) continue;

                // 计算加权平均法线
                var avg = new FbxVector4(0, 0, 0, 0);
                foreach (var vi in list) avg += vi.weight * vi.normal;
                avg /= avg.Length();

                foreach (var vi in list)
                {
                    var tan = meshTangents[vi.vertexIndex];
                    var bin = vi.normal.CrossProduct(tan) * tan.W;

                    double sx = avg.DotProduct(tan);
                    double sy = avg.DotProduct(bin);
                    double sz = avg.DotProduct(vi.normal);

                    var ts = new FbxVector4(sx, sy, sz, 0);
                    ts /= ts.Length();

                    var enc = UnitVectorToOctahedron(ts);
                    uvArr.SetAt(vi.vertexIndex, enc);
                }
            }
        }
  /// <summary>
        /// 基于 MikkTSpace 计算切线，返回长度 = 所有多边形顶点总数
        /// </summary>
        static FbxVector4[] GetMeshTangents(FbxMesh fbxMesh)
        {
            int polyCount = fbxMesh.GetPolygonCount();
            // 动态分配锯齿数组
            var polyTangents = new FbxVector4[polyCount][];
            int totalVerts = 0;
            for (int i = 0; i < polyCount; i++)
            {
                int vs = fbxMesh.GetPolygonSize(i);
                polyTangents[i] = new FbxVector4[vs];
                totalVerts += vs;
            }

            // 构建 UV 索引表
            var uvLists = new List<List<int>>(polyCount);
            var uvs0 = fbxMesh.GetLayer(0).GetUVs();
            var dirArr = uvs0.GetDirectArray();
            if (uvs0.GetReferenceMode() == FbxLayerElement.EReferenceMode.eDirect)
            {
                int c = 0;
                for (int i = 0; i < polyCount; i++)
                {
                    int vs = fbxMesh.GetPolygonSize(i);
                    var l = new List<int>(vs);
                    for (int j = 0; j < vs; j++) l.Add(c++);
                    uvLists.Add(l);
                }
            }
            else
            {
                var idxArr = uvs0.GetIndexArray();
                int c = 0;
                for (int i = 0; i < polyCount; i++)
                {
                    int vs = fbxMesh.GetPolygonSize(i);
                    var l = new List<int>(vs);
                    for (int j = 0; j < vs; j++) l.Add(idxArr.GetAt(c++));
                    uvLists.Add(l);
                }
            }

            // MikkTSpace 回调
            void getPos(int p, int v, out float x, out float y, out float z)
            {
                int ci = fbxMesh.GetPolygonVertex(p, v);
                var cpt = fbxMesh.GetControlPointAt(ci);
                x = (float)cpt.X; y = (float)cpt.Y; z = (float)cpt.Z;
            }
            void getNrm(int p, int v, out float x, out float y, out float z)
            {
                fbxMesh.GetPolygonVertexNormal(p, v, out var n);
                n /= n.Length();
                x = (float)n.X; y = (float)n.Y; z = (float)n.Z;
            }
            void getUV(int p, int v, out float u, out float w)
            {
                int idx = uvLists[p][v];
                var uv = dirArr.GetAt(idx);
                u = (float)uv.X; w = (float)uv.Y;
            }
            void setTan(int p, int v, float tx, float ty, float tz, float s)
            {
                polyTangents[p][v] = new FbxVector4(tx, ty, tz, s);
            }

            MikkGenerator.GenerateTangentSpace(
                polyCount,
                fbxMesh.GetPolygonSize,
                getPos,
                getNrm,
                getUV,
                setTan
            );

            // 扁平化
            var result = new FbxVector4[totalVerts];
            int k = 0;
            for (int i = 0; i < polyCount; i++)
                for (int j = 0; j < polyTangents[i].Length; j++)
                    result[k++] = polyTangents[i][j];

            return result;
        }

        /// <summary>
        /// 八面体编码：将切线空间单位向量压缩到 2D 坐标
        /// </summary>
        static FbxVector2 UnitVectorToOctahedron(FbxVector4 v)
        {
            double ax = Math.Abs(v.X), ay = Math.Abs(v.Y), az = Math.Abs(v.Z);
            double sum = ax + ay + az;
            var r = new FbxVector2(v.X, v.Y) / sum;
            if (v.Z < 0)
            {
                double sx = r.X >= 0 ? 1 : -1;
                double sy = r.Y >= 0 ? 1 : -1;
                r = new FbxVector2((1 - Math.Abs(r.Y)) * sx, (1 - Math.Abs(r.X)) * sy);
            }
            return r;
        }
    }

}

   
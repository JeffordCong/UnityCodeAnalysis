using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace GPUAnimationLib
{
    [CustomEditor(typeof(GPUAnimationController)),CanEditMultipleObjects]
    public class GPUAnimationControllerEditor : Editor
    {
        PreviewRenderUtility m_Preview;
        GPUAnimationController m_PreviewTarget;
        MeshRenderer m_PreviewMeshRenderer;
        GameObject m_BoundsViewer;
        MaterialPropertyBlock m_TargetBlock;
        float m_PreviewTickSpeed = 1f;
        int m_PreviewAnimIndex = 0;
        bool m_PreviewReplay = true;

        Vector2 m_RotateDelta;
        Vector3 m_CameraDirection;
        float m_CameraDistance = 8f;
        int m_testAnimIndex = 0;
        GPUAnimationController selectTarget;
        public override void OnInspectorGUI()
        {
             base.OnInspectorGUI();
            if (selectTarget == null) return;
            GUILayout.Label("SetAnimation");
            //执行选中的对象的动画
            if (selectTarget.m_Data == null)
            {
                m_testAnimIndex = EditorGUILayout.IntField("_AnimIndex:", m_testAnimIndex);
            }
            else
            {
                if (m_PreviewTarget != null)
                {
                    string[] animname = m_PreviewTarget.m_Data.m_AnimationClips.Select(p => p.name).ToArray();
                    int[] animindex = m_PreviewTarget.m_Data.m_AnimationClips.Select(p => p.id).ToArray();
                    m_testAnimIndex = EditorGUILayout.IntPopup("Anim Name", m_testAnimIndex, animname, animindex);
                }      

            }

            if (GUILayout.Button("Execute"))
            {
                selectTarget.SetAnimation(m_testAnimIndex);
            }

        }

        private void OnEnable()
        {
            selectTarget = target as GPUAnimationController;
            
            if (!HasPreviewGUI()) return;

            m_CameraDirection = Vector3.Normalize(new Vector3(0,3f,15f));
            m_Preview = new PreviewRenderUtility();
            m_Preview.camera.fieldOfView = 30.0f;
            m_Preview.camera.nearClipPlane = 0.3f;
            m_Preview.camera.farClipPlane = 1000;
            m_Preview.camera.transform.position = m_CameraDirection * m_CameraDistance;
            //在预览窗口创建一个新的对象
            m_PreviewTarget = GameObject.Instantiate(((Component)target).gameObject).GetComponent<GPUAnimationController>();
            m_Preview.AddSingleGO(m_PreviewTarget.gameObject);
            m_PreviewMeshRenderer = m_PreviewTarget.GetComponent<MeshRenderer>();
            m_TargetBlock = new MaterialPropertyBlock();
            m_PreviewTarget.transform.position = Vector3.zero;
            m_PreviewTarget.Init();
            m_PreviewTarget.SetAnimation(0);
           
            var mat = new Material(Shader.Find("UI/Unlit/Transparent"));
            mat.SetColor("_Color", new Color(1, 1, 1, 0.2f));

            m_BoundsViewer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_BoundsViewer.GetComponent<MeshRenderer>().sharedMaterial = mat;
          
            m_Preview.AddSingleGO(m_BoundsViewer.gameObject);
            m_BoundsViewer.transform.SetParent(m_PreviewTarget.transform);
            m_BoundsViewer.transform.localRotation = Quaternion.identity;
            m_BoundsViewer.transform.localScale = m_PreviewTarget.m_MeshFilter.sharedMesh.bounds.size;
            m_BoundsViewer.transform.localPosition = m_PreviewTarget.m_MeshFilter.sharedMesh.bounds.center;
            m_BoundsViewer.SetActive(false);

            EditorApplication.update += Update;

        }

        private void OnDisable()
        {
            selectTarget = null;
            if (!HasPreviewGUI()) return;

            m_Preview?.Cleanup();
            m_Preview = null;
            if (m_PreviewTarget != null) DestroyImmediate(m_PreviewTarget.gameObject);
            m_PreviewTarget = null;
            m_PreviewMeshRenderer = null;
            m_TargetBlock = null;
            EditorApplication.update -= Update;

        }


        #region Preview
        public override bool HasPreviewGUI()
        {
            if((target as GPUAnimationController).m_Data == null) return false;
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            OnPreviewInputCheck();
            PreviewGUI();
            if (m_Preview == null) return;
            m_Preview.BeginPreview(r, background);
            m_Preview.camera.Render();
            m_Preview.EndAndDrawPreview(r);
        }

        public override void OnPreviewSettings()
        {
            base.OnPreviewSettings();
            //设置预览动画Title栏上的动画名称信息
            if(m_PreviewTarget == null || m_PreviewTarget.m_Ticker == null) return;
            var param = m_PreviewTarget.m_Ticker.m_Anim;
            GUILayout.Label(string.Format("{0},Loop:{1}", param.name, param.loop ? 1 : 0));
        }

        void Update()
        {
            if(m_PreviewReplay && m_PreviewTarget.GetScale() >= 1)
            {
                m_PreviewTarget.SetTime(0);
            }
            float delatime = UTime.deltaTime;
            (target as GPUAnimationController).Tick(delatime);
            m_PreviewTarget.Tick(delatime);

            m_Preview.camera.transform.position = m_CameraDirection * m_CameraDistance;
            m_Preview.camera.transform.LookAt(m_PreviewTarget.transform);
            m_PreviewTarget.transform.rotation = Quaternion.Euler(m_RotateDelta.y, m_RotateDelta.x, 0f);
            Repaint();
        }

        /// <summary>
        /// 在预览窗口鼠标输入事件检测
        /// </summary>
        void OnPreviewInputCheck()
        {
            if (Event.current == null) return;
            if(Event.current.type == EventType.MouseDrag)
            {
                m_RotateDelta += Event.current.delta;
            }

            if(Event.current.type == EventType.ScrollWheel)
            {
                m_CameraDistance = Mathf.Clamp(m_CameraDistance + Event.current.delta.y * .2f, 0, 20f);
            }
        }

        void PreviewGUI()
        {

            if (m_PreviewTarget == null) return;
            var m_Data = m_PreviewTarget.m_Data;
            var anims = new string[m_Data.m_AnimationClips.Length];
            for(int i =0; i < anims.Length; i++)
            {
                anims[i] = m_Data.m_AnimationClips[i].name;//.Substring(m_Data.m_AnimationClips[i].name.LastIndexOf("_") + 1);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Play:");
            m_PreviewAnimIndex = GUILayout.SelectionGrid(m_PreviewAnimIndex, anims, anims.Length>5?5:anims.Length);
            if(m_PreviewTarget.m_Ticker.m_AnimIndex != m_PreviewAnimIndex)
            {   
                m_PreviewTarget.SetAnimation(m_PreviewAnimIndex);
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed:");
            m_PreviewTickSpeed = GUILayout.HorizontalSlider(m_PreviewTickSpeed, 0f, 3f);
            GUILayout.Label("Replay:");
            m_PreviewReplay = GUILayout.Toggle(m_PreviewReplay, "");
            GUILayout.Label("Bounds:");
            m_BoundsViewer.SetActive(GUILayout.Toggle(m_BoundsViewer.activeSelf, ""));
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }



#endregion

    }
}
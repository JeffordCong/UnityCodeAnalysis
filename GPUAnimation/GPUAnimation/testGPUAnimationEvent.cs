using System.Collections;
using System.Collections.Generic;
using GPUAnimationLib;
using UnityEngine;

public class testGPUAnimationEvent : MonoBehaviour
{
    GPUAnimationController gPUAnimationController;
    // Start is called before the first frame update
    void Start()
    {
        gPUAnimationController = GetComponent<GPUAnimationController>();
        gPUAnimationController.OnAnimEvent = (funcname) =>
        {
            Debug.Log("---Animation Event: " + funcname);
        };

    }

    void Update()
    {
        gPUAnimationController?.Tick(Time.deltaTime);
     
    }
    //void OnGUI()
    //{
    //    if (GUILayout.Button("Atk"))
    //    {
    //        gPUAnimationController.SetAnimation(2);
    //    }
    //    if (GUILayout.Button("Hit"))
    //    {

    //        gPUAnimationController.EnableHit();
    //    }


    //}
    
}

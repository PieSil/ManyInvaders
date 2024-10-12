using Mediapipe;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitGlog : MonoBehaviour
{
    private void Awake() {
        Glog.Initialize("MediaPipeUnityPlugin");
        Glog.Logtostderr = true; // when true, log will be output to `Editor.log` / `Player.log` 
        Glog.Minloglevel = 0; // output INFO logs
        Glog.V = 3; // output more verbose logs
    }

    private void OnDestroy() {
        Glog.Shutdown();
    }
}

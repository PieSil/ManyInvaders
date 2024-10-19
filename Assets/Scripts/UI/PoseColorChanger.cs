using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoseColorChanger : MonoBehaviour
{
    [SerializeField] private HandPoseDetection _poseDetector;
    private Image _image;

    private void Start() {
        _image = GetComponent<Image>();
        if (_image == null ) {
            Debug.LogError("No image component found");
            Destroy(this);
        }

        _image.color = Color.red;

        _poseDetector.PoseEvent += OnPoseChange;
    }

    private void OnPoseChange(object sender, PoseEventArgs e) {
        if (e.Poses.gun) {
            _image.color = Color.green;
        } else {
            _image.color = Color.red;
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoseColorChanger : MonoBehaviour
{
    [SerializeField] private HandInputManager _handInputManager;
    private Image _image;

    private void Start() {
        _image = GetComponent<Image>();
        if (_image == null ) {
            Debug.LogError("No image component found");
            Destroy(this);
        }

        _image.color = Color.red;
    }

    private void Update() {

        if (_handInputManager.GetHandInput(HandInputType.GUN_SHOOTING)) {
            _image.color = Color.yellow;
        } else if (_handInputManager.GetHandInput(HandInputType.GUN_LOADED)) {
            _image.color = Color.green;
        } else if (_handInputManager.GetHandInput(HandInputType.HAND_OPEN)) {
            _image.color = Color.blue;
        } else if (_handInputManager.GetHandInput(HandInputType.NONE)) {
            _image.color = Color.red;
        } else {
            // hand lost
            _image.color = Color.black;
        }
    }

    /*
    private void OnPoseChange(object sender, PoseEventArgs e) {
        if (e.Poses.gun) {
            _image.color = e.Poses.Loaded ? Color.green : Color.yellow;
        } else if (e.Poses.open_hand){
            _image.color = Color.blue;
        } else {
            _image.color = Color.red;
        }
    }
    */

}

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MouseClickSimulator : MonoBehaviour
{

    [SerializeField] private Canvas _canvas;
    [SerializeField] private HandInputManager _handInputManager;
    private GraphicRaycaster _graphicRaycaster;
    private EventSystem _eventSystem;
    void Start() {
        if (_canvas == null) {
            Debug.LogError("No canvas reference");
        } else {
            _graphicRaycaster = _canvas.GetComponent<GraphicRaycaster>();
            if (_graphicRaycaster == null ) {
                Debug.LogError("Canvas has no GraphicRaycaster component");
            }
        }

        if (_handInputManager == null ) {
            Debug.LogError("No HandInputManager component");
        }

        _eventSystem = EventSystem.current;
        
    }

    void Update() {
 
        if (_handInputManager.GetHandInputDown(HandInputType.GUN_SHOOTING)) {
            var pointerPos = _handInputManager.GetPointerPos3D(true);
            pointerPos.z = Camera.main.transform.position.z;
            PointerEventData pointerData = new PointerEventData(_eventSystem) {
                position = pointerPos
            };


            List<RaycastResult> results = new List<RaycastResult>();
            _graphicRaycaster.Raycast(pointerData, results);
            

            if (results.Count > 0) {
                GameObject clickedObject = results[0].gameObject;

                Button button = clickedObject.GetComponent<Button>();
                if (button != null) {
                    button.onClick.Invoke();
                }
            }

        }
    }
}

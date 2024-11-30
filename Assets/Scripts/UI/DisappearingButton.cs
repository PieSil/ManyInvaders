using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisappearingButton : MonoBehaviour
{
    public void Disable() {
        gameObject.SetActive(false);
    }
}

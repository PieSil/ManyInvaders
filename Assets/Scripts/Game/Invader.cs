using System;
using System.Collections;
using System.Collections.Generic;
// using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.EventSystems;

public class Invader : MonoBehaviour
{
    private ValueTuple<int, int> _gridIndices;
    public Action<InvaderDestroyedEventArgs> InvaderDestroyed;


    private void OnDestroy() {
        if (InvaderDestroyed != null) {
            InvaderDestroyed(new InvaderDestroyedEventArgs(_gridIndices));
        }
    }

    public void SetGridIndices(ValueTuple<int, int> gridIndices) { 
        _gridIndices = gridIndices;
    }

}

public class InvaderDestroyedEventArgs : EventArgs {
    private ValueTuple<int, int> _gridIndices;
    public ValueTuple<int, int> GridIndices => _gridIndices;

    public InvaderDestroyedEventArgs(ValueTuple<int, int> gridIndices) {
        _gridIndices = gridIndices;
    }
}

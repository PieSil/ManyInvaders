using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AdvancedMath {

    public static float Sigmoid(float x) {
        return 1.0f / (1.0f + Mathf.Exp(x));
    }

    public static float SmoothClamp(float x, float min, float max) {
        if (max < min) {
            (min, max) = (max, min);
        }

        float sigmoid = Sigmoid(x);
        return min + (max - min) * sigmoid;
    }

}

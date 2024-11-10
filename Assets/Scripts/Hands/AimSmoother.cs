using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class AimSmoother : MonoBehaviour
{
    [SerializeField] HandPoseDetection _detector;
    private float _alpha = 0.3f;
    private Vector3 _prevMcp = new Vector3(0, 0, 0);
    private Vector3 _prevMcpToTip = new Vector3(0, 0, 0);
    private bool _hasPrevData = false;
    private float _aimingPlaneDepth = -1.0f;

    public Action<AimEventArgs> AimEvent;
    void Start() {
        _detector.AimEvent += OnAimChange;
        _hasPrevData = false;
    }

    private void OnAimChange(PointingEventArgs e) {

        Vector3 newMcp = new Vector3(0, 0, 0);
        Vector3 newMcpToTip = new Vector3(0, 0, 0);

        if (_hasPrevData) {
            
            if (e.HasData) {
                // Exponential Moving Average
                newMcp = e.Mcp * _alpha + (1 - _alpha) * _prevMcp;
                newMcpToTip = e.McpToTip * _alpha + (1 - _alpha) * _prevMcpToTip;

            } else {
                newMcp = _prevMcp;
                newMcpToTip = _prevMcpToTip;
                _hasPrevData = false; // reset previous data
            }

            _prevMcp = newMcp;
            _prevMcpToTip = newMcpToTip;

            if (AimEvent != null) {
                AimEvent(new AimEventArgs(GetAimingPoint(newMcp, newMcpToTip)));
            }

        } else {

            if (e.HasData) {
                newMcp = e.Mcp;
                newMcpToTip = e.McpToTip;
                _prevMcp = e.Mcp;
                _prevMcpToTip = e.McpToTip;
                _hasPrevData = true;

                if (AimEvent != null) {
                    AimEvent(new AimEventArgs(GetAimingPoint(newMcp, newMcpToTip)));
                }

            } else {
                // empty event
                if (AimEvent != null) {
                    AimEvent(new AimEventArgs());
                }
            }
        }
    }

    private Vector2 GetAimingPoint(Vector3 mcp, Vector3 mcpToTip) {

        Vector2 aimingPoint;
        if (Vector3.Dot(mcpToTip, new Vector3(0, 0, 1)) == 0) {
            aimingPoint = new Vector2(.0f, .0f);
        } else {

            float numerator = _aimingPlaneDepth - mcp.z;
            float z_frac = numerator / mcpToTip.z;
            float x_intersection = (z_frac * mcpToTip.x) + mcp.x;
            float y_intersection = (z_frac * mcpToTip.y) + mcp.y;

            aimingPoint = new Vector2(x_intersection, y_intersection);
        }

        return aimingPoint;
    }
}

public class AimEventArgs : EventArgs {

    public readonly Vector2 AimingPoint;
    public readonly bool HasAimingPoint;
    public AimEventArgs(Vector2 aimingPoint) {
        AimingPoint = aimingPoint;
        HasAimingPoint = true;
    }

    public AimEventArgs() {
        AimingPoint = new Vector2(0, 0);
        HasAimingPoint = false;
    }
}

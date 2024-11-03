using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandPoseSmoother : MonoBehaviour
{
    [SerializeField] HandPoseDetection _detector;
    // private Queue<RawPoseEventArgs> _eventsBuffer = new Queue<RawPoseEventArgs>();
    // private int _slidingWindowSize = 2;
    private float _alpha = 0.1f;
    private float _beta;
    // private List<float> _weights = new List<float>();
    // private RawPoseEventArgs _lastKnown = null;
    private RawPoseEventArgs _prevEvent = null;
    private RawPoseEventArgs _prevSmoothed = null;

    public EventHandler<RawPoseEventArgs> PoseEvent;
    void Start() {
        // _beta = Mathf.Log(2) / (_slidingWindowSize / 2);
        _detector.PoseEvent += OnPoseChange;
        // _eventsBuffer.Clear();

        /*
        float _beta_sum = 0;
        for (int i = 0; i < _slidingWindowSize; i++) {
            _beta_sum += Mathf.Exp(- _beta * i);
        }

        for (int i = 0; i < _slidingWindowSize; i++) {
            _weights.Add(Mathf.Exp(-_beta * i) / _beta_sum);
        }

        _weights.Reverse();
        */
    }

    private void OnPoseChange(object sender, RawPoseEventArgs e) {
        Vector3 new_mcp = e.Mcp;
        Vector3 new_tip = e.Tip;
        Vector3 new_mcp_to_tip = e.Mcp_to_tip;

        if (_prevEvent != null) {
            
            if (!e.Poses.lost_hand) {
                new_mcp = e.Mcp * _alpha + (1 - _alpha) * _prevEvent.Mcp;
                new_tip = e.Tip * _alpha + (1 - _alpha) * _prevEvent.Tip;
                new_mcp_to_tip = e.Mcp_to_tip * _alpha + (1 - _alpha) * _prevEvent.Mcp_to_tip;


            } else {
                new_mcp = _prevEvent.Mcp;
                new_tip = _prevEvent.Tip;
                new_mcp_to_tip = _prevEvent.Mcp_to_tip;
            }

            if (PoseEvent != null) {
                PoseEvent(this, new RawPoseEventArgs(e.Poses, new_mcp, new_tip, new_mcp_to_tip));
            }

        }

        if (!e.Poses.lost_hand) {
            _prevEvent = new RawPoseEventArgs(e.Poses, new_mcp, new_tip, new_mcp_to_tip);
        }
    }

    /*
    private void OnPoseChange(object sender, RawPoseEventArgs e) {

        if (!e.Poses.lost_hand) {
            _eventsBuffer.Enqueue(e);
            _lastKnown = e;
        } else {

            if (_lastKnown != null) {
                _eventsBuffer.Enqueue(_lastKnown);
            }

            // else skip
        }
        

        if (_eventsBuffer.Count > _slidingWindowSize) { 
            // remove first
            _eventsBuffer.Dequeue();
        }

        if (_eventsBuffer.Count == _slidingWindowSize) {
            // compute average and send event
            // for now just manage vectors

            Vector3 new_mcp = new Vector3(0, 0, 0);
            Vector3 new_tip = new Vector3(0, 0, 0);
            Vector3 new_mcp_to_tip = new Vector3(0, 0, 0);
            int i = 0;
            foreach (RawPoseEventArgs v in _eventsBuffer) {
                if (!v.Poses.lost_hand) {
                    new_mcp += v.Mcp * _weights[i];
                    new_tip += v.Tip * _weights[i];
                    new_mcp_to_tip += v.Mcp_to_tip * _weights[i];
                }
                i++;
            }

            new_mcp /= _weights.Sum();
            new_tip /= _weights.Sum();
            new_mcp_to_tip /= _weights.Sum();

            if (PoseEvent != null) {
                PoseEvent(sender, new RawPoseEventArgs(e.Poses, new_mcp, new_tip, new_mcp_to_tip));
            }

        }
    }
    */
}

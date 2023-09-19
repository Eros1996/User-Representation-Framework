using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;

public class HandRetargeting : MonoBehaviour
{
    private XRHandSubsystem m_HandSubsystem;

    // Start is called before the first frame update
    void Start()
    {
        var handSubsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(handSubsystems);

        for (var i = 0; i < handSubsystems.Count; ++i)
        {
            var handSubsystem = handSubsystems[i];
            if (handSubsystem.running)
            {
                m_HandSubsystem = handSubsystem;
                break;
            }
        }

        if (m_HandSubsystem != null)
        {
            m_HandSubsystem.updatedHands += OnUpdatedHands;
        }

    }

    void OnUpdatedHands(XRHandSubsystem subsystem,
        XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
        XRHandSubsystem.UpdateType updateType)
    {
        switch (updateType)
        {
            case XRHandSubsystem.UpdateType.Dynamic:
                // Update game logic that uses hand data
                break;
            case XRHandSubsystem.UpdateType.BeforeRender:
                // Update visual objects that use hand data
                var xrRightHand = subsystem.rightHand;
                var xrLeftHand = subsystem.leftHand;

                if (xrRightHand.isTracked)
                {
                    UpdateJointTransforms(xrRightHand, "Right hand");
                }

                if (xrLeftHand.isTracked)
                {
                    UpdateJointTransforms(xrLeftHand, "Left hand");
                }

                break;
        }
    }
    
    void UpdateJointTransforms(XRHand hand, string str)
    {
        for(var i = XRHandJointID.BeginMarker.ToIndex(); i < XRHandJointID.EndMarker.ToIndex(); i++)
        {
            var trackingData = hand.GetJoint(XRHandJointIDUtility.FromIndex(i));
            if (trackingData.TryGetPose(out Pose pose) && trackingData.trackingState != XRHandJointTrackingState.None)
            {
                if (trackingData.trackingState == XRHandJointTrackingState.WillNeverBeValid)
                {
                    Debug.Log("NEVER BE VALID" + str + " -> " + trackingData.id);
                }
                else
                {
                    // Update avatar hands
                    Debug.Log(str + " -> " + trackingData.id);
                }
            }
        }
    }
}

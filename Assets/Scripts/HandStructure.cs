using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;

public class HandStructure : MonoBehaviour
{
    public Vector3 rotationOffset;
    
    [Tooltip("List of XR Hand Joints with a reference to a transform to drive.")]
    public List<JointToTransformReference> jointReferences;

    private List<Transform> m_Fingers;
    private XRHandSubsystem m_HandSubsystem;

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
    
    [ContextMenu("Setup Joints References")]
    private void SetupJointsReferences()
    {
        m_Fingers = this.GetComponentsInChildren<Transform>().ToList();
        
        List<Transform> thumbs = m_Fingers.Where(finger => finger.name.Contains("Thumb")).ToList();
        List<Transform> indices = m_Fingers.Where(finger => finger.name.Contains("Index")).ToList();
        List<Transform> middles = m_Fingers.Where(finger => finger.name.Contains("Middle")).ToList();
        List<Transform> rings = m_Fingers.Where(finger => finger.name.Contains("Ring")).ToList();
        List<Transform> pinkies= m_Fingers.Where(finger => finger.name.Contains("Pinky")).ToList();

        jointReferences = new List<JointToTransformReference>();
        for (var i = XRHandJointID.BeginMarker.ToIndex(); i < XRHandJointID.EndMarker.ToIndex(); i++)
        {
            var reference = new JointToTransformReference
            {
                jointTransform = null, //(m_Fingers.Count <= i || XRHandJointIDUtility.FromIndex(i) == XRHandJointID.Palm) ? null : m_Fingers[i],
                xrHandJointID = XRHandJointIDUtility.FromIndex(i)
            };
            jointReferences.Add(reference);
        }
        
        var jointToTransform = new JointToTransformReference
        {
            jointTransform = this.transform,
            xrHandJointID = XRHandJointID.BeginMarker
        };
        jointReferences[0] = jointToTransform;
        
        MatchFingerWithJoint(thumbs, XRHandJointID.ThumbMetacarpal.ToIndex(), XRHandJointID.ThumbTip.ToIndex());
        MatchFingerWithJoint(indices, XRHandJointID.IndexMetacarpal.ToIndex(), XRHandJointID.IndexTip.ToIndex());
        MatchFingerWithJoint(middles, XRHandJointID.MiddleMetacarpal.ToIndex(), XRHandJointID.MiddleTip.ToIndex());
        MatchFingerWithJoint(rings, XRHandJointID.RingMetacarpal.ToIndex(), XRHandJointID.RingTip.ToIndex());
        MatchFingerWithJoint(pinkies, XRHandJointID.LittleMetacarpal.ToIndex(), XRHandJointID.LittleTip.ToIndex());
    }

    private void MatchFingerWithJoint(List<Transform> fingerList, int startIndex, int endIndex)
    {
        int index = fingerList.Count - 1;
        
        for (int i = endIndex; i >= startIndex; i--)
        {
            if (index < 0) continue;
            var jointToTransform = new JointToTransformReference
            {
                jointTransform = fingerList[index],
                xrHandJointID = XRHandJointIDUtility.FromIndex(i)
            };

            jointReferences[i] = jointToTransform;
            index--;
        }
    }
    
    void OnUpdatedHands(XRHandSubsystem subsystem,XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,XRHandSubsystem.UpdateType updateType)
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

                if (xrRightHand.isTracked && this.name.Contains("Right"))
                {
                    UpdateJointTransforms(xrRightHand, "Right hand");
                }

                if (xrLeftHand.isTracked && this.name.Contains("Left"))
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
            var xrHandJoint = jointReferences[i].xrHandJointID;
            var jointTransform = jointReferences[i].jointTransform;

            if (jointTransform == null)
            {
                continue;
            }
            
            var trackingData = hand.GetJoint(xrHandJoint);
            if (trackingData.TryGetPose(out Pose pose) && trackingData.trackingState != XRHandJointTrackingState.None)
            {
                if (trackingData.trackingState == XRHandJointTrackingState.WillNeverBeValid)
                {
                    Debug.Log("NEVER BE VALID" + str + " -> " + trackingData.id);
                }
                else
                {
                    // Update avatar hands
                    
                    // world position
                    jointTransform.position = pose.position;
                    jointTransform.rotation = pose.rotation * Quaternion.Euler(rotationOffset);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Hands;

[Serializable]
public struct JointToHumanBodyBonesReference
{
    [SerializeField]
    [Tooltip("The XR Hand Joint Identifier that will drive the Transform.")]
    XRHandJointID m_XRHandJointID;

    [FormerlySerializedAs("m_JointTransform")]
    [SerializeField]
    [Tooltip("The Transform that will be driven by the specified XR Joint.")]
    HumanBodyBones m_HumanBodyBoneTransform;

    /// <summary>
    /// The <see cref="XRHandJointID"/> that will drive the Transform.
    /// </summary>
    public XRHandJointID xrHandJointID
    {
        get => m_XRHandJointID;
        set => m_XRHandJointID = value;
    }

    /// <summary>
    /// The Transform that will be driven by the specified joint's tracking data.
    /// </summary>
    public HumanBodyBones humanBodyBonesTransform
    {
        get => m_HumanBodyBoneTransform;
        set => m_HumanBodyBoneTransform = value;
    }
}

public class HandStructure : MonoBehaviour
{
    public Vector3 rotationOffset;
    public Vector3 rotationThumb;
    
    [Tooltip("List of XR Hand Joints with a reference to a transform to drive.")]
    public List<JointToTransformReference> jointToTransformReferences;
    public List<JointToHumanBodyBonesReference> jointToHumanBodyBones;

    private List<Transform> m_Fingers;
    private XRHandSubsystem m_HandSubsystem;
    private Animator m_Animator;
    
    void Start()
    {
        m_Animator = GetComponentInParent<Animator>();    
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
    
    [ContextMenu("Setup Joints Dic")]
    private void SetupDictionaryOfJoints()
    {
        jointToHumanBodyBones = new List<JointToHumanBodyBonesReference>();
        JointToHumanBodyBonesReference joint2HumanBoneRef;
        
        for (var i = XRHandJointID.BeginMarker.ToIndex(); i < XRHandJointID.EndMarker.ToIndex(); i++)
        {
            joint2HumanBoneRef = new JointToHumanBodyBonesReference
            {
                xrHandJointID = XRHandJointIDUtility.FromIndex(i),
                humanBodyBonesTransform = HumanBodyBones.LastBone
            };
            jointToHumanBodyBones.Add(joint2HumanBoneRef);
        }
        
        var isRight = this.gameObject.name.Contains("RightHand");
        if (isRight)
        {
            joint2HumanBoneRef = new JointToHumanBodyBonesReference
            {
                xrHandJointID = XRHandJointID.Wrist,
                humanBodyBonesTransform = HumanBodyBones.RightHand
            };
            jointToHumanBodyBones[0] = joint2HumanBoneRef;
            
            FindMatchingJoint(39, XRHandJointID.ThumbMetacarpal.ToIndex(), XRHandJointID.ThumbDistal.ToIndex());
            FindMatchingJoint(42, XRHandJointID.IndexProximal.ToIndex(), XRHandJointID.IndexDistal.ToIndex());
            FindMatchingJoint(45, XRHandJointID.MiddleProximal.ToIndex(), XRHandJointID.MiddleDistal.ToIndex());
            FindMatchingJoint(48, XRHandJointID.RingProximal.ToIndex(), XRHandJointID.RingDistal.ToIndex());
            FindMatchingJoint(51, XRHandJointID.LittleProximal.ToIndex(), XRHandJointID.LittleDistal.ToIndex());
        }
        else
        {
            joint2HumanBoneRef = new JointToHumanBodyBonesReference
            {
                xrHandJointID = XRHandJointID.Wrist,
                humanBodyBonesTransform = HumanBodyBones.LeftHand
            };
            jointToHumanBodyBones[0] = joint2HumanBoneRef;
            
            FindMatchingJoint(24, XRHandJointID.ThumbMetacarpal.ToIndex(), XRHandJointID.ThumbDistal.ToIndex());
            FindMatchingJoint(27, XRHandJointID.IndexProximal.ToIndex(), XRHandJointID.IndexDistal.ToIndex());
            FindMatchingJoint(30, XRHandJointID.MiddleProximal.ToIndex(), XRHandJointID.MiddleDistal.ToIndex());
            FindMatchingJoint(33, XRHandJointID.RingProximal.ToIndex(), XRHandJointID.RingDistal.ToIndex());
            FindMatchingJoint(36, XRHandJointID.LittleProximal.ToIndex(), XRHandJointID.LittleDistal.ToIndex());
        }
    }

    private void FindMatchingJoint(int startIndx, int startIndex, int endIndex)
    {
        for (int i = startIndex; i <= endIndex; i++)
        {
            var joint2HumanBoneRef = new JointToHumanBodyBonesReference
            {
                xrHandJointID = XRHandJointIDUtility.FromIndex(i),
                humanBodyBonesTransform = (HumanBodyBones)startIndx 
            };
            startIndx++;
            jointToHumanBodyBones[i] = joint2HumanBoneRef;
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
    
         jointToTransformReferences = new List<JointToTransformReference>();
         for (var i = XRHandJointID.BeginMarker.ToIndex(); i < XRHandJointID.EndMarker.ToIndex(); i++)
         {
             var reference = new JointToTransformReference
             {
                 jointTransform = null, //(m_Fingers.Count <= i || XRHandJointIDUtility.FromIndex(i) == XRHandJointID.Palm) ? null : m_Fingers[i],
                 xrHandJointID = XRHandJointIDUtility.FromIndex(i)
             };
             jointToTransformReferences.Add(reference);
         }
         
         var jointToTransform = new JointToTransformReference
         {
             jointTransform = this.transform,
             xrHandJointID = XRHandJointID.BeginMarker
         };
         jointToTransformReferences[0] = jointToTransform;
         
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
    
             jointToTransformReferences[i] = jointToTransform;
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
                    UpdateHumanBodyBones(xrRightHand, "Right hand");
                    //UpdateJointTransforms(xrRightHand, "Right hand");
                }

                if (xrLeftHand.isTracked && this.name.Contains("Left"))
                {
                    UpdateHumanBodyBones(xrLeftHand, "Left hand");
                    //UpdateJointTransforms(xrLeftHand, "Left hand");

                }

                break;
        }
    }
    
    void UpdateHumanBodyBones(XRHand hand, string str)
    {
        for(var i = XRHandJointID.BeginMarker.ToIndex(); i < XRHandJointID.EndMarker.ToIndex(); i++)
        {
            var xrHandJoint = jointToHumanBodyBones[i].xrHandJointID;
            var humanBodyBones = jointToHumanBodyBones[i].humanBodyBonesTransform;

            if (humanBodyBones != HumanBodyBones.LastBone)
            {
                var trackingData = hand.GetJoint(xrHandJoint);
                if (trackingData.TryGetPose(out Pose pose) &&
                    trackingData.trackingState != XRHandJointTrackingState.None)
                {
                    if (trackingData.trackingState == XRHandJointTrackingState.WillNeverBeValid)
                    {
                        Debug.Log("NEVER BE VALID" + str + " -> " + trackingData.id);
                    }
                    else
                    {
                        var m_BoneTransform = m_Animator.GetBoneTransform(humanBodyBones);
                        
                        switch (xrHandJoint)
                        {
                            // Update avatar hands
                            case XRHandJointID.ThumbMetacarpal:
                            case XRHandJointID.ThumbDistal:
                            case XRHandJointID.ThumbProximal:
                                m_BoneTransform.position = pose.position;
                                m_BoneTransform.rotation = pose.rotation * Quaternion.Euler(rotationThumb);
                                break;
                            default:
                                m_BoneTransform.position = pose.position;
                                m_BoneTransform.rotation = pose.rotation * Quaternion.Euler(rotationOffset);
                                break;
                        }
                    }
                }
            }
        }
    }
    
    void UpdateJointTransforms(XRHand hand, string str)
    {
        for(var i = XRHandJointID.BeginMarker.ToIndex(); i < XRHandJointID.EndMarker.ToIndex(); i++)
        {
            var xrHandJoint = jointToTransformReferences[i].xrHandJointID;
            var jointTransform = jointToTransformReferences[i].jointTransform;
    
            if (jointTransform != null)
            {
                var trackingData = hand.GetJoint(xrHandJoint);
                if (trackingData.TryGetPose(out Pose pose) &&
                    trackingData.trackingState != XRHandJointTrackingState.None)
                {
                    if (trackingData.trackingState == XRHandJointTrackingState.WillNeverBeValid)
                    {
                        Debug.Log("NEVER BE VALID" + str + " -> " + trackingData.id);
                    }
                    else
                    {
                        // Update avatar hands
                        switch (xrHandJoint)
                        {
                            // Update avatar hands
                            case XRHandJointID.ThumbMetacarpal:
                            case XRHandJointID.ThumbDistal:
                            case XRHandJointID.ThumbProximal:
                                jointTransform.position = pose.position;
                                jointTransform.rotation = pose.rotation * Quaternion.Euler(rotationThumb);
                                break;
                            default:
                                jointTransform.position = pose.position;
                                jointTransform.rotation = pose.rotation * Quaternion.Euler(rotationOffset);
                                break;
                        }
                    }
                }
            }
        }
    }
}

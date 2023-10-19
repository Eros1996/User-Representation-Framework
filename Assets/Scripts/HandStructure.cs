using System;
using System.Collections.Generic;
using System.Linq;
using Leap.Unity;
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

    [FormerlySerializedAs("m_HumanBodyBoneTransform")]
    [FormerlySerializedAs("m_JointTransform")]
    [SerializeField]
    [Tooltip("The Transform that will be driven by the specified XR Joint.")]
    HumanBodyBones mHumanBodyBoneTransform;

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
    public HumanBodyBones humanBodyBoneTransform
    {
        get => mHumanBodyBoneTransform;
        set => mHumanBodyBoneTransform = value;
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
    private HumanPoseHandler poseHandler;
    private bool m_IsHandTrackingStarted;
    private bool m_IsRightHand;
    private HumanPose m_HumanPose;
    
    void Start()
    {
        m_Animator = GetComponentInParent<Animator>(); 
        m_HumanPose = new HumanPose();
        poseHandler = new HumanPoseHandler(m_Animator.avatar, m_Animator.transform);

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

    [ContextMenu("HumanTraitBoneName")]
    private void HumanTraitBoneName()
    {
        string[] muscleName = HumanTrait.MuscleName;
        for (int i = 0; i < HumanTrait.BoneCount; ++i)
        {
            try
            {
                Debug.Log( HumanTrait.BoneName[i] + " -> " + HumanTrait.MuscleName[HumanTrait.MuscleFromBone(i, 1)] + " min: " + HumanTrait.GetMuscleDefaultMin(i) + " max: " + HumanTrait.GetMuscleDefaultMax(i));
            }
            catch (Exception e)
            {
                Debug.Log( HumanTrait.BoneName[i] + " do not have muscle");
                continue;
            }
        }
    }
    
    // [ContextMenu("Rig Hand")]
    // private void CreateRig()
    // {
    //     m_Animator = GetComponentInParent<Animator>();
    //     if (m_IsRightHand)
    //     {
    //         var wrist = m_Animator.GetBoneTransform(HumanBodyBones.RightHand);
    //         //var thumbP = m_Animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
    //         var indexP = m_Animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
    //         var middleP = m_Animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
    //         var ringP = m_Animator.GetBoneTransform(HumanBodyBones.RightRingProximal);
    //         var littleP = m_Animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
    //         
    //         var palm = new GameObject("Palm");
    //         palm.transform.position = Vector3.Scale((wrist.position + middleP.position), 0.5f*Vector3.one);
    //         palm.transform.SetParent(wrist);
    //         palm.transform.localRotation = middleP.transform.localRotation;
    //
    //         var indexM = new GameObject("IndexMetacarpal");
    //         indexM.transform.position = new Vector3((wrist.position.x + palm.transform.position.x)/2f, indexP.position.y, indexP.position.z);
    //         indexM.transform.SetParent(wrist);
    //         indexM.transform.localRotation = indexP.transform.localRotation;
    //         indexP.SetParent(indexM.transform);
    //         
    //         var middleM = new GameObject("MiddleMetacarpal");
    //         middleM.transform.position = new Vector3((wrist.position.x + palm.transform.position.x)/2f, middleP.position.y, middleP.position.z);
    //         middleM.transform.SetParent(wrist);
    //         middleM.transform.localRotation = middleP.transform.localRotation;
    //         middleP.SetParent(middleM.transform);
    //         
    //         var ringM = new GameObject("RingMetacarpal");
    //         ringM.transform.position = new Vector3((wrist.position.x + palm.transform.position.x)/2f, ringP.position.y, ringP.position.z);
    //         ringM.transform.SetParent(wrist);
    //         ringM.transform.localRotation = ringP.transform.localRotation;
    //         ringP.SetParent(ringM.transform);
    //         
    //         var littleM = new GameObject("LittleMetacarpal");
    //         littleM.transform.position = new Vector3((wrist.position.x + palm.transform.position.x)/2f, littleP.position.y, littleP.position.z);
    //         littleM.transform.SetParent(wrist);
    //         littleM.transform.localRotation = littleP.transform.localRotation;
    //         littleP.SetParent(littleM.transform);
    //     }
    //     else
    //     {
    //         var wrist = m_Animator.GetBoneTransform(HumanBodyBones.LeftHand);
    //         //var thumbP = m_Animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
    //         var indexP = m_Animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
    //         var middleP = m_Animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
    //         var ringP = m_Animator.GetBoneTransform(HumanBodyBones.LeftRingProximal);
    //         var littleP = m_Animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal);
    //         
    //         var palm = new GameObject("Palm");
    //         palm.transform.position = Vector3.Scale((wrist.position + middleP.position), 0.5f*Vector3.one);
    //         palm.transform.SetParent(wrist);
    //         palm.transform.localRotation = middleP.transform.localRotation;
    //
    //         var indexM = new GameObject("IndexMetacarpal");
    //         indexM.transform.position = new Vector3((wrist.position.x + palm.transform.position.x)/2f, indexP.position.y, indexP.position.z);
    //         indexM.transform.SetParent(wrist);
    //         indexM.transform.localRotation = indexP.transform.localRotation;
    //         indexP.SetParent(indexM.transform);
    //         
    //         var middleM = new GameObject("MiddleMetacarpal");
    //         middleM.transform.position = new Vector3((wrist.position.x + palm.transform.position.x)/2f, middleP.position.y, middleP.position.z);
    //         middleM.transform.SetParent(wrist);
    //         middleM.transform.localRotation = middleP.transform.localRotation;
    //         middleP.SetParent(middleM.transform);
    //         
    //         var ringM = new GameObject("RingMetacarpal");
    //         ringM.transform.position = new Vector3((wrist.position.x + palm.transform.position.x)/2f, ringP.position.y, ringP.position.z);
    //         ringM.transform.SetParent(wrist);
    //         ringM.transform.localRotation = ringP.transform.localRotation;
    //         ringP.SetParent(ringM.transform);
    //         
    //         var littleM = new GameObject("LittleMetacarpal");
    //         littleM.transform.position = new Vector3((wrist.position.x + palm.transform.position.x)/2f, littleP.position.y, littleP.position.z);
    //         littleM.transform.SetParent(wrist);
    //         littleM.transform.localRotation = littleP.transform.localRotation;
    //         littleP.SetParent(littleM.transform);
    //     }
    // }
    
    [ContextMenu("Setup Joints To HumanBodyBones")]
    private void SetupJointsToHumanBodyBones()
    {
        jointToHumanBodyBones = new List<JointToHumanBodyBonesReference>();
        JointToHumanBodyBonesReference joint2HumanBoneRef;
        
        for (var i = XRHandJointID.BeginMarker.ToIndex(); i < XRHandJointID.EndMarker.ToIndex(); i++)
        {
            joint2HumanBoneRef = new JointToHumanBodyBonesReference
            {
                xrHandJointID = XRHandJointIDUtility.FromIndex(i),
                humanBodyBoneTransform = HumanBodyBones.LastBone
            };
            jointToHumanBodyBones.Add(joint2HumanBoneRef);
        }
        
        m_IsRightHand = this.gameObject.name.Contains("RightHand");
        if (m_IsRightHand)
        {
            joint2HumanBoneRef = new JointToHumanBodyBonesReference
            {
                xrHandJointID = XRHandJointID.Wrist,
                humanBodyBoneTransform = HumanBodyBones.RightHand
            };
            jointToHumanBodyBones[0] = joint2HumanBoneRef;
            
            FindMatchingJoint((int)HumanBodyBones.RightThumbProximal, XRHandJointID.ThumbMetacarpal.ToIndex(), XRHandJointID.ThumbDistal.ToIndex());
            FindMatchingJoint((int)HumanBodyBones.RightIndexProximal, XRHandJointID.IndexProximal.ToIndex(), XRHandJointID.IndexDistal.ToIndex());
            FindMatchingJoint((int)HumanBodyBones.RightMiddleProximal, XRHandJointID.MiddleProximal.ToIndex(), XRHandJointID.MiddleDistal.ToIndex());
            FindMatchingJoint((int)HumanBodyBones.RightRingProximal, XRHandJointID.RingProximal.ToIndex(), XRHandJointID.RingDistal.ToIndex());
            FindMatchingJoint((int)HumanBodyBones.RightLittleProximal, XRHandJointID.LittleProximal.ToIndex(), XRHandJointID.LittleDistal.ToIndex());
        }
        else
        {
            joint2HumanBoneRef = new JointToHumanBodyBonesReference
            {
                xrHandJointID = XRHandJointID.Wrist,
                humanBodyBoneTransform = HumanBodyBones.LeftHand
            };
            jointToHumanBodyBones[0] = joint2HumanBoneRef;
            
            FindMatchingJoint((int)HumanBodyBones.LeftThumbProximal, XRHandJointID.ThumbMetacarpal.ToIndex(), XRHandJointID.ThumbDistal.ToIndex());
            FindMatchingJoint((int)HumanBodyBones.LeftIndexProximal, XRHandJointID.IndexProximal.ToIndex(), XRHandJointID.IndexDistal.ToIndex());
            FindMatchingJoint((int)HumanBodyBones.LeftMiddleProximal, XRHandJointID.MiddleProximal.ToIndex(), XRHandJointID.MiddleDistal.ToIndex());
            FindMatchingJoint((int)HumanBodyBones.LeftRingProximal, XRHandJointID.RingProximal.ToIndex(), XRHandJointID.RingDistal.ToIndex());
            FindMatchingJoint((int)HumanBodyBones.LeftLittleProximal, XRHandJointID.LittleProximal.ToIndex(), XRHandJointID.LittleDistal.ToIndex());
        }
    }

    private void FindMatchingJoint(int startIndx, int startIndex, int endIndex)
    {
        for (int i = startIndex; i <= endIndex; i++)
        {
            var joint2HumanBoneRef = new JointToHumanBodyBonesReference
            {
                xrHandJointID = XRHandJointIDUtility.FromIndex(i),
                humanBodyBoneTransform = (HumanBodyBones)startIndx 
            };
            startIndx++;
            jointToHumanBodyBones[i] = joint2HumanBoneRef;
        }
    }
    
    // [ContextMenu("Setup Joints References")]
    //  private void SetupJointsReferences()
    //  {
    //      m_Fingers = this.GetComponentsInChildren<Transform>().ToList();
    //      
    //      List<Transform> thumbs = m_Fingers.Where(finger => finger.name.Contains("Thumb")).ToList();
    //      List<Transform> indices = m_Fingers.Where(finger => finger.name.Contains("Index")).ToList();
    //      List<Transform> middles = m_Fingers.Where(finger => finger.name.Contains("Middle")).ToList();
    //      List<Transform> rings = m_Fingers.Where(finger => finger.name.Contains("Ring")).ToList();
    //      List<Transform> pinkies= m_Fingers.Where(finger => finger.name.Contains("Pinky")).ToList();
    //
    //      jointToTransformReferences = new List<JointToTransformReference>();
    //      for (var i = XRHandJointID.BeginMarker.ToIndex(); i < XRHandJointID.EndMarker.ToIndex(); i++)
    //      {
    //          var reference = new JointToTransformReference
    //          {
    //              jointTransform = null, //(m_Fingers.Count <= i || XRHandJointIDUtility.FromIndex(i) == XRHandJointID.Palm) ? null : m_Fingers[i],
    //              xrHandJointID = XRHandJointIDUtility.FromIndex(i)
    //          };
    //          jointToTransformReferences.Add(reference);
    //      }
    //      
    //      var jointToTransform = new JointToTransformReference
    //      {
    //          jointTransform = this.transform,
    //          xrHandJointID = XRHandJointID.BeginMarker
    //      };
    //      jointToTransformReferences[0] = jointToTransform;
    //      
    //      MatchFingerWithJoint(thumbs, XRHandJointID.ThumbMetacarpal.ToIndex(), XRHandJointID.ThumbTip.ToIndex());
    //      MatchFingerWithJoint(indices, XRHandJointID.IndexMetacarpal.ToIndex(), XRHandJointID.IndexTip.ToIndex());
    //      MatchFingerWithJoint(middles, XRHandJointID.MiddleMetacarpal.ToIndex(), XRHandJointID.MiddleTip.ToIndex());
    //      MatchFingerWithJoint(rings, XRHandJointID.RingMetacarpal.ToIndex(), XRHandJointID.RingTip.ToIndex());
    //      MatchFingerWithJoint(pinkies, XRHandJointID.LittleMetacarpal.ToIndex(), XRHandJointID.LittleTip.ToIndex());
    //  }
    //
    //  private void MatchFingerWithJoint(List<Transform> fingerList, int startIndex, int endIndex)
    //  {
    //      int index = fingerList.Count - 1;
    //      
    //      for (int i = endIndex; i >= startIndex; i--)
    //      {
    //          if (index < 0) continue;
    //          var jointToTransform = new JointToTransformReference
    //          {
    //              jointTransform = fingerList[index],
    //              xrHandJointID = XRHandJointIDUtility.FromIndex(i)
    //          };
    //
    //          jointToTransformReferences[i] = jointToTransform;
    //          index--;
    //      }
    //  }
    
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
        
        poseHandler.GetHumanPose(ref m_HumanPose);
        
        for(var i = XRHandJointID.BeginMarker.ToIndex(); i < XRHandJointID.EndMarker.ToIndex(); i++)
        {
            var xrHandJoint = jointToHumanBodyBones[i].xrHandJointID;
            var humanBodyBones = jointToHumanBodyBones[i].humanBodyBoneTransform;

            if (humanBodyBones != HumanBodyBones.LastBone && xrHandJoint != XRHandJointID.Wrist)
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
                        int muscleIndexStretched, muscleIndexSpread = 0;
                        float clampedRotation;
                        var poseRot = pose.rotation.eulerAngles;
                        switch (xrHandJoint)
                        {
                            case XRHandJointID.ThumbMetacarpal:
                                muscleIndexSpread = HumanTrait.MuscleFromBone((int)humanBodyBones, 1);
                                m_HumanPose.muscles[muscleIndexSpread] = RemapAxis(poseRot.x);
                               
                                //Debug.Log(HumanTrait.MuscleName[muscleIndexSpread] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexSpread)*m_Animator.humanScale + " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexSpread)*m_Animator.humanScale + " rot " + poseRot.x);
                                
                                muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
                                m_HumanPose.muscles[muscleIndexStretched] = RemapAxis(poseRot.z);

                                //Debug.Log(HumanTrait.MuscleName[muscleIndexStretched] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexStretched)*m_Animator.humanScale + " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexStretched)*m_Animator.humanScale + " rot " + poseRot.z);
                                break;
                            case XRHandJointID.ThumbProximal:
                            case XRHandJointID.ThumbDistal:
                                muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
                                m_HumanPose.muscles[muscleIndexStretched] = RemapAxis(poseRot.z);
                                break;
                            case XRHandJointID.IndexProximal:
                            case XRHandJointID.MiddleProximal:
                            case XRHandJointID.RingProximal:
                            case XRHandJointID.LittleProximal:
                                muscleIndexSpread = HumanTrait.MuscleFromBone((int)humanBodyBones, 1);
                                m_HumanPose.muscles[muscleIndexSpread] = RemapAxis(poseRot.z);

                                //Debug.Log(HumanTrait.MuscleName[muscleIndexSpread] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexSpread) *m_Animator.humanScale+ " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexSpread)*m_Animator.humanScale + " rot " + poseRot.z);

                                muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
                                m_HumanPose.muscles[muscleIndexStretched] = RemapAxis(poseRot.x);

                                //Debug.Log(HumanTrait.MuscleName[muscleIndexStretched] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexStretched)*m_Animator.humanScale + " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexStretched)*m_Animator.humanScale + " rot " + poseRot.x);
                                break;
                            
                            default:
                                muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
                                m_HumanPose.muscles[muscleIndexStretched] = RemapAxis(poseRot.x);

                                //Debug.Log(HumanTrait.MuscleName[muscleIndexStretched] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexStretched)*m_Animator.humanScale + " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexStretched)*m_Animator.humanScale + " rot " + poseRot.x);
                                break;
                        }
                    }
                }
            }
        }
        poseHandler.SetHumanPose(ref m_HumanPose);
    }

    private float RemapAxis(float angle)
    {
        // Normalize the angle to the range of 0 to 1
        float normalizedAngle = angle / 360f; // sottrarre 180 e riportare tra -1 1

        // Remap the normalized angle to the range of -1 to 1
        float remappedAngle = (normalizedAngle * 2f) - 1f;

        return remappedAngle;
    }
}

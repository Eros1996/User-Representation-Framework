using System;
using System.Collections.Generic;
using System.Linq;
using Leap.Unity;
using RootMotion.FinalIK;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using UnityEngine.XR.Hands;

[Serializable]
public struct JointToHumanBodyBonesReference
{
    [SerializeField]
    [Tooltip("The XR Hand Joint Identifier that will drive the Transform.")]
    XRHandJointID m_XRHandJointID;
    
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
    public HumanBodyBones humanBodyBoneTransform
    {
        get => m_HumanBodyBoneTransform;
        set => m_HumanBodyBoneTransform = value;
    }
}

public class HandStructure : MonoBehaviour
{
    public Vector3 wristRotationOffset;
    [Tooltip("List of XR Hand Joints with a reference to a transform to drive.")]
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
        LoadSubsystem();
    }

    private void LoadSubsystem()
    {
        m_Animator = GetComponentInParent<Animator>();
        //poseHandler = new HumanPoseHandler(m_Animator.avatar, m_Animator.transform);
        //poseHandler.GetHumanPose(ref m_HumanPose);

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

    private void OnEnable()
    {
        LoadSubsystem();
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

                if (xrRightHand.isTracked)
                {
                    //UpdateHumanBodyBones(xrRightHand, "Right hand");
                    //UpdateFingersTransform(xrRightHand, "Right hand");
                }
                else
                {
                    //PrecomputeHumanPose();
                }

                if (xrLeftHand.isTracked)
                {
                    //UpdateHumanBodyBones(xrLeftHand, "Left hand");
                    UpdateFingersTransform(xrLeftHand, "Left hand");
                }
                else
                {
                    //PrecomputeHumanPose();
                }
                break;
        }
    }
    
    void UpdateFingersTransform(XRHand hand, string str)
    {
        Debug.Log(str);
        var wristIndex = XRHandJointID.Wrist.ToIndex();
        var wristJoint = jointToHumanBodyBones[wristIndex].xrHandJointID;
        var wristTrackingData = hand.GetJoint(wristJoint);
        if (wristTrackingData.TryGetPose(out var wristJointPose))
        {
            // var wristHumanBodyBones = jointToHumanBodyBones[wristIndex].humanBodyBoneTransform;
            // var wristTransform = m_Animator.GetBoneTransform(wristHumanBodyBones);
            // wristTransform.localRotation = wristJointPose.rotation;
            // wristTransform.localPosition = wristJointPose.position;
                
            for (var fingerIndex = (int)XRHandFingerID.Thumb;
                 fingerIndex <= (int)XRHandFingerID.Little;
                 ++fingerIndex)
            {
                var parentPose = wristJointPose;
                var fingerId = (XRHandFingerID)fingerIndex;
                var jointIndexBack = fingerId.GetBackJointID().ToIndex();
                var jointIndexFront = fingerId.GetFrontJointID().ToIndex();
                for (var jointIndex = jointIndexFront;
                     jointIndex <= jointIndexBack;
                     ++jointIndex)
                {
                    var fingerHumanBodyBones = jointToHumanBodyBones[jointIndex].humanBodyBoneTransform;
                    if(fingerHumanBodyBones == HumanBodyBones.LastBone)
                        continue;
                    
                    var fingerJoint = jointToHumanBodyBones[jointIndex].xrHandJointID;
                    var fingerTrackingData = hand.GetJoint(fingerJoint);
                    if (fingerTrackingData.TryGetPose(out var fingerJointPose))
                    {
                        CalculateLocalTransformPose(parentPose, fingerJointPose, out var jointLocalPose);
                        parentPose = fingerJointPose;
                        var fingerTransform = m_Animator.GetBoneTransform(fingerHumanBodyBones); 
                        fingerTransform.localRotation = jointLocalPose.rotation;
                        fingerTransform.position = fingerJointPose.position;
                    }
                }
            }
        }
    }
    
     void UpdateFingersTransform(XRHand hand)
    {
        for (var fingerIndex = (int)XRHandFingerID.Thumb;
             fingerIndex <= (int)XRHandFingerID.Little;
             ++fingerIndex)
        {
            var fingerId = (XRHandFingerID)fingerIndex;
            var jointIndexBack = fingerId.GetBackJointID().ToIndex();
            var jointIndexFront = fingerId.GetFrontJointID().ToIndex();
            for (var jointIndex = jointIndexFront;
                 jointIndex <= jointIndexBack;
                 ++jointIndex)
            {
                var fingerHumanBodyBones = jointToHumanBodyBones[jointIndex].humanBodyBoneTransform;
                if(fingerHumanBodyBones == HumanBodyBones.LastBone)
                    continue;
                    
                var fingerJoint = jointToHumanBodyBones[jointIndex].xrHandJointID;
                var fingerTrackingData = hand.GetJoint(fingerJoint);
                if (fingerTrackingData.TryGetPose(out var fingerJointPose))
                {
                    var fingerTransform = m_Animator.GetBoneTransform(fingerHumanBodyBones);
                    fingerTransform.localRotation = fingerJointPose.rotation;
                    fingerTransform.position = fingerJointPose.position;
                }
            }
        }
    }
    
    void CalculateLocalTransformPose(in Pose parentPose, in Pose jointPose, out Pose jointLocalPose)
    {
        var inverseParentRotation = Quaternion.Inverse(parentPose.rotation);
        jointLocalPose.position = inverseParentRotation * (jointPose.position - parentPose.position);
        jointLocalPose.rotation = inverseParentRotation * jointPose.rotation;
    }

    void PrecomputeHumanPose()
    {
        
    }

    // void UpdateHumanBodyBones(XRHand hand, string str)
    // {
    //     for(var i = XRHandJointID.BeginMarker.ToIndex(); i < XRHandJointID.EndMarker.ToIndex(); i++)
    //     {
    //         var xrHandJoint = jointToHumanBodyBones[i].xrHandJointID;
    //         var humanBodyBones = jointToHumanBodyBones[i].humanBodyBoneTransform;
    //
    //         if (humanBodyBones == HumanBodyBones.LastBone || xrHandJoint == XRHandJointID.Wrist) continue;
    //         var trackingData = hand.GetJoint(xrHandJoint);
    //         if (trackingData.TryGetPose(out Pose pose))
    //         {
    //             if (trackingData.trackingState is XRHandJointTrackingState.WillNeverBeValid or XRHandJointTrackingState.None)
    //             {
    //                 Debug.Log("NONE OR NEVER BE VALID" + str + " -> " + trackingData.id);
    //             }
    //             else
    //             {
    //                 int muscleIndexStretched, muscleIndexSpread = 0;
    //                 float remappedVal = 0;
    //                 var poseRot = pose.rotation.eulerAngles;
    //                 
    //                 switch (xrHandJoint)
    //                 {
    //                     case XRHandJointID.ThumbMetacarpal:
    //                         muscleIndexSpread = HumanTrait.MuscleFromBone((int)humanBodyBones, 1);
    //                         //remappedVal = Mathf.Clamp(poseRot.x, HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread));
    //                         remappedVal = math.remap(0, 360, HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread),  poseRot.x);
    //                         remappedVal = math.remap(HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread), -1, 1, remappedVal);
    //                         m_HumanPose.muscles[muscleIndexSpread] = remappedVal;
    //
    //                         //Debug.Log(HumanTrait.MuscleName[muscleIndexSpread] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexSpread)*m_Animator.humanScale + " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexSpread)*m_Animator.humanScale + " rot " + poseRot.x);
    //                             
    //                         muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
    //                         //remappedVal = Mathf.Clamp(poseRot.z, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched));
    //                         remappedVal = math.remap(0, 360, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched),  poseRot.z);
    //                         remappedVal = math.remap(HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched), -1, 1, remappedVal);
    //                         m_HumanPose.muscles[muscleIndexStretched] = remappedVal;
    //                         
    //                         //Debug.Log(HumanTrait.MuscleName[muscleIndexStretched] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexStretched)*m_Animator.humanScale + " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexStretched)*m_Animator.humanScale + " rot " + poseRot.z);
    //                         break;
    //                     case XRHandJointID.ThumbProximal:
    //                     case XRHandJointID.ThumbDistal:
    //                         muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
    //                         //remappedVal = Mathf.Clamp(poseRot.z, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched));
    //                         remappedVal = math.remap(0, 360, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched),  poseRot.z);
    //                         remappedVal = math.remap(HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched), -1, 1, remappedVal);
    //                         m_HumanPose.muscles[muscleIndexStretched] = remappedVal;
    //                         break;
    //                     case XRHandJointID.IndexProximal:
    //                     case XRHandJointID.MiddleProximal:
    //                     case XRHandJointID.RingProximal:
    //                     case XRHandJointID.LittleProximal:
    //                         muscleIndexSpread = HumanTrait.MuscleFromBone((int)humanBodyBones, 1);
    //                         //remappedVal = Mathf.Clamp(poseRot.z, HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread));
    //                         remappedVal = math.remap(0, 360, HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread),  poseRot.z);
    //                         remappedVal = math.remap(HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread), -1, 1, remappedVal);
    //                         m_HumanPose.muscles[muscleIndexSpread] = remappedVal;
    //                         //Debug.Log(HumanTrait.MuscleName[muscleIndexSpread] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexSpread) *m_Animator.humanScale+ " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexSpread)*m_Animator.humanScale + " rot " + poseRot.z);
    //
    //                         muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
    //                         //remappedVal = Mathf.Clamp(poseRot.x, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched));
    //                         remappedVal = math.remap(0, 360, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched),  poseRot.x);
    //                         remappedVal = math.remap(HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched), -1, 1, remappedVal);
    //                         m_HumanPose.muscles[muscleIndexStretched] = remappedVal;
    //                         //Debug.Log(HumanTrait.MuscleName[muscleIndexStretched] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexStretched)*m_Animator.humanScale + " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexStretched)*m_Animator.humanScale + " rot " + poseRot.x);
    //                         break;
    //                     default:
    //                         muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
    //                         //remappedVal = Mathf.Clamp(poseRot.x, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched));
    //                         remappedVal = math.remap(0, 360, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched),  poseRot.x);
    //                         remappedVal = math.remap(HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched), -1, 1, remappedVal);
    //                         m_HumanPose.muscles[muscleIndexStretched] = remappedVal;
    //                         //Debug.Log(HumanTrait.MuscleName[muscleIndexStretched] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexStretched)*m_Animator.humanScale + " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexStretched)*m_Animator.humanScale + " rot " + poseRot.x);
    //                         break;
    //                 }
    //             }
    //         }
    //     }
    //     poseHandler.SetHumanPose(ref m_HumanPose);
    // }
}

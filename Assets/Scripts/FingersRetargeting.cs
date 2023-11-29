using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

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

public class FingersRetargeting : MonoBehaviour
{
    public bool isRightHand;
    // public Vector3 proximalRotationOffset;
    // public Vector3 intermediateRotationOffset;
    // public Vector3 distalRotationOffset;
    // public Vector3 thumbRotationOffset;
    // public Vector3 metacarpalRotationOffset;
    public List<JointToHumanBodyBonesReference> jointToHumanBodyBones;

    private List<Transform> m_Fingers;
    private XRHandSubsystem m_HandSubsystem;
    private Animator m_Animator;
    private HumanPoseHandler poseHandler;
    private bool m_IsHandTrackingStarted;
    private XRHandTrackingEvents m_XRHandTrackingEvents;
    private GameObject worlds, locals, cubeWorld, cubeLocal;
    private GameObject m_XRRig;
    private XRInputModalityManager m_InputModalityManager;
    private float m_HandScale;
    private bool m_IsScaleFix;
    private Vector3 m_ProximalRotationOffset = new Vector3(90, 90, 90);
    private Vector3 m_IntermediateRotationOffset = new Vector3(90, 90, 90);
    private Vector3 m_DistalRotationOffset = new Vector3(90, 90, 90);
    private Vector3 m_ThumbRotationOffset = new Vector3();
    private Vector3 m_MetacarpalRotationOffset = new Vector3();
    
    private void Awake()
    {
        m_XRRig = GameObject.Find("XR Origin (XR Rig)");
        m_InputModalityManager = m_XRRig.GetComponent<XRInputModalityManager>();
        m_XRHandTrackingEvents = isRightHand ? m_InputModalityManager.rightHand.GetComponentInChildren<XRHandTrackingEvents>() : m_InputModalityManager.leftHand.GetComponentInChildren<XRHandTrackingEvents>();;
        m_Animator = GetComponentInParent<Animator>();
    }
    
    private void OnEnable()
    {
        m_IsScaleFix = false;
        LoadSubsystem();
    }

    private void OnDisable()
    {
        this.transform.localScale = Vector3.one;
    }

    private void SetHandScale(XRHand hand)
    {
        var avtWrist = this.transform;
        var fingerWristData = hand.GetJoint(XRHandJointID.Wrist);
        fingerWristData.TryGetPose(out var xrWristJointPose);
        
        var fingerMiddleDistalData = hand.GetJoint(XRHandJointID.MiddleDistal);
        fingerMiddleDistalData.TryGetPose(out var xrMiddleDistalJointPose); 
        var middleDistalHumanBodyBones = jointToHumanBodyBones[14].humanBodyBoneTransform;
        var avtMiddleDistalTransform = m_Animator.GetBoneTransform(middleDistalHumanBodyBones);

        var avtScale = Vector3.Distance(avtWrist.position, avtMiddleDistalTransform.position);
        var xrScale = Vector3.Distance(xrWristJointPose.position, xrMiddleDistalJointPose.position);
        m_HandScale = xrScale / avtScale;
        
        this.transform.localScale = Vector3.one * m_HandScale;
    }
    
    private void LoadSubsystem()
    {
        if (m_HandSubsystem is not null) return;
        
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
            // Debug.Log("Update fingers");
            m_HandSubsystem.updatedHands += OnUpdatedHands;
            m_XRHandTrackingEvents.jointsUpdated.AddListener(UpdateJoints);
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
                Debug.Log( HumanTrait.BoneName[i] + " do not have muscle" + e);
                continue;
            }
        }
    }
    
    [ContextMenu("Setup Joints To HumanBodyBones")]
    public void SetupJointsToHumanBodyBones()
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
        
        if (isRightHand)
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
                XRHand currXrHand = isRightHand ? subsystem.rightHand : subsystem.leftHand;
                UpdateFingers(currXrHand);
                break;
        }
    }

    void UpdateJoints(XRHandJointsUpdatedEventArgs args)
    {
        XRHand currXrHand = isRightHand ? m_HandSubsystem.rightHand : m_HandSubsystem.leftHand;
        UpdateFingers(currXrHand);
    }

    void UpdateFingers(XRHand hand)
    {
        if (!m_IsScaleFix)
        {
            SetHandScale(hand);
            m_ThumbRotationOffset = isRightHand ? new Vector3(180, 90, 90) : new Vector3(0, 90, 90);
            m_MetacarpalRotationOffset = isRightHand ? new Vector3(180, 90, 90) : new Vector3(0, 90, 90);
            m_IsScaleFix = true;
        }
        // var wristIndex = XRHandJointID.Wrist.ToIndex();
        //
        // var xrWristJoint = jointToHumanBodyBones[wristIndex].xrHandJointID;
        // var fingerWristData = hand.GetJoint(xrWristJoint);
        // fingerWristData.TryGetPose(out var xrWristJointPose);
        //
        // var wristHumanBodyBones = jointToHumanBodyBones[wristIndex].humanBodyBoneTransform;
        // var avtWristTransform = m_Animator.GetBoneTransform(wristHumanBodyBones);
        // avtWristTransform.position = xrWristJointPose.position + m_XRRig.transform.position;
        // avtWristTransform.rotation = xrWristJointPose.rotation * m_XRRig.transform.rotation;
        
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
                var xrFingerJoint = jointToHumanBodyBones[jointIndex].xrHandJointID;
                var fingerTrackingData = hand.GetJoint(xrFingerJoint);
                fingerTrackingData.TryGetPose(out var xrFingerJointPose);
                
                var fingerHumanBodyBones = jointToHumanBodyBones[jointIndex].humanBodyBoneTransform;
                if (fingerHumanBodyBones == HumanBodyBones.LastBone)
                    continue;
                
                var avtFingerTransform = m_Animator.GetBoneTransform(fingerHumanBodyBones);
                avtFingerTransform.position = xrFingerJointPose.position + m_XRRig.transform.position;
                switch (xrFingerJoint)
                {
                    case XRHandJointID.IndexProximal or XRHandJointID.LittleProximal or XRHandJointID.MiddleProximal or XRHandJointID.RingProximal:
                        avtFingerTransform.rotation = xrFingerJointPose.rotation * Quaternion.Euler(m_ProximalRotationOffset);
                        break;
                    case XRHandJointID.IndexIntermediate or XRHandJointID.LittleIntermediate or XRHandJointID.MiddleIntermediate or XRHandJointID.RingIntermediate:
                        avtFingerTransform.rotation = xrFingerJointPose.rotation * Quaternion.Euler(m_IntermediateRotationOffset);
                        break;
                    case XRHandJointID.IndexDistal or XRHandJointID.LittleDistal or XRHandJointID.MiddleDistal or XRHandJointID.RingDistal:
                        avtFingerTransform.rotation = xrFingerJointPose.rotation * Quaternion.Euler(m_DistalRotationOffset);
                        break;
                    case XRHandJointID.ThumbMetacarpal:
                        avtFingerTransform.rotation = xrFingerJointPose.rotation * Quaternion.Euler(m_MetacarpalRotationOffset);
                        break;
                    default:
                        avtFingerTransform.rotation = xrFingerJointPose.rotation * Quaternion.Euler(m_ThumbRotationOffset);
                        break;
                }

                //avtFingerTransform.rotation *= m_XRRig.transform.rotation;
                
                // var xrTRS = Matrix4x4.TRS(xrFingerJointPose.position, xrFingerJointPose.rotation, new Vector3(1, 1, 1));
                // var avtTRS = Matrix4x4.TRS(avtFingerTransform.position, avtFingerTransform.rotation, avtFingerTransform.localScale);
                // var combTRS = avtTRS.inverse * xrTRS;
                //
                // avtFingerTransform.position = combTRS.MultiplyPoint3x4(xrFingerJointPose.position);
                // avtFingerTransform.eulerAngles = combTRS.MultiplyVector(xrFingerJointPose.rotation.eulerAngles);

                // cubeWorld = GameObject.Find(fingerHumanBodyBones.ToString());
                // if (cubeWorld == null)
                // {
                //     cubeWorld = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //     cubeWorld.name = fingerHumanBodyBones.ToString();
                //     cubeWorld.transform.localScale = Vector3.one * 0.01f;
                // }
                // cubeWorld.transform.SetLocalPositionAndRotation(avtFingerTransform.position, avtFingerTransform.rotation);
            }
        }
    }
    
    void CalculateLocalTransformPose(in Pose parentPose, in Pose jointPose, out Pose jointLocalPose)
    {
        var inverseParentRotation = Quaternion.Inverse(parentPose.rotation);
        jointLocalPose.position = inverseParentRotation * (jointPose.position - parentPose.position);
        jointLocalPose.rotation = inverseParentRotation * jointPose.rotation;
    }
    
    // void UpdateHumanBodyBones(XRHand hand)
    // {
    //     poseHandler.GetHumanPose(ref m_HumanPose);
    //     
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
    //                 Debug.Log("NONE OR NEVER BE VALID -> " + trackingData.id);
    //             }
    //             else
    //             {
    //                 int muscleIndexStretched, muscleIndexSpread = 0;
    //                 float remappedVal = 0;
    //                 var poseRot = pose.rotation.eulerAngles;
    //                 
    //                 muscleIndexSpread = HumanTrait.MuscleFromBone((int)humanBodyBones, 1);
    //                 if (muscleIndexSpread != -1)
    //                 {
    //                     m_HumanPose.muscles[muscleIndexSpread] = Mathf.Lerp(HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread), pose.rotation.z);
    //                 }
    //                 muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
    //                 if (muscleIndexStretched != -1)
    //                 {
    //                     m_HumanPose.muscles[muscleIndexStretched] = Mathf.Lerp(HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched), pose.rotation.x);
    //                 }
    //                 
    //                 // switch (xrHandJoint)
    //                 // {
    //                 //     case XRHandJointID.ThumbMetacarpal:
    //                 //         muscleIndexSpread = HumanTrait.MuscleFromBone((int)humanBodyBones, 1);
    //                 //         //remappedVal = Mathf.Clamp(poseRot.x, HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread));
    //                 //         remappedVal = math.remap(0, 360, HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread),  poseRot.x);
    //                 //         remappedVal = math.remap(HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread), -1, 1, remappedVal);
    //                 //         m_HumanPose.muscles[muscleIndexSpread] = remappedVal;
    //                 //
    //                 //         //Debug.Log(HumanTrait.MuscleName[muscleIndexSpread] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexSpread)*m_Animator.humanScale + " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexSpread)*m_Animator.humanScale + " rot " + poseRot.x);
    //                 //             
    //                 //         muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
    //                 //         //remappedVal = Mathf.Clamp(poseRot.z, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched));
    //                 //         remappedVal = math.remap(0, 360, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched),  poseRot.z);
    //                 //         remappedVal = math.remap(HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched), -1, 1, remappedVal);
    //                 //         m_HumanPose.muscles[muscleIndexStretched] = remappedVal;
    //                 //         
    //                 //         //Debug.Log(HumanTrait.MuscleName[muscleIndexStretched] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexStretched)*m_Animator.humanScale + " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexStretched)*m_Animator.humanScale + " rot " + poseRot.z);
    //                 //         break;
    //                 //     case XRHandJointID.ThumbProximal:
    //                 //     case XRHandJointID.ThumbDistal:
    //                 //         muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
    //                 //         //remappedVal = Mathf.Clamp(poseRot.z, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched));
    //                 //         remappedVal = math.remap(0, 360, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched),  poseRot.z);
    //                 //         remappedVal = math.remap(HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched), -1, 1, remappedVal);
    //                 //         m_HumanPose.muscles[muscleIndexStretched] = remappedVal;
    //                 //         break;
    //                 //     case XRHandJointID.IndexProximal:
    //                 //     case XRHandJointID.MiddleProximal:
    //                 //     case XRHandJointID.RingProximal:
    //                 //     case XRHandJointID.LittleProximal:
    //                 //         muscleIndexSpread = HumanTrait.MuscleFromBone((int)humanBodyBones, 1);
    //                 //         //remappedVal = Mathf.Clamp(poseRot.z, HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread));
    //                 //         remappedVal = math.remap(0, 360, HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread),  poseRot.z);
    //                 //         remappedVal = math.remap(HumanTrait.GetMuscleDefaultMin(muscleIndexSpread), HumanTrait.GetMuscleDefaultMax(muscleIndexSpread), -1, 1, remappedVal);
    //                 //         m_HumanPose.muscles[muscleIndexSpread] = remappedVal;
    //                 //         //Debug.Log(HumanTrait.MuscleName[muscleIndexSpread] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexSpread) *m_Animator.humanScale+ " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexSpread)*m_Animator.humanScale + " rot " + poseRot.z);
    //                 //
    //                 //         muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
    //                 //         //remappedVal = Mathf.Clamp(poseRot.x, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched));
    //                 //         remappedVal = math.remap(0, 360, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched),  poseRot.x);
    //                 //         remappedVal = math.remap(HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched), -1, 1, remappedVal);
    //                 //         m_HumanPose.muscles[muscleIndexStretched] = remappedVal;
    //                 //         //Debug.Log(HumanTrait.MuscleName[muscleIndexStretched] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexStretched)*m_Animator.humanScale + " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexStretched)*m_Animator.humanScale + " rot " + poseRot.x);
    //                 //         break;
    //                 //     default:
    //                 //         muscleIndexStretched = HumanTrait.MuscleFromBone((int)humanBodyBones, 2);
    //                 //         //remappedVal = Mathf.Clamp(poseRot.x, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched));
    //                 //         remappedVal = math.remap(0, 360, HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched),  poseRot.x);
    //                 //         remappedVal = math.remap(HumanTrait.GetMuscleDefaultMin(muscleIndexStretched), HumanTrait.GetMuscleDefaultMax(muscleIndexStretched), -1, 1, remappedVal);
    //                 //         m_HumanPose.muscles[muscleIndexStretched] = remappedVal;
    //                 //         //Debug.Log(HumanTrait.MuscleName[muscleIndexStretched] + " -> min: " + HumanTrait.GetMuscleDefaultMin(muscleIndexStretched)*m_Animator.humanScale + " max: " + HumanTrait.GetMuscleDefaultMax(muscleIndexStretched)*m_Animator.humanScale + " rot " + poseRot.x);
    //                 //         break;
    //                 // }
    //             }
    //         }
    //     }
    //     poseHandler.SetHumanPose(ref m_HumanPose);
    // }
}

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
    public List<JointToHumanBodyBonesReference> jointToHumanBodyBones;

    private List<Transform> m_Fingers;
    private XRHandSubsystem m_HandSubsystem;
    private Animator m_Animator;
    private HumanPoseHandler poseHandler;
    private bool m_IsHandTrackingStarted;
    private GameObject worlds, locals, cubeWorld, cubeLocal;
    private GameObject m_XRRig;
    private XRInputModalityManager m_InputModalityManager;
    private XRHandSkeletonDriver m_XRHandSkeletonDriver;
    private float m_HandScale;
    private bool m_IsScaleFix;
    private Vector3 m_ProximalRotationOffset = new Vector3(90, 90, 90);
    private Vector3 m_IntermediateRotationOffset = new Vector3(90, 90, 90);
    private Vector3 m_DistalRotationOffset = new Vector3(90, 90, 90);
    private Vector3 m_ThumbRotationOffset;
    private Vector3 m_MetacarpalRotationOffset;
    private XRHand m_XrHand;
    private List<JointToTransformReference> m_JointTransformReferences;

    public void IsScaleFix(bool isScaleFix)
    {
        m_IsScaleFix = isScaleFix;
    }
    
    private void Start()
    {
        m_XRRig = GameObject.Find("XR Origin (XR Rig)");
        m_InputModalityManager = m_XRRig.GetComponent<XRInputModalityManager>();
        m_XRHandSkeletonDriver = isRightHand ? m_InputModalityManager.rightHand.GetComponentInChildren<XRHandSkeletonDriver>() : m_InputModalityManager.leftHand.GetComponentInChildren<XRHandSkeletonDriver>();
        m_JointTransformReferences = m_XRHandSkeletonDriver.jointTransformReferences;
        m_Animator = GetComponentInParent<Animator>();
    }
    
    private void OnEnable()
    {
        this.transform.localScale = Vector3.one;
        m_IsScaleFix = false;
        LoadSubsystem();
    }

    private void OnDisable()
    {
        this.transform.localScale = Vector3.one;
        m_HandSubsystem = null;
    }

    private void SetHandScale(XRHand hand)
    {
        this.transform.localScale = Vector3.one;
        
        var avtWrist = this.transform;
        var fingerWristData = hand.GetJoint(XRHandJointID.Wrist);
        if (!fingerWristData.TryGetPose(out var xrWristJointPose)) return;
        
        var fingerMiddleData = hand.GetJoint(XRHandJointID.MiddleDistal);
        if(!fingerMiddleData.TryGetPose(out var xrMiddleJointPose)) return; 
        var avtMiddleDistalTransform = isRightHand ? m_Animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal) : m_Animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal);;
        if (avtMiddleDistalTransform is null) return;
        
        var avtScale = Vector3.Distance(avtWrist.position, avtMiddleDistalTransform.position);
        var xrScale = Vector3.Distance(xrWristJointPose.position, xrMiddleJointPose.position);
        
        m_HandScale = xrScale / avtScale;
        this.transform.localScale = Vector3.one * m_HandScale;
        
        Debug.Log("SCALING " + hand.handedness);
    }
    
    private void LoadSubsystem()
    {
        var handSubsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(handSubsystems);

        foreach (var handSubsystem in handSubsystems)
        {
            if (!handSubsystem.running) continue;
            m_HandSubsystem = handSubsystem;
            break;
        }

        if (m_HandSubsystem == null) return;
        m_XrHand = isRightHand ? m_HandSubsystem.rightHand : m_HandSubsystem.leftHand;
        m_HandSubsystem.updatedHands += OnUpdatedHands;
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
                UpdateSkeletonFingers(m_XrHand);
                break;
        }
    }
    
    void UpdateSkeletonFingers(XRHand hand)
    {
        if (!m_IsScaleFix)
        {
            SetHandScale(hand);
            m_ThumbRotationOffset = isRightHand ? new Vector3(180, 90, 90) : new Vector3(0, 90, 90);
            m_MetacarpalRotationOffset = isRightHand ? new Vector3(180, 90, 90) : new Vector3(0, 90, 90);
            m_IsScaleFix = true;
        }

        for (var i = XRHandJointID.ThumbMetacarpal.ToIndex(); i < XRHandJointID.EndMarker.ToIndex(); i++)
        {
            var fingerHumanBodyBones = jointToHumanBodyBones[i].humanBodyBoneTransform;
            if (fingerHumanBodyBones == HumanBodyBones.LastBone)
                continue;
            var avtFingerTransform = m_Animator.GetBoneTransform(fingerHumanBodyBones);
            var xrFinger = jointToHumanBodyBones[i].xrHandJointID;

            foreach (var jointToTransform in m_JointTransformReferences)
            {
                if (jointToTransform.xrHandJointID == xrFinger)
                {
                    var xrSkeletonJointTransform = jointToTransform.jointTransform;
                    //avtFingerTransform.position = xrSkeletonJointTransform.position;

                    switch (xrFinger)
                    {
                        case XRHandJointID.IndexProximal or XRHandJointID.LittleProximal or XRHandJointID.MiddleProximal or XRHandJointID.RingProximal:
                            avtFingerTransform.rotation = xrSkeletonJointTransform.rotation * Quaternion.Euler(m_ProximalRotationOffset);
                            break;
                        case XRHandJointID.IndexIntermediate or XRHandJointID.LittleIntermediate or XRHandJointID.MiddleIntermediate or XRHandJointID.RingIntermediate:
                            avtFingerTransform.rotation = xrSkeletonJointTransform.rotation * Quaternion.Euler(m_IntermediateRotationOffset);
                            break;
                        case XRHandJointID.IndexDistal or XRHandJointID.LittleDistal or XRHandJointID.MiddleDistal or XRHandJointID.RingDistal:
                            avtFingerTransform.rotation = xrSkeletonJointTransform.rotation * Quaternion.Euler(m_DistalRotationOffset);
                            break;
                        case XRHandJointID.ThumbMetacarpal:
                            avtFingerTransform.rotation = xrSkeletonJointTransform.rotation * Quaternion.Euler(m_MetacarpalRotationOffset);
                            break;
                        case XRHandJointID.ThumbProximal or XRHandJointID.ThumbDistal:
                            avtFingerTransform.rotation = xrSkeletonJointTransform.rotation * Quaternion.Euler(m_ThumbRotationOffset);
                            break;
                        default:
                            avtFingerTransform.rotation = xrSkeletonJointTransform.rotation;
                            break;
                    }
                }
            }
        }   
    }
}

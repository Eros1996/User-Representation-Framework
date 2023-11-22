using System;
using System.Collections;
using System.Collections.Generic;
using RootMotion.Demos;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class AvatarTrackingModalityManager : MonoBehaviour
{
    private XRInputModalityManager m_InputModalityManager;
    private GameObject m_XRRig, m_XRCam, m_XRParent;
    private Transform m_XRHead, m_XRLC, m_XRRC, m_XRLH, m_XRRH;

    private VRIK m_VRIK;
    private VRIKCalibrationBasic m_CalibrationBasic;
    private VRIKCalibrationController m_calibrationController;
    private Animator m_Animator;
    private bool autoTune;

    private GameObject m_AvHead, m_AvLeftHand, m_AvRightHand;
    private FingerRig m_FingerRigL, m_FingerRigR;

    private XRHandSkeletonDriver m_XRLeftHandSkeletonDriver, m_XRRightHandSkeletonDriver;
    private FingersRetargeting m_HandStructureL;
    private FingersRetargeting m_HandStructureR;
    private AnimateOnInput m_AnimationInput;
    private OnButtonPress m_ButtonsInput;
    private RuntimeAnimatorController m_animatorController;
    
    // Start is called before the first frame update
    void Start()
    {
        m_XRRig = GameObject.Find("XR Origin (XR Rig)");
        m_XRCam = GameObject.Find("Main Camera");
        m_XRParent = GameObject.Find("XR Interaction Hands Setup");
        m_XRCam.GetComponent<Camera>().nearClipPlane = 0.05f;
        Debug.Log(m_XRRig ? "Found XR Origin" : "Could not find XR Origin!");

        //Rig and avatar components
        m_Animator = GetComponent<Animator>();
        m_InputModalityManager = m_XRRig.GetComponent<XRInputModalityManager>();
        m_XRLeftHandSkeletonDriver = m_InputModalityManager.leftHand.GetComponentInChildren<XRHandSkeletonDriver>();
        m_XRRightHandSkeletonDriver = m_InputModalityManager.rightHand.GetComponentInChildren<XRHandSkeletonDriver>();
        m_AnimationInput = GetComponent<AnimateOnInput>();
        m_ButtonsInput = m_XRParent.GetComponent<OnButtonPress>();
        m_animatorController = m_Animator.runtimeAnimatorController;
        
        //Hand References
        m_AvLeftHand = m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).gameObject;
        m_AvRightHand = m_Animator.GetBoneTransform(HumanBodyBones.RightHand).gameObject;
        //m_FingerRigL = m_AvLeftHand.GetComponent<FingerRig>();
        //m_FingerRigR = m_AvRightHand.GetComponent<FingerRig>();
        //MapFingers(m_FingerRigL, m_XRLeftHandSkeletonDriver);
        //MapFingers(m_FingerRigR, m_XRRightHandSkeletonDriver);
        m_HandStructureL = m_AvLeftHand.GetComponent<FingersRetargeting>();
        m_HandStructureR = m_AvRightHand.GetComponent<FingersRetargeting>();
        
        // XR References
        m_XRHead = m_XRCam.transform.Find("Head IK_target");
        m_XRLC = m_InputModalityManager.leftController.transform.Find("Left Arm IK_target");
        m_XRRC = m_InputModalityManager.rightController.transform.Find("Right Arm IK_target");
        m_XRLH = m_XRLeftHandSkeletonDriver.jointTransformReferences[XRHandJointID.Wrist.ToIndex()].jointTransform.Find("Left Arm IK_target");
        m_XRRH = m_XRRightHandSkeletonDriver.jointTransformReferences[XRHandJointID.Wrist.ToIndex()].jointTransform.Find("Right Arm IK_target");
        
        // Avatar calibration information
        m_VRIK = gameObject.AddComponent<VRIK>();
        SetVRIKLocomotionMode(IKSolverVR.Locomotion.Mode.Animated, 0);
        m_CalibrationBasic = gameObject.AddComponent<VRIKCalibrationBasic>();
        m_CalibrationBasic.ik = m_VRIK;
        m_CalibrationBasic.centerEyeAnchor = m_XRHead;
        
        // Events 
        m_InputModalityManager.motionControllerModeStarted.AddListener(SwitchToMotionController);
        m_InputModalityManager.trackedHandModeStarted.AddListener(SwitchToHandTracking);
    }

    private void SwitchToHandTracking()
    {
        Debug.Log("Calibrating hand tracking...");
        m_CalibrationBasic.leftHandAnchor = m_XRLH;
        m_CalibrationBasic.rightHandAnchor = m_XRRH;
        SetVRIKLocomotionMode(IKSolverVR.Locomotion.Mode.Procedural, 1);
        StartVRIKCalibration();
        ActivateFingerTracking(true);
        m_Animator.runtimeAnimatorController = null;
    }

    private void SwitchToMotionController()
    {
        Debug.Log("Calibrating controller...");
        m_Animator.runtimeAnimatorController = m_animatorController;
        m_CalibrationBasic.leftHandAnchor = m_XRLC;
        m_CalibrationBasic.rightHandAnchor = m_XRRC;
        ActivateFingerTracking(false);
        SetVRIKLocomotionMode(IKSolverVR.Locomotion.Mode.Animated, 1);
        StartVRIKCalibration();
    }

    private void SetVRIKLocomotionMode(IKSolverVR.Locomotion.Mode locomotionMode, int weight)
    {
        m_VRIK.solver.locomotion.mode = locomotionMode;
        m_VRIK.solver.locomotion.weight = weight;
    }
    
    private void ActivateFingerTracking(bool activate)
    {
        m_ButtonsInput.enabled = !activate;
        m_AnimationInput.enabled = !activate;
        m_HandStructureL.enabled = activate;
        m_HandStructureR.enabled = activate;

        // if (activate)
        // {
        //     m_FingerRigL.weight = 1;
        //     m_FingerRigR.weight = 1;
        // }
        // else
        // {
        //     m_FingerRigL.weight = 0;
        //     m_FingerRigR.weight = 0;
        // }
    }
    
    private void MapFingers(FingerRig FingerRig, XRHandSkeletonDriver HandSkeleton)
    {
        FingerRig.fingers[0].target = HandSkeleton.jointTransformReferences[5].jointTransform;
        FingerRig.fingers[1].target = HandSkeleton.jointTransformReferences[15].jointTransform;
        FingerRig.fingers[2].target = HandSkeleton.jointTransformReferences[10].jointTransform;
        FingerRig.fingers[3].target = HandSkeleton.jointTransformReferences[20].jointTransform;
        FingerRig.fingers[4].target = HandSkeleton.jointTransformReferences[24].jointTransform;
    }
    
    private void OnDisable()
    {
        m_InputModalityManager.motionControllerModeStarted.RemoveListener(SwitchToMotionController);
        m_InputModalityManager.trackedHandModeStarted.RemoveListener(SwitchToHandTracking);
    }

    public void StartVRIKCalibration()
    {
        m_CalibrationBasic.Calibrate();
    }
}
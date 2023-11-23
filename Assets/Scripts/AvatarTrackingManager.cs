using System.Collections;
using System.Collections.Generic;
using RootMotion.Demos;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class AvatarTrackingManager : MonoBehaviour
{
    private XRInputModalityManager m_InputModalityManager;
    private GameObject m_XRRig, m_XRCam, m_XRParent;
    private Transform m_XRHead, m_XRLC, m_XRRC, m_XRLH, m_XRRH, m_XRLF, m_XRRF, m_XRP;

    private VRIK m_VRIK;
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
        FindXRComponents();
        FindAvatarComponents();
        SetupVrikCalibrationInformation();
        SetupXREvents();
    }
    
    private void SetupXREvents()
    {
        m_InputModalityManager.motionControllerModeStarted.AddListener(SwitchToMotionController);
        m_InputModalityManager.trackedHandModeStarted.AddListener(SwitchToHandTracking);
    }
    
    private void SetupVrikCalibrationInformation()
    {
        m_VRIK = gameObject.AddComponent<VRIK>();
        SetVRIKLocomotionMode(IKSolverVR.Locomotion.Mode.Animated, 0);
        m_calibrationController = gameObject.AddComponent<VRIKCalibrationController>();
        m_calibrationController.ik = m_VRIK;
        m_calibrationController.headTracker = m_XRHead;
    }

    private void FindXRComponents()
    {
        m_XRRig = GameObject.Find("XR Origin (XR Rig)");
        m_XRCam = GameObject.Find("Main Camera");
        m_XRParent = GameObject.Find("XR Interaction Hands Setup");
        m_XRCam.GetComponent<Camera>().nearClipPlane = 0.05f;
        
        m_InputModalityManager = m_XRRig.GetComponent<XRInputModalityManager>();
        m_XRLeftHandSkeletonDriver = m_InputModalityManager.leftHand.GetComponentInChildren<XRHandSkeletonDriver>();
        m_XRRightHandSkeletonDriver = m_InputModalityManager.rightHand.GetComponentInChildren<XRHandSkeletonDriver>();
        m_ButtonsInput = m_XRParent.GetComponent<OnButtonPress>();
        
        m_XRHead = m_XRCam.transform.Find("Head IK_target");
        m_XRLC = m_InputModalityManager.leftController.transform.Find("Left Arm IK_target");
        m_XRRC = m_InputModalityManager.rightController.transform.Find("Right Arm IK_target");
        m_XRLH = m_XRLeftHandSkeletonDriver.jointTransformReferences[XRHandJointID.Wrist.ToIndex()].jointTransform.Find("Left Arm IK_target");
        m_XRRH = m_XRRightHandSkeletonDriver.jointTransformReferences[XRHandJointID.Wrist.ToIndex()].jointTransform.Find("Right Arm IK_target");
        m_XRLF = GameObject.Find("Left Foot IK_target").transform;
        m_XRRF = GameObject.Find("Right Foot IK_target").transform;
        m_XRP = GameObject.Find("Pelvis IK_target").transform;
        //m_ButtonsInput = m_XRParent.AddComponent<OnButtonPress>();
        //m_ButtonsInput.action.AddBinding("<XRController>{LeftHand}/secondaryButton");
        //m_ButtonsInput.OnPress.AddListener(StartVRIKCalibration);
    }

    private void FindAvatarComponents()
    {
        m_Animator = GetComponent<Animator>();
        m_animatorController = m_Animator.runtimeAnimatorController;
        
        m_AnimationInput = GetComponent<AnimateOnInput>();
        
        m_AvLeftHand = m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).gameObject;
        m_AvRightHand = m_Animator.GetBoneTransform(HumanBodyBones.RightHand).gameObject;
        m_HandStructureL = m_AvLeftHand.AddComponent<FingersRetargeting>();
        m_HandStructureR = m_AvRightHand.AddComponent<FingersRetargeting>();
        m_HandStructureR.isRightHand = true;
        m_HandStructureL.SetupJointsToHumanBodyBones();
        m_HandStructureR.SetupJointsToHumanBodyBones();
    }
    
    private void SwitchToHandTracking()
    {
        Debug.Log("Calibrating hand tracking...");
        m_calibrationController.leftHandTracker = m_XRLH;
        m_calibrationController.rightHandTracker = m_XRRH;
        m_calibrationController.leftFootTracker = m_XRLF;
        m_calibrationController.rightFootTracker = m_XRRF;
        m_calibrationController.bodyTracker = m_XRP;
        SetVRIKLocomotionMode(IKSolverVR.Locomotion.Mode.Procedural, 1);
        StartVRIKCalibration();
        ActivateFingerTracking(true);
        m_Animator.runtimeAnimatorController = null;
    }

    private void SwitchToMotionController()
    {
        Debug.Log("Calibrating controller...");
        m_Animator.runtimeAnimatorController = m_animatorController;
        m_calibrationController.leftHandTracker = m_XRLC;
        m_calibrationController.rightHandTracker = m_XRRC;
        m_calibrationController.leftFootTracker = m_XRLF;
        m_calibrationController.rightFootTracker = m_XRRF;
        m_calibrationController.bodyTracker = m_XRP;
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
    }
    
    private void OnDisable()
    {
        m_InputModalityManager.motionControllerModeStarted.RemoveListener(SwitchToMotionController);
        m_InputModalityManager.trackedHandModeStarted.RemoveListener(SwitchToHandTracking);
    }

    public void StartVRIKCalibration()
    {
        m_calibrationController.Calibrate();
    }
}

using System.Collections;
using RootMotion.Demos;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public enum HMD
{
    Sitting,
    StandUp
}

public class AvatarTrackingManager : MonoBehaviour
{
    public HMD hmdType;
    
    private XRInputModalityManager m_InputModalityManager;
    private GameObject m_XRRig, m_XRCam, m_XRParent, m_XRCamOff;
    private Transform m_XRHead, m_XRLC, m_XRRC, m_XRLH, m_XRRH, m_XRLF, m_XRRF, m_XRP, m_WristLeft, m_WristRight;

    private VRIK m_VRIK;
    private VRIKCalibrationController m_calibrationController;
    private Animator m_Animator;
    private bool autoTune;

    private GameObject m_AvHead, m_AvLeftHand, m_AvRightHand;
    private FingerRig m_FingerRigL, m_FingerRigR;

    private XRHandSkeletonDriver m_XRLeftHandSkeletonDriver, m_XRRightHandSkeletonDriver;
    private FingersRetargeting m_FingersRetargetingL;
    private FingersRetargeting m_FingersRetargetingR;
    private AnimateOnInput m_AnimationInput;
    private OnButtonPress m_ButtonsInput;
    private RuntimeAnimatorController m_AnimatorController;
    private bool m_InitializationDone;
    
    // Start is called before the first frame update
    void Start()
    {
        FindXRComponents();
        SetupSittingOffset();
        FindAvatarComponents();
        SetupVrikCalibrationInformation();
        SetupXREvents();
    }

    private void SetupSittingOffset()
    {
        if (hmdType == HMD.Sitting)
        {
            var myOffset = new GameObject();
            myOffset.name = "Sitting Offset";
            myOffset.transform.SetParent(m_XRRig.transform);
            myOffset.transform.position = Vector3.up * 0.35f;
            m_XRCamOff.transform.SetParent(myOffset.transform);
        }
    }

    private void SetupXREvents()
    {
        m_InputModalityManager.motionControllerModeStarted.AddListener(SwitchToMotionController);
        m_InputModalityManager.trackedHandModeStarted.AddListener(SwitchToHandTracking);
    }
    
    private void SetupVrikCalibrationInformation()
    {
        m_VRIK = GetComponent<VRIK>() ?? gameObject.AddComponent<VRIK>();
        m_VRIK.solver.plantFeet = true;
        
        m_calibrationController = GetComponent<VRIKCalibrationController>() ?? gameObject.AddComponent<VRIKCalibrationController>();
        m_calibrationController.settings = new VRIKCalibrator.Settings();
        m_calibrationController.ik = m_VRIK;
        m_calibrationController.headTracker = m_XRHead;
    }
    
    private void FindXRComponents()
    {
        m_XRRig = GameObject.Find("XR Origin (XR Rig)");
        m_XRCam = GameObject.Find("Main Camera");
        m_XRParent = GameObject.Find("XR Interaction Hands Setup");
        m_XRCamOff = GameObject.Find("Camera Offset");
        
        m_InputModalityManager = m_XRRig.GetComponent<XRInputModalityManager>();
        m_XRLeftHandSkeletonDriver = m_InputModalityManager.leftHand.GetComponentInChildren<XRHandSkeletonDriver>();
        m_XRRightHandSkeletonDriver = m_InputModalityManager.rightHand.GetComponentInChildren<XRHandSkeletonDriver>();
        m_ButtonsInput = m_XRParent.GetComponent<OnButtonPress>();
        
        m_XRHead = m_XRCam.transform.Find("Head IK_target");
        
        m_XRLC = m_InputModalityManager.leftController.transform.Find("Left Arm IK_target");
        m_XRRC = m_InputModalityManager.rightController.transform.Find("Right Arm IK_target");
        
        m_WristLeft = m_XRLeftHandSkeletonDriver.jointTransformReferences[XRHandJointID.BeginMarker.ToIndex()].jointTransform;
        m_XRLH = m_WristLeft.Find("Left Arm IK_target-Leap offset").gameObject.activeSelf ? m_WristLeft.Find("Left Arm IK_target-Leap offset") : m_WristLeft;
        m_WristRight = m_XRRightHandSkeletonDriver.jointTransformReferences[XRHandJointID.BeginMarker.ToIndex()].jointTransform;
        m_XRRH = m_WristRight.Find("Right Arm IK_target-Leap offset").gameObject.activeSelf ? m_WristRight.Find("Right Arm IK_target-Leap offset") : m_WristRight;
        
        m_XRLF = GameObject.Find("Left Foot IK_target")?.transform;
        m_XRRF = GameObject.Find("Right Foot IK_target")?.transform;
        m_XRP = GameObject.Find("Pelvis IK_target")?.transform;
        
        m_ButtonsInput = m_XRParent.GetComponent<OnButtonPress>() ?? m_XRParent.AddComponent<OnButtonPress>();
        m_ButtonsInput.action.AddBinding("<XRController>{LeftHand}/triggerPressed");
        m_ButtonsInput.action.AddBinding("<MetaAimHand>{LeftHand}/indexPressed"); // Remove when Leap Motion implement also other actions

        // m_ButtonsInput.action.AddCompositeBinding("ButtonWithOneModifier")
        //     .With("Button", "<MetaAimHand>{RightHand}/ringPressed")
        //     .With("Modifier", "<MetaAimHand>{LeftHand}/ringPressed");
        m_ButtonsInput.OnPress.AddListener(VRIKCalibration);
    }

    private void FindAvatarComponents()
    {
        m_Animator = GetComponent<Animator>() ?? gameObject.AddComponent<Animator>();
        m_AnimatorController = m_Animator.runtimeAnimatorController;
        
        m_AnimationInput = GetComponent<AnimateOnInput>() ?? gameObject.AddComponent<AnimateOnInput>();
        
        m_AvLeftHand = m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).gameObject;
        m_AvRightHand = m_Animator.GetBoneTransform(HumanBodyBones.RightHand).gameObject;
        
        m_FingersRetargetingL = m_AvLeftHand.GetComponent<FingersRetargeting>() ?? m_AvLeftHand.AddComponent<FingersRetargeting>();
        m_FingersRetargetingR = m_AvRightHand.GetComponent<FingersRetargeting>() ?? m_AvRightHand.AddComponent<FingersRetargeting>();
        m_FingersRetargetingR.isRightHand = true;
        m_FingersRetargetingL.isRightHand = false;
        m_FingersRetargetingL.SetupJointsToHumanBodyBones();
        m_FingersRetargetingR.SetupJointsToHumanBodyBones();
        EnableFingerRetargeting(false);
    }

    private IEnumerator WaitXRMode()
    {
        while ((XRInputModalityManager.currentInputMode.Value == XRInputModalityManager.InputMode.None) ||
               (XRInputModalityManager.currentInputMode.Value == XRInputModalityManager.InputMode.TrackedHand &&
                m_WristLeft.localPosition == new Vector3(-0.1f, 0, 0) && m_WristRight.localPosition == new Vector3(0.1f, 0, 0)))
        {
            Debug.Log("NONE");
            yield return null;
        }
        
        m_calibrationController.leftFootTracker = m_XRLF.localPosition == Vector3.zero ? null : m_XRLF;
        m_calibrationController.rightFootTracker = m_XRRF.localPosition == Vector3.zero ? null : m_XRLF;
        m_calibrationController.bodyTracker = m_XRP.localPosition == Vector3.zero ? null : m_XRP;
        
        switch (XRInputModalityManager.currentInputMode.Value)
        {
            case XRInputModalityManager.InputMode.TrackedHand:
                EnableAnimation(false);
                m_Animator.runtimeAnimatorController = null;
                SetVRIKLocomotionMode(IKSolverVR.Locomotion.Mode.Procedural, 1);
                m_calibrationController.leftHandTracker = m_XRLH;
                m_calibrationController.rightHandTracker = m_XRRH;
                CalibrationData();
                EnableFingerRetargeting(true);
                Debug.Log("fingers on");
                break;
            case XRInputModalityManager.InputMode.MotionController:
                EnableAnimation(true);
                m_Animator.runtimeAnimatorController = m_AnimatorController;
                SetVRIKLocomotionMode(IKSolverVR.Locomotion.Mode.Animated, 1);
                m_calibrationController.leftHandTracker = m_XRLC;
                m_calibrationController.rightHandTracker = m_XRRC;
                CalibrationData();
                EnableFingerRetargeting(false);
                Debug.Log("controller on");
                break;
        }
    }
    
    private void EnableFingerRetargeting(bool enable)
    {
        m_FingersRetargetingL.enabled = enable;
        m_FingersRetargetingR.enabled = enable;
        
        m_FingersRetargetingL.IsScaleFix(!enable);
        m_FingersRetargetingR.IsScaleFix(!enable);
    }

    private void EnableAnimation(bool enable)
    {
        m_AnimationInput.enabled = enable;
    }
    
    private void SwitchToHandTracking()
    {
        Debug.Log("Calibrating hand tracking...");
        StartCoroutine(WaitXRMode());
    }

    private void SwitchToMotionController()
    {
        Debug.Log("Calibrating controller...");
        StartCoroutine(WaitXRMode());
    }

    private void SetVRIKLocomotionMode(IKSolverVR.Locomotion.Mode locomotionMode, int weight)
    {
        m_VRIK.solver.locomotion.mode = locomotionMode;
        m_VRIK.solver.locomotion.weight = weight;
    }
    
    private void OnDisable()
    {
        m_InputModalityManager.motionControllerModeStarted.RemoveListener(SwitchToMotionController);
        m_InputModalityManager.trackedHandModeStarted.RemoveListener(SwitchToHandTracking);
    }

    private void CalibrationData()
    {
        m_calibrationController.data = VRIKCalibrator.Calibrate(m_calibrationController.ik,
            m_calibrationController.settings,
            m_calibrationController.headTracker,
            m_calibrationController.bodyTracker,
            m_calibrationController.leftHandTracker,
            m_calibrationController.rightHandTracker,
            m_calibrationController.leftFootTracker,
            m_calibrationController.rightFootTracker);
    }
    
    public void VRIKCalibration()
    {
        StartCoroutine(WaitXRMode());
        Debug.Log("CALIBRATED");
    }
}

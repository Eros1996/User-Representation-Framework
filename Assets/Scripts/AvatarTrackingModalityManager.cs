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
    private GameObject m_XRRig, m_XRCam;
    private Transform m_XRHead, m_XRLC, m_XRRC, m_XRLH, m_XRRH;

    private VRIK m_VRIK;
    private VRIKCalibrationBasic m_CalibrationBasic;
    private VRIKCalibrationController m_calibrationController;
    private Animator m_Animator;
    private bool autoTune;

    private GameObject m_AvHead, m_AvLeftHand, m_AvRightHand;
    private FingerRig m_FingerRigL, m_FingerRigR;

    private XRHandSkeletonDriver m_XRLeftHandSkeletonDriver, m_XRRightHandSkeletonDriver;
    private HandStructure m_HandStructureL, m_HandStructureR;
    
    // Start is called before the first frame update
    void Start()
    {
        m_XRRig = GameObject.Find("XR Origin (XR Rig)");
        m_XRCam = GameObject.Find("Main Camera");
        m_XRCam.GetComponent<Camera>().nearClipPlane = 0.05f;
        Debug.Log(m_XRRig ? "Found XR Origin" : "Could not find XR Origin!");

        m_Animator = GetComponent<Animator>();
        m_InputModalityManager = m_XRRig.GetComponent<XRInputModalityManager>();
        m_XRLeftHandSkeletonDriver = m_InputModalityManager.leftHand.GetComponentInChildren<XRHandSkeletonDriver>();
        m_XRRightHandSkeletonDriver = m_InputModalityManager.rightHand.GetComponentInChildren<XRHandSkeletonDriver>();

        //Hand References
        m_AvLeftHand = m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).gameObject;
        m_AvRightHand = m_Animator.GetBoneTransform(HumanBodyBones.RightHand).gameObject;
        m_FingerRigL = m_AvLeftHand.GetComponent<FingerRig>();
        m_FingerRigR = m_AvRightHand.GetComponent<FingerRig>();
        m_HandStructureL = m_AvLeftHand.GetComponent<HandStructure>();
        m_HandStructureR = m_AvRightHand.GetComponent<HandStructure>();
        
        // XR References
        m_XRHead = m_XRCam.transform.Find("Head IK_target");
        m_XRLC = m_InputModalityManager.leftController.transform.Find("Left Arm IK_target");
        m_XRRC = m_InputModalityManager.rightController.transform.Find("Right Arm IK_target");
        m_XRLH = m_XRLeftHandSkeletonDriver.jointTransformReferences[XRHandJointID.Wrist.ToIndex()].jointTransform.Find("Left Arm IK_target");
        m_XRRH = m_XRRightHandSkeletonDriver.jointTransformReferences[XRHandJointID.Wrist.ToIndex()].jointTransform.Find("Right Arm IK_target");
        
        // Avatar calibration information
        m_VRIK = gameObject.AddComponent<VRIK>();
        m_VRIK.solver.locomotion.weight = 0;
        m_CalibrationBasic = gameObject.AddComponent<VRIKCalibrationBasic>();
        m_CalibrationBasic.ik = m_VRIK;
        m_CalibrationBasic.centerEyeAnchor = m_XRHead;
        
        // Events 
        m_InputModalityManager.motionControllerModeStarted.AddListener(SwitchToMotionController);
        m_InputModalityManager.trackedHandModeStarted.AddListener(SwitchToHandTracking);
    }

    private void Update()
    {

    }

    private void SwitchToHandTracking()
    {
        m_CalibrationBasic.leftHandAnchor = m_XRLH;
        m_CalibrationBasic.rightHandAnchor = m_XRRH;
        Calibration();
        ActivateFingerTracking(true);
    }

    private void SwitchToMotionController()
    {
        ActivateFingerTracking(false);
        m_CalibrationBasic.leftHandAnchor = m_XRLC;
        m_CalibrationBasic.rightHandAnchor = m_XRRC;
        Calibration();
    }

    private void ActivateFingerTracking(bool activate)
    {
        // if (activate)
        // {
        //     m_InputModalityManager.leftController.GetComponentInChildren<SkinnedMeshRenderer>().materials[1] = 
        // }
        // else
        // {
        //     m_InputModalityManager.leftController.GetComponentInChildren<SkinnedMeshRenderer>().materials[1] = 
        // }
        
        m_HandStructureL.enabled = activate;
        m_HandStructureR.enabled = activate;
        
        // if (!activate)
        // {
        //     m_FingerRigL.weight = 0;
        //     m_FingerRigR.weight = 0;
        //     return;
        // }
        //
        // m_FingerRigL.weight = 1;
        // MapFingers(m_FingerRigL, m_XRLeftHandSkeletonDriver);
        //
        // m_FingerRigR.weight = 1;
        // MapFingers(m_FingerRigR, m_XRRightHandSkeletonDriver);
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

    public void Calibration()
    {
        m_CalibrationBasic.Calibrate();
        Debug.Log("Calibrating...");
    }
}
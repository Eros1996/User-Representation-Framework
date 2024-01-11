using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using RootMotion.FinalIK;
using UnityEngine;

public class RecordAnimation : MonoBehaviour
{
	public AnimationClip clip;

    private Animator m_Animator;
    private bool m_RecordingAnim, m_RecordingCSV;

    private HumanPoseHandler m_PoseHandler;
    private HumanPose m_HumanPose;
    private Dictionary<string, AnimationCurve> m_MuscleCurve = new Dictionary<string, AnimationCurve>();
    private float time = 0;
    
    private List<float> m_TransformList = new List<float>();
	private CultureInfo invC = CultureInfo.InvariantCulture;
	private string m_FilePath;
    private StreamWriter m_Writer;

    private string[] fingers = { "Thumb", "Index", "Middle", "Ring", "Little" };

    private VRIK vrik;
    
	// Start is called before the first frame update
	void Start()
    {
		m_Animator = GetComponent<Animator>();
		m_PoseHandler = new HumanPoseHandler(m_Animator.avatar, this.transform);
		vrik = GetComponent<VRIK>();
    }
	
	private void LateUpdate()
	{
		if (Input.GetKeyDown(KeyCode.A))
			if(!m_RecordingAnim) 
				StartRecordingAnim();
			else 
				StopRecordingAnim();
		
		if (Input.GetKeyDown(KeyCode.C))
			if(!m_RecordingCSV) 
				StartRecordingCsv();
			else 
				StopRecordingCsv();
	}
	
	public void StartRecordingCsv() 
    {
	    if(m_RecordingCSV) return;

	    Debug.Log("Start Recording");
	    m_RecordingCSV = true;
		StartCoroutine(Recording());
    }

    public void StopRecordingCsv() 
    {
	    if(!m_RecordingCSV) return;

	    m_RecordingCSV = false;
	    StopCoroutine(Recording());
	    WriteToCsv(GetCsvPath());
	    Debug.Log("Stop Recording");
    }

    public void StopRecordingAnim()
    {
	    if(!m_RecordingAnim) return;
	    
	    vrik.solver.OnPostUpdate -= BuildDictOfMuscle;

	    m_RecordingAnim = false;
	    clip.ClearCurves();
	    
	    foreach (var kvp in m_MuscleCurve) 
	    {
		    clip.SetCurve("", typeof(Animator), kvp.Key, kvp.Value);
	    }

	    clip.EnsureQuaternionContinuity();
	    
	    Debug.Log("Stop Recording");
    }

    public void StartRecordingAnim()
    {
	    if(m_RecordingAnim) return;
	    
	    Debug.Log("Start Recording");
	    m_RecordingAnim = true;
	    vrik.solver.OnPostUpdate += BuildDictOfMuscle;
    }
    
    #region ANIM RECORDING

    private void BuildDictOfMuscle()
	{
		m_PoseHandler.GetHumanPose(ref m_HumanPose);

		AddCurve("RootT.x", m_HumanPose.bodyPosition.x);
		AddCurve("RootT.y", m_HumanPose.bodyPosition.y);
		AddCurve("RootT.z", m_HumanPose.bodyPosition.z);
		
		AddCurve("RootQ.x", m_HumanPose.bodyRotation.x);
		AddCurve("RootQ.y", m_HumanPose.bodyRotation.y);
		AddCurve("RootQ.z", m_HumanPose.bodyRotation.z);
		AddCurve("RootQ.w", m_HumanPose.bodyRotation.w);
		
		AddCurve("LeftHandT.x", m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).transform.position.x);
		AddCurve("LeftHandT.y", m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).transform.position.y);
		AddCurve("LeftHandT.z", m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).transform.position.z);
		
		AddCurve("LeftHandQ.x", m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).transform.rotation.x);
		AddCurve("LeftHandQ.y", m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).transform.rotation.y);
		AddCurve("LeftHandQ.z", m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).transform.rotation.z);
		AddCurve("LeftHandQ.w", m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).transform.rotation.w);
		
		AddCurve("RightHandT.x", m_Animator.GetBoneTransform(HumanBodyBones.RightHand).transform.position.x);
		AddCurve("RightHandT.y", m_Animator.GetBoneTransform(HumanBodyBones.RightHand).transform.position.y);
		AddCurve("RightHandT.z", m_Animator.GetBoneTransform(HumanBodyBones.RightHand).transform.position.z);
		
		AddCurve("RightHandQ.x", m_Animator.GetBoneTransform(HumanBodyBones.RightHand).transform.position.x);
		AddCurve("RightHandQ.y", m_Animator.GetBoneTransform(HumanBodyBones.RightHand).transform.rotation.y);
		AddCurve("RightHandQ.z", m_Animator.GetBoneTransform(HumanBodyBones.RightHand).transform.rotation.z);
		AddCurve("RightHandQ.w", m_Animator.GetBoneTransform(HumanBodyBones.RightHand).transform.rotation.w);
		
		AddCurve("LeftFootT.x", m_Animator.GetBoneTransform(HumanBodyBones.LeftFoot).transform.position.x);
		AddCurve("LeftFootT.y", m_Animator.GetBoneTransform(HumanBodyBones.LeftFoot).transform.position.y);
		AddCurve("LeftFootT.z", m_Animator.GetBoneTransform(HumanBodyBones.LeftFoot).transform.position.z);
		
		AddCurve("LeftFootQ.x", m_Animator.GetBoneTransform(HumanBodyBones.LeftFoot).transform.rotation.x);
		AddCurve("LeftFootQ.y", m_Animator.GetBoneTransform(HumanBodyBones.LeftFoot).transform.rotation.y);
		AddCurve("LeftFootQ.z", m_Animator.GetBoneTransform(HumanBodyBones.LeftFoot).transform.rotation.z);
		AddCurve("LeftFootQ.w", m_Animator.GetBoneTransform(HumanBodyBones.LeftFoot).transform.rotation.w);
		
		AddCurve("RightFootT.x", m_Animator.GetBoneTransform(HumanBodyBones.RightFoot).transform.position.x);
		AddCurve("RightFootT.y", m_Animator.GetBoneTransform(HumanBodyBones.RightFoot).transform.position.y);
		AddCurve("RightFootT.z", m_Animator.GetBoneTransform(HumanBodyBones.RightFoot).transform.position.z);
		
		AddCurve("RightFootQ.x", m_Animator.GetBoneTransform(HumanBodyBones.RightFoot).transform.rotation.x);
		AddCurve("RightFootQ.y", m_Animator.GetBoneTransform(HumanBodyBones.RightFoot).transform.rotation.y);
		AddCurve("RightFootQ.z", m_Animator.GetBoneTransform(HumanBodyBones.RightFoot).transform.rotation.z);
		AddCurve("RightFootQ.w", m_Animator.GetBoneTransform(HumanBodyBones.RightFoot).transform.rotation.w);

		for (int i = 0; i < HumanTrait.BoneCount; ++i)
		{
			try
			{
				string s = MuscleNameCheck(HumanTrait.MuscleName[HumanTrait.MuscleFromBone(i, 0)]);
				AddCurve(s, m_HumanPose.muscles[HumanTrait.MuscleFromBone(i, 0)]);
			}
			catch
			{
				// ignored
			}
		
			try
			{
				string s = MuscleNameCheck(HumanTrait.MuscleName[HumanTrait.MuscleFromBone(i, 1)]);
				AddCurve(s, m_HumanPose.muscles[HumanTrait.MuscleFromBone(i, 1)]);
			}
			catch
			{
				// ignored
			}
		
			try
			{
				string s = MuscleNameCheck(HumanTrait.MuscleName[HumanTrait.MuscleFromBone(i, 2)]);
				AddCurve(s, m_HumanPose.muscles[HumanTrait.MuscleFromBone(i, 2)]);
			}
			catch
			{
				// ignored
			}
		}
		
		time += 1/60f;
	}

	private string MuscleNameCheck(string s)
	{
		if (fingers.Any(c => s.Contains(c)))
		{
			var c = s.Split(" ");
			s = c.Length == 3
				? c[0] + "Hand." + c[1] + "." + c[2]
				: c[0] + "Hand." + c[1] + "." + c[2] + " " + c[3];
		}

		return s;
	}

	private void AddCurve(string s, float val)
	{
		if (!m_MuscleCurve.ContainsKey(s))
		{
			var curve = new AnimationCurve();
			curve.AddKey(time, val);
			m_MuscleCurve.Add(s, curve);
		}
		else
		{
			m_MuscleCurve[s].AddKey(time, val);
		}
	}
	#endregion
	
	#region CSV RECORDING
	private IEnumerator Recording() {

        while (m_RecordingCSV) 
        {
            for (int i = (int)HumanBodyBones.Hips; i < (int)HumanBodyBones.LastBone; i++) 
            { 
                var t = m_Animator.GetBoneTransform((HumanBodyBones)i);
                if (t == null) 
                {
					m_TransformList.Add(0.0f); m_TransformList.Add(0.0f);
					m_TransformList.Add(0.0f); m_TransformList.Add(0.0f);
					m_TransformList.Add(0.0f); m_TransformList.Add(0.0f);
				}
				else
				{
					m_TransformList.Add(t.position.x); m_TransformList.Add(t.position.y); m_TransformList.Add(t.position.z);
					m_TransformList.Add(t.eulerAngles.x); m_TransformList.Add(t.eulerAngles.y); m_TransformList.Add(t.eulerAngles.z);
				}
			}

			yield return null;
		}
	}

    private void WriteToCsv(string filePath) {

		m_Writer = new StreamWriter(filePath);

		for (int i = (int)HumanBodyBones.Hips; i < (int)HumanBodyBones.LastBone; i++)
		{
			m_Writer.Write((HumanBodyBones)i + "PosX, ");
			m_Writer.Write((HumanBodyBones)i + "PosY, ");
			m_Writer.Write((HumanBodyBones)i + "PosZ, ");
			m_Writer.Write((HumanBodyBones)i + "RotX, ");
			m_Writer.Write((HumanBodyBones)i + "RotY, ");
			m_Writer.Write((HumanBodyBones)i + "RotZ, ");
		}

		for (int i = 0; i < m_TransformList.Count; i++) 
        {
			if (i % 330 == 0)
			{
				m_Writer.WriteLine();
			}
			var t = m_TransformList[i];
			m_Writer.Write(t.ToString(invC) + ", ");      
		}

		m_Writer.Flush();
		m_Writer.Close();
    }

	// public void ReadFromCsv(string filePath) 
	// {
	// 	if (File.Exists(filePath))
	// 	{
	// 		StartCoroutine(CopyFrameToHumanoid(filePath));
	// 	}
	// }
	//
	// private IEnumerator CopyFrameToHumanoid(string filePath) 
	// {
	// 	string[] lines = File.ReadAllLines(filePath);
	// 	int i = 1;
	// 	while (i < lines.Length) 
	// 	{
	// 		string[] values = lines[i].Split(',');
	// 		for (int j = (int)HumanBodyBones.Hips; j < (int)HumanBodyBones.LastBone; j++) 
	// 		{
	// 			var t = m_Animator.GetBoneTransform((HumanBodyBones)j);
	// 			if (t != null) { 
	// 				var p = new Vector3(float.Parse(values[j * 6 + 0], invC), float.Parse(values[j * 6 + 1], invC), float.Parse(values[j * 6 + 2], invC));
	// 				var r = new Vector3(float.Parse(values[j * 6 + 3], invC), float.Parse(values[j * 6 + 4], invC), float.Parse(values[j * 6 + 5], invC));
	// 				t.position = p;
	// 				t.eulerAngles = r;
	// 			}
	// 		}
	//
	// 		i++;
	// 		yield return null;
	// 	}
	// }
	#endregion
	
	private string GetCsvPath()
	{
#if UNITY_EDITOR
		return Application.dataPath + "/" + "animation.csv";
#elif UNITY_ANDROID
        return Application.persistentDataPath+ "/"+"animation.csv";
#elif UNITY_IPHONE
        return Application.persistentDataPath+"/"+"animation.csv";
#else
        return Application.dataPath +"/"+"animation.csv";
#endif
	}

}

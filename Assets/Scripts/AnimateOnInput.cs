using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

[System.Serializable]
public class AnimationInput
{
    public string animationPropertyName;
    public InputActionProperty action;
}

public class AnimateOnInput : MonoBehaviour
{
    public List<AnimationInput> animationInputs;
    
    private Animator m_Animator;

    private void Awake()
    {
        m_Animator = this.GetComponent<Animator>();

        Debug.Log("AWAKE");
    }

    private void Start()
    {
        Debug.Log("START");
    }

    private void Update()
    {
        foreach (var item in animationInputs)
        {
            float actionValue = item.action.action.ReadValue<float>();
            m_Animator.SetFloat(item.animationPropertyName, actionValue);
        }
    }

    private void OnEnable()
    {
        foreach (var param in m_Animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Float)
            {
                param.defaultFloat = 0f;
            }
        }

    }
    
    private void OnDisable()
    {
        // m_Animator.WriteDefaultValues();
        // m_Animator.writeDefaultValuesOnDisable = true;
        foreach (var param in m_Animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Float)
            {
                param.defaultFloat = 0f;
            }
        }
    }
}

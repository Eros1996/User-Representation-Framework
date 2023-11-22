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
        m_Animator.writeDefaultValuesOnDisable = true;
    }

    void LateUpdate()
    {
        foreach (var item in animationInputs)
        {
            float actionValue = item.action.action.ReadValue<float>();
            m_Animator.SetFloat(item.animationPropertyName, actionValue);
        }
    }
}

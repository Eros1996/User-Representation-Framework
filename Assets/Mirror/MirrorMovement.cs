using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorMovement : MonoBehaviour
{
    private Transform playerTarget;
    private Transform mirror;
        
    // Start is called before the first frame update
    void Awake()
    {
        playerTarget = Camera.main.transform;
        mirror = this.transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 localPlayer = mirror.InverseTransformPoint(playerTarget.position);
        transform.position = mirror.TransformPoint(new Vector3(localPlayer.x, localPlayer.y, -localPlayer.z));

        Vector3 lookAtMirror = mirror.InverseTransformPoint(new Vector3(-localPlayer.x, localPlayer.y, localPlayer.z));
        transform.LookAt(lookAtMirror);
    }
}

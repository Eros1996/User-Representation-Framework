using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitXROrigin : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var XRO = this.transform;
        var CamO = GameObject.Find("Camera Offset").transform;
        var MainC = GameObject.Find("Main Camera").transform;

        var initPosition = Vector3.zero;
        var initRotation = Quaternion.Euler(Vector3.zero);

        XRO.position = initPosition;
        XRO.rotation = initRotation;
        CamO.position = initPosition;
        CamO.rotation = initRotation;
        MainC.position = initPosition;
        MainC.rotation = initRotation;
        
        Debug.Log("Position XR Origin: " + XRO.position + ", Camera Offset: " + CamO.position + ", Main Camera: " + MainC.position);
        Debug.Log("Rotation XR Origin: " + XRO.rotation + ", Camera Offset: " + CamO.rotation + ", Main Camera: " + MainC.rotation);
    }
}

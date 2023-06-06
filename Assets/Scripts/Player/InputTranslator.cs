using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputTranslator : MonoBehaviour
{
    [SerializeField]
    Transform CHub;
    CameraMount CM;
    [SerializeField]
    Transform Player;
    PlayerMotor PM;

    public Transform marker;

    public float xsensitivity;
    //uses its transform as reference field for what inputs should be relative to.

    public Vector3 InputEllipsed;


    public Vector3 InputCameraSpace;

    // Start is called before the first frame update
    void Start()
    {
        PM = Player.GetComponent<PlayerMotor>();
        CM = CHub.GetComponent<CameraMount>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (PM.mode == PlayerTransitMode.Walking) {
            float InputX = Input.GetAxis("Horizontal");
            float InputY = Input.GetAxis("Vertical");

            Vector3 inputDir = new Vector3(InputX, 0, InputY);

            inputDir = Quaternion.AngleAxis(CM.internalYaw, Vector3.up) * inputDir;


            InputCameraSpace = inputDir;




            marker.position = PM.transform.position + inputDir + Vector3.up;
        } 
    }

}

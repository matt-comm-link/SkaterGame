using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum CameraMode 
{
    PlayerFollow,
    playerControlled,
    Fixed,
    Rail,
    Scene
}

public class CameraMount : MonoBehaviour
{


    [SerializeField]
    Transform Player;
    [SerializeField]
    Transform PlayerLookPoint;
    Transform camera;

    PlayerMotor pm;

    [SerializeField]
    bool invertx, inverty;

    public float sensitivityX, sensitivityY;
    public float internalYaw, internalPitch;
    [SerializeField]
    float maxPitch, minPitch;
    public Vector3 CamFar;
    public Vector3 CamNear;

    [SerializeField]
    float snaprate;

    public CameraMode mode;

    // Start is called before the first frame update
    void Start()
    {
        camera = transform.GetChild(0);
        pm = Player.GetComponent<PlayerMotor>();
    }

    // Update is called once per frame
    void Update()
    {
        if (pm.Skating)
            mode = CameraMode.PlayerFollow;
        else
            mode = CameraMode.playerControlled;



        if(mode == CameraMode.playerControlled) 
        {
            transform.position = Vector3.Lerp(transform.position, Player.position, snaprate);

            if(invertx)
                internalYaw += Input.GetAxis("LookX") * sensitivityX;
            else
                internalYaw -= Input.GetAxis("LookX") * sensitivityX;

            if(inverty)
                internalPitch += Input.GetAxis("LookY") * sensitivityY;
            else
                internalPitch -= Input.GetAxis("LookY") * sensitivityY;

            internalPitch = Mathf.Clamp(internalPitch, minPitch, maxPitch);

            transform.rotation = Quaternion.identity * Quaternion.AngleAxis(internalYaw, Vector3.up) * Quaternion.AngleAxis(internalPitch, Vector3.right);

            camera.LookAt(PlayerLookPoint.position, Vector3.up);

        }

        if (mode == CameraMode.PlayerFollow)
        {
            transform.position = Vector3.Lerp(transform.position, Player.position, snaprate);
            transform.rotation = Quaternion.Slerp(transform.rotation, Player.rotation, snaprate);
        }
    }
}

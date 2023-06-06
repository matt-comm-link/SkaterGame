using UnityEngine;
using System.Collections.Generic;

public enum PlayerTransitMode 
{
    Frozen,
    Interact,
    Staggered,
    Recover,
    Standing,
    Walking,
    Pushing,
    Coast,
    Glide,
    Jump,
    Fall,
    Rail,
    RailGlide
}


public class PlayerMotor : MonoBehaviour
{
    Rigidbody rb;
    [SerializeField]
    InputTranslator IT;
    [SerializeField]
    CameraMount CM;
    //Keep in mind the RB does rotate a bit to allow for slopes
    [SerializeField]
    List<Transform> groundcasters = new List<Transform>();
    [SerializeField]
    float groundedRange; //How far down from the casters to be considered grounded
    [SerializeField]
    float groundedStickRange; //Stick to ground if grounded

    [SerializeField]
    List<PhysicMaterial> PMs;

    float attackBar;
    float friction;
    float itimer;
    float staggerTimer;
    float recoverTimer;
    float interactTimer;
    float currentSpeed;
    public bool grounded;
    float airTime;
    bool enteredJumpFromGlide;
    public bool Skating;
    public float gravity;
    public float downForce;

    public PlayerTransitMode mode;
    public bool attacking;

    [SerializeField]
    float walkForce;
    public float pushForce;
    [SerializeField]
    bool pushForbidden;

    [SerializeField]
    float pushFriction, glideFriction, railFriction, airFriction, walkFriction, topPushSpeed;
    [SerializeField]
    float recoveriTimer, landiTimer, hitStaggerTime, hscrashStaggerTime, crashStaggerTime;
    [SerializeField]
    float PushThreshold, StandThreshold, pushTurnFraction, glideTurnFraction, coastTurnFraction, fallStrafeFraction;
    [SerializeField]
    float jumpForce, minJumpForce, jumpGlideMod, jumpDuration, fallSlow;

    [SerializeField]
    float walkturnrate;

    [SerializeField]
    float SelfRightingForce; //IDK if I need this

    //r value of the ellipse.  
    //According to some guy on the internet you can map a circle onto an ellipse with the same area with x = x/r, y = yr or something like that.
    //I'm a little dubious as to if this is true but 
    public float minEllipse, maxEllipse;
    //assume we start stretching at 0
    [SerializeField]
    float ellipseStretchEnd;

    [SerializeField]
    Vector2 ViewLocalInput; //Just to see what the input is doing locally


    Vector3 velocity;

    RigidbodyConstraints walkConstraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY |  RigidbodyConstraints.FreezeRotationZ;
    RigidbodyConstraints pushConstraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionX;
    RigidbodyConstraints jumpConstraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        //rb.isKinematic = true;
    }
    private void FixedUpdate()
    {
        DetermineState();

        //Allow for different constrained movements based on whether in the air or pushing.
        //Can't slide sideways while skating but can move fine when walking. 
        if (!grounded)
            rb.constraints = jumpConstraints;
        else if (Skating)
            rb.constraints = pushConstraints;
        else
            rb.constraints = walkConstraints;



        float InputX = Input.GetAxis("Horizontal");
        float InputY = Input.GetAxis("Vertical");
        Vector3 moveVector = Vector3.zero;
        Vector3 turnVector = Vector3.zero;


        //Input transformed into camera space
        Vector3 InputDir = IT.InputCameraSpace;

        //camera input relative to the RB direction, ellipse it based on how fast we're going, and then return it to world space.
        Vector3 LocalInputDir = transform.InverseTransformDirection(InputDir);
        float ellipseR = Mathf.Lerp(minEllipse, maxEllipse, Mathf.Clamp01(rb.velocity.magnitude / ellipseStretchEnd));
        Vector3 LocalInputOnEllipsis = new Vector3(LocalInputDir.x * 1 * ellipseR, 0, LocalInputDir.z / ellipseR);

        Vector3 AdjustedInputInWorldspace = transform.TransformDirection(LocalInputOnEllipsis);



        ViewLocalInput = new Vector2(LocalInputOnEllipsis.x, LocalInputOnEllipsis.z);

        //Get angle between forward and where we're going
        float InputAngleChange = Vector3.SignedAngle(transform.forward, InputDir, Vector3.up);
        //limit by ellipse R. TBH I'm just making this up as I go along.
        float appliedInput = Mathf.Sign(InputAngleChange) * InputAngleChange * ellipseR;

        if (mode > PlayerTransitMode.Recover && mode < PlayerTransitMode.Rail)
        {
            //go forth!
            rb.AddForce(new Vector3(AdjustedInputInWorldspace.x, 0, AdjustedInputInWorldspace.z) * walkForce, ForceMode.Force);

            transform.rotation *= Quaternion.AngleAxis(LocalInputOnEllipsis.x * walkturnrate, transform.up);

            //transform.rotation *= Quaternion.AngleAxis(appliedInput * Time.fixedDeltaTime, transform.up);

        }

        //APPLY ROTATION BASED ON where the stick is held relative to the camera
        //JESUS CHRIST MATT COMMENT YOUR CODE! 
        #region old code
        //I DON'T KNOW WHAT I WAS GUNNING FOR HERE BUT I REMEMBER GETTING STUCK SO LET'S TRY SOMETHING NEW
        /*
        float InputAngleChange = Vector3.SignedAngle(transform.forward, InputDir, Vector3.up);
        float slowValue = Mathf.Clamp((rb.velocity.magnitude - turnRateStartSlow) / (turnRateStopSlow - turnRateStartSlow), 0, 1);
        float upperBounds = Mathf.Lerp(turnRateMax, turnRateMin, slowValue);
        float pull = InputDir.magnitude;
        float appliedInput = Mathf.Sign(InputAngleChange) * Mathf.Clamp(Mathf.Abs(InputAngleChange) * pull, 0, upperBounds);
        if(mode > PlayerTransitMode.Recover && mode < PlayerTransitMode.Rail) 
        {
            transform.rotation *= Quaternion.AngleAxis(appliedInput * Time.fixedDeltaTime, transform.up);

        }
        */
        #endregion


        switch (mode) 
        {
            //No movement here
            case PlayerTransitMode.Frozen:
                rb.velocity = Vector3.zero;
                break;
            case PlayerTransitMode.Interact:
                rb.velocity = Vector3.zero;
                interactTimer -= Time.fixedDeltaTime;
                if (interactTimer <= 0)
                    mode = PlayerTransitMode.Standing;
                break;
            case PlayerTransitMode.Staggered:
                staggerTimer -= Time.fixedDeltaTime;
                rb.velocity = Vector3.zero;
                //transition to recovery mode
                if (staggerTimer <= 0) 
                    mode = PlayerTransitMode.Recover;
                break;
            case PlayerTransitMode.Recover:
                recoverTimer -= Time.fixedDeltaTime;
                rb.velocity = Vector3.zero;
                //Transition to standing mode. Add post-recovery iFrames
                if (recoverTimer <= 0) 
                {
                    mode = PlayerTransitMode.Standing;
                    itimer = recoveriTimer;
                }

                break;
            //This is just for animation, you're not doing anything;
            case PlayerTransitMode.Standing:
                //cut velocity by walk friction
                friction = (Time.fixedDeltaTime * walkFriction);
                //press into ground
                rb.AddForce(-transform.up * downForce, ForceMode.Force);
                break;
            //Movement, 3rd person character movement with untethered orbit camera. 
            case PlayerTransitMode.Walking:
                //Move around in 3D space based on the inputs
                //rb.AddRelativeForce(moveVector * walkForce, ForceMode.Force);


                //transform.rotation = Quaternion.Euler(0, CM.internalYaw, 0);
                //Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.up, moveVector), walkturnrate);

                //cut velocity by walk friction
                friction = (Time.fixedDeltaTime * walkFriction);

                //press into ground
                rb.AddForce(-transform.up * downForce, ForceMode.Force);
                break;
            //Movement, 3rd person inertia based movement with push velocity increases and push speed limit
            case PlayerTransitMode.Pushing:
                //Push forwards based on the inputs and animation state, turn gradually
                //only push if not going top speed
                if(rb.velocity.magnitude < topPushSpeed)
                    rb.velocity += Vector3.forward * InputDir.y * pushForce * Time.deltaTime;

               // turnVector = new Vector3(0, InputX * pushTurnFraction, 0);
                //rb.AddTorque(turnVector, ForceMode.Force);

                //cut velocity by push friction
                friction = (Time.fixedDeltaTime * pushFriction);

                //press into ground
                rb.AddForce(-transform.up * downForce, ForceMode.Force);

                break;
            //Movement, 3rd person inertia based movement with push levels of friction and increased turning rates
            case PlayerTransitMode.Coast:
                //slow down by the push friction value turn less gradually
                //turnVector = new Vector3(0, InputX * coastTurnFraction, 0);
                //rb.AddTorque(turnVector, ForceMode.Force);

                //cut velocity by push friction
                friction = (Time.fixedDeltaTime * pushFriction);

                //press into ground
                rb.AddForce(-transform.up * downForce, ForceMode.Force);
                break;
            //Movement, 3rd person inertia based movement with glide levels of friction & reduced ability to turn, increased jump force (in jump start code)
            case PlayerTransitMode.Glide:
                //slow down by the glide friction value turn gradually
                //turnVector = new Vector3(0, InputX * glideTurnFraction, 0);
                //rb.AddTorque(turnVector, ForceMode.Force);

                //cut velocity by glide friction, which is not much
                friction = (Time.fixedDeltaTime * glideFriction);

                //press into ground
                rb.AddForce(-transform.up * downForce, ForceMode.Force);

                break;
            //Movement, 3rd person inertia based movement midair with jump levels of friction & limited ability to turn
            case PlayerTransitMode.Jump:
                //Add upwards force if jumping duration not exceeded
                if(Input.GetButton("Jump") && airTime < jumpDuration)
                    moveVector.y += Mathf.Lerp(jumpForce, minJumpForce, airTime/jumpDuration);
                if (enteredJumpFromGlide)
                    moveVector *= jumpGlideMod;

                moveVector += Vector3.forward * InputDir.y * pushForce * fallStrafeFraction;
                moveVector *= Time.deltaTime;

                rb.velocity += moveVector;

                //rb.AddForce(moveVector * pushForce, ForceMode.Force);

                //Turn gradually
                //turnVector = new Vector3(0, InputX * glideTurnFraction, 0);
                //rb.AddTorque(turnVector, ForceMode.Force);

                //cut velocity by air friction
                friction = (Time.fixedDeltaTime * airFriction);

                break;
            //Movement, 3rd person inertia based falling midair with jump levels of friction & no ability to turn but some strafing for fine landing
            case PlayerTransitMode.Fall:

                //slow fall if jump is held
                if (Input.GetButton("Jump"))
                    moveVector.y += fallSlow;
                if (enteredJumpFromGlide)
                    moveVector *= jumpGlideMod;

                moveVector += Vector3.forward * InputDir.y * pushForce * fallStrafeFraction;
                moveVector.x += InputX * fallStrafeFraction;
                moveVector *= Time.deltaTime;

                rb.velocity += moveVector;

                //Strafe gradually

                //Add upwards force if jumping duration not exceeded
                rb.AddRelativeForce(moveVector * pushForce, ForceMode.Force);



                //cut velocity by push friction
                friction = (Time.fixedDeltaTime * airFriction);

                break;
            //Movement, 3rd person inertia based movement stuck to rail, with basic downhill speed gain and rail friction 
            case PlayerTransitMode.Rail:
                //NOT IMPLEMENTED UNTIL I IMPLEMENT RAIL GRINDING
                rb.velocity += (-Vector3.up * downForce * Time.deltaTime);

                break;
            //Movement, 3rd person inertia based movement stuck to rail, with basic downhill speed gain and glide friction 
            case PlayerTransitMode.RailGlide:
                //NOT IMPLEMENTED UNTIL I IMPLEMENT RAIL GRINDING
                rb.velocity += (-Vector3.up * downForce * Time.deltaTime);
                break;

        }

        //OK, if Drag is F = (fluid * coefficient * Area * speed^2)/2 and we assume f,c and a to be constant, it's just coefficient speed squared over 2.
        // F = MA, A = F/M

        Vector3 dragacc = -transform.forward * ((friction * rb.velocity.magnitude * rb.velocity.magnitude) / (2 * rb.mass));

        GetComponent<Collider>().material.dynamicFriction = friction;

        if (mode != PlayerTransitMode.Frozen && itimer > 0) 
        {
            itimer -= Time.fixedDeltaTime;
            //apply drag
            //rb.velocity += dragacc;
            //rb.velocity += -Vector3.up * gravity * Time.fixedDeltaTime;
        }

        velocity = rb.velocity;

        //For testing I'm turning off movement
        //rb.velocity = Vector3.zero;
    }

    //Determine variables about the mode the character is in right now
    void DetermineState() 
    {

        //Skip past modes that end on a timer
        if (mode == PlayerTransitMode.Frozen)
            return;
        if (mode == PlayerTransitMode.Interact)
            return;
        if (mode == PlayerTransitMode.Staggered)
            return;
        if (mode == PlayerTransitMode.Recover)
            return;

        currentSpeed = rb.velocity.magnitude;


        //Determine moving/standing state

        //GROUNDED CODE HERE
        bool groundedOld = grounded;

        grounded = false;
        RaycastHit hit;
        for (int i = 0; i < groundcasters.Count; i++)
        {
            Physics.Raycast(groundcasters[i].position, -transform.up, out hit, groundedRange);
            if (hit.collider != null)
                grounded = true;
        }

        //Add post-fall iFrames
        if (groundedOld && !grounded) 
        {
            if (mode == PlayerTransitMode.Glide)
                enteredJumpFromGlide = true;
            itimer = landiTimer;
        }

        //if in the air
        if (!grounded)
        {
            Skating = false;
            friction = airFriction;
            if (rb.velocity.y > 0)
                mode = PlayerTransitMode.Jump;
            else 
                mode = PlayerTransitMode.Fall;
            return;
        }
        //reset if you're grounded
        enteredJumpFromGlide = false;

        //determine if in rollerskating mode
        if (!Skating && rb.velocity.magnitude > PushThreshold)
            Skating = true;
        if (Skating && rb.velocity.magnitude < StandThreshold)
            Skating = false;
        if(pushForbidden)
            Skating = false;

        //if skating physics apply
        if (Skating)
        {
            friction = pushFriction;
            if (Input.GetButton("Glide")) 
            {
                friction = glideFriction;
                mode = PlayerTransitMode.Glide;
            }
            else if (Input.GetAxis("Vertical") != 0)
                mode = PlayerTransitMode.Pushing;
            else
                mode = PlayerTransitMode.Coast;
        }
        //walk/move around normally
        else
        {
            friction = walkFriction;
            if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
                mode = PlayerTransitMode.Walking;
            else
                mode = PlayerTransitMode.Standing;
        }


    }


    //Stagger in some cases
    //NOT IMPLEMENTED YET
    void TakeAttack() 
    {
        if (itimer > 0)
            return;
    }
}

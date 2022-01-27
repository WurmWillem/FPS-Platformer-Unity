using System;
using System.Collections;
using UnityEngine;

public class DaniMovement : MonoBehaviour
{
    //Assingables
    public Transform playerCam;
    public Transform orientation;
    public ParticleSystem jumpParticles;

    //Other
    private Rigidbody rb;

    //Rotation and look
    private float xRotation;
    private float sensitivity = 75f;
    private float sensMultiplier = 1f;

    //Movement
    [SerializeField] private int moveSpeed = 4500;
    [SerializeField] private int maxSpeed = 30;
    [SerializeField] private bool grounded;
    [SerializeField] LayerMask whatIsGround;

    //Counter movement
    [SerializeField] private float counterMovement = 0.175f;
    private float threshold = 0.01f;
    [SerializeField] private float maxSlopeAngle = 35f;
    [SerializeField] private float counterMovementAir = 0.01f;

    //Jumping
    private bool readyToJump = true;
    private float jumpCooldown = 0.3f;
    [SerializeField] private float jumpForce = 550f;
    private int jumps = 1;

    //Input
    private float x, y;
    private bool jumping, sprinting, isDashing, crouching, restart, test;

    //Sliding
    private Vector3 normalVector = Vector3.up;
    //private Vector3 wallNormalVector;

    //Dash
    [SerializeField] private float dashSpeed = 4000f;
    [SerializeField] private float dashDuration = 0.35f;

    private float dashCooldown = 0.1f;
    private float dashCheck = 0f;
    private bool canDash = true;
    private bool canDashIfGrounded;

    //Reset
    private Vector3 playerPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        playerPos = transform.localPosition;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Update()
    {
        MyInput();
        Look();
    }

    // Find user input
    private void MyInput()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");

        jumping = Input.GetButton("Jump");
        isDashing = Input.GetKey(KeyCode.LeftShift);

        restart = Input.GetKey(KeyCode.R);
        test = Input.GetKey(KeyCode.LeftAlt);
    }

    private void Movement()
    {
        //Extra gravity
        rb.AddForce(Vector3.down * Time.deltaTime * 10);

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);

        //reset jumps and dashframes
        if (grounded) jumps = 1; 

        //reset dash
        if (grounded && canDashIfGrounded)
        {
            canDash = true;
        }

        //If holding jump && ready to jump, then jump
        if (readyToJump && jumping) Jump();

        if (restart) RestartGame();

        if (test) Debug.Log(rb.velocity);
        //Set max speed
        float maxSpeed = this.maxSpeed;

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        //Some multipliers
        float multiplier = 1f, multiplierForward = 1f, multiplierSide = 1f;

        // Movement in air
        if (!grounded)
        {
            multiplier = 0.95f;
            multiplierForward = 0.95f;
            multiplierSide = 0.75f;
        }

        // activate dash
        if (isDashing && canDash)
        {
            StartCoroutine(Dash());
        }
        
        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierForward);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier * multiplierSide);
    } 

    private IEnumerator Dash()
    {
        if (Time.time > dashCheck)
        {
            //Debug.Log("hello");
            float timePassed = 0f;
            canDash = false;
            canDashIfGrounded = false;
            while (timePassed < dashDuration)
            {
                timePassed += Time.deltaTime;

                Vector3 vel = rb.velocity;

               rb.AddForce(orientation.transform.forward * dashSpeed * Time.deltaTime); 


                rb.velocity = new Vector3(vel.x, 0, vel.z);
                
                yield return null;
            }

            Vector3 vel1 = rb.velocity;
            rb.velocity = new Vector3(vel1.x * 0.7f, vel1.y * 0.7f, vel1.z * 0.7f);
            //Debug.Log(rb.velocity);
            yield return new WaitForSeconds(0.25f);
            canDashIfGrounded = true;
            //Debug.Log("reset dash");
            
        }
        dashCheck = dashCooldown + Time.time;
    }

    private void Jump()
    {
        if ((grounded || jumps > 0) && readyToJump)
        {
            readyToJump = false;
            jumps -= 1;
            Instantiate(jumpParticles, transform.position, transform.rotation);

            //Add jump forces
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            //If jumping while falling, reset y velocity.
            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f)
                rb.velocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.velocity.y > 0)
                rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        //if (jumping) return;

        if (grounded)
        {
            //Counter movement
            if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
            {
                rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
            }
            if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
            {
                rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
            }
        }

        if (!grounded)
        {
            if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
            {
                rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovementAir);
            }
            if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
            {
                rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovementAir);
            }

            //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
            if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed)
            {
                float fallspeed = rb.velocity.y;
                Vector3 n = rb.velocity.normalized * maxSpeed;
                rb.velocity = new Vector3(n.x, fallspeed, n.z);
            }
        }

        
    }

    private float desiredX;
    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = playerCam.transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Perform the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);
    }

    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    private bool cancellingGrounded;

    /// Handle ground detection
    private void OnCollisionStay(Collision other)
    {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal))
            {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        //Invoke ground/wall cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }
    private void RestartGame()
    {
        transform.localPosition = playerPos;
    }
    private void StopGrounded()
    {
        grounded = false;
    }

}
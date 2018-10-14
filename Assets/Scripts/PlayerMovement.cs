﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller3D))]
public class PlayerMovement : MonoBehaviour
{
    public CameraControler myCamera;
    public Transform rotateObj;
    public SkinnedMeshRenderer Body;
    public GameObject churroRojo;
    public GameObject churroAzul;
    public Material teamBlueMat;
    public Material teamRedMat;
    PlayerCombat myPlayerCombat;

    public GameController.controllerName contName;
    public Team team = Team.blue;
    [HideInInspector]
    public Controller3D controller;

    public enum Team
    {
        red,
        blue
    }
    [HideInInspector]
    public MoveState moveSt = MoveState.NotMoving;
    public enum MoveState
    {
        Moving,
        NotMoving,//Not stunned, breaking
        Knockback,//Stunned
        MovingBreaking,//Moving but reducing speed by breakAcc till maxMovSpeed
        Boost
    }
    [HideInInspector]
    public JumpState jumpSt = JumpState.none;
    public enum JumpState
    {
        Jumping,
        Breaking,//Emergency stop
        none
    }
    [HideInInspector]
    public bool noInput = false;
    Vector3 objectiveVel;
    [HideInInspector]
    public Vector3 currentVel;
    [Header("SPEED")]
    public float maxMoveSpeed = 10.0f;
    float maxMoveSpeed2; // is the max speed from which we aply the joystick sensitivity value
    float currentMaxMoveSpeed = 10.0f; // its the final max speed, after the joyjoystick sensitivity value
    [Tooltip("Maximum speed that you can travel at horizontally when hit by someone")]
    public float maxKnockbackSpeed = 300f;
    [HideInInspector]
    public float currentSpeed = 0;
    public float maxSpeedInWater = 5f;
    public float maxVerticalSpeedInWater = 3f;
    [Header("BOOST")]
    public float boostSpeed = 20f;
    public float boostCD = 5f;
    public float boostDuration = 1f;
    float boostTime = 0f;
    bool boostReady = true;
    [Header("ACCELERATIONS")]
    public float initialAcc = 2.0f;
    public float breakAcc = -2.0f;
    public float movingAcc = 2.0f;
    //public float breakAccOnHit = -2.0f;
    float gravity;
    [Header("JUMP")]
    public float jumpHeight = 4f;
    public float jumpApexTime = 0.4f;
    float jumpVelocity;
    float timePressingJump = 0.0f;
    float maxTimePressingJump;
    [Tooltip("How fast the 'stop jump early' stops in the air. This value is multiplied by the gravity and then applied to the vertical speed.")]
    public float breakJumpForce = 2.0f;
    [Tooltip("During how much part of the jump (in time to reach the apex) is the player able to stop the jump. 1 is equals to the whole jump, and 0.5 is equals the half of the jump time.")]
    public float pressingJumpActiveProportion = 0.7f;
    [Header("WALLJUMP")]
    public float wallJumpVelocity = 10f;
    public float stopWallMaxTime = 0.5f;
    float stopWallTime = 0;
    bool wallJumping = false;
    Vector3 anchorPoint;
    Vector3 wallNormal;
    [Tooltip("Vertical angle in which the player wall-jumps.")]
    public float wallJumpAngle=30;
    [Tooltip("Minimum horizontal angle in which the player wall-jumps. This number ranges from 0 to 90. 0 --> parallel to the wall; 90 --> perpendicular to the wall")]
    public float wallJumpMinHorizAngle = 30;
    float wallJumpRadius;
    float walJumpConeHeight = 1;
    GameObject lastWall;
    //bool wallJumped = false;

    public void SetVelocity(Vector3 vel)
    {
        objectiveVel = vel;
        currentVel = objectiveVel;
    }

    private void Awake()
    {
        currentSpeed = 0;
        noInput = false;
        controller = GetComponent<Controller3D>();
        myPlayerCombat = GetComponent<PlayerCombat>();
        lastWall = null;
    }
    public void KonoStart()
    {
        gravity = -(2 * jumpHeight) / Mathf.Pow(jumpApexTime, 2);
        jumpVelocity = Mathf.Abs(gravity * jumpApexTime);
        maxTimePressingJump = jumpApexTime * pressingJumpActiveProportion;
        wallJumpRadius = Mathf.Atan(wallJumpAngle*Mathf.Deg2Rad) * walJumpConeHeight;
        wallJumpMinHorizAngle = Mathf.Clamp(wallJumpMinHorizAngle, 0, 90);
        print("Gravity = " + gravity + "; Jump Velocity = " + jumpVelocity);
        Body.material = team == Team.blue ? teamBlueMat : teamRedMat;
        switch (team)
        {
            case Team.blue:
                churroAzul.SetActive(true);
                churroRojo.SetActive(false);
                break;
            case Team.red:
                churroAzul.SetActive(false);
                churroRojo.SetActive(true);
                break;
        }
        currentMaxMoveSpeed = maxMoveSpeed2 = maxMoveSpeed;
        //PRUEBAS
        Vector3 centro = new Vector3(1,1,1);
        float radio = 1;
        float angle = 270;
        float xpos = centro.x + (radio*Mathf.Cos(angle*Mathf.Deg2Rad));
        float zpos = centro.z + (radio * Mathf.Sin(angle * Mathf.Deg2Rad));
        Vector3 punto = new Vector3(xpos, centro.y, zpos);
        Debug.DrawLine(centro, punto, Color.yellow, 20);
    }
    int frameCounter = 0;
    public void KonoUpdate()
    {
        if (controller.collisions.above || controller.collisions.below)
        {
            //print("SETTING VEL.Y TO 0");
            currentVel.y = 0;
        }
        //print("FRAME NUMBER " + frameCounter);
        frameCounter++;
        ProcessStun();

        HorizontalMovement();
        //print("vel = " + currentVel.ToString("F4"));
        UpdateFacingDir();
        VerticalMovement();
        //print("vel = " + currentVel.ToString("F4"));

        //print("CurrentVel = " + currentVel);
        ProcessWallJump();//IMPORTANTE QUE VAYA ANTES DE LLAMAR A "MOVE"
        controller.Move(currentVel * Time.deltaTime);
        myPlayerCombat.KonoUpdate();
        controller.collisions.ResetAround();
    }

    [HideInInspector]
    public Vector3 currentMovDir;
    float joystickAngle;
    float deadzone = 0.15f;
    float joystickSens = 0;

    public void CalculateMoveDir()
    {
        float horiz = Input.GetAxisRaw(contName + "H");
        float vert = -Input.GetAxisRaw(contName + "V");
        //print("H = " + horiz + "; V = " + vert);
        // Check that they're not BOTH zero - otherwise
        // dir would reset because the joystick is neutral.
        Vector3 temp = new Vector3(horiz, 0, vert);
        joystickSens = temp.magnitude;
        //print("temp.magnitude = " + temp.magnitude);
        if (temp.magnitude >= deadzone && !noInput)
        {
            moveSt = MoveState.Moving;
            currentMovDir = temp;
            currentMovDir.Normalize();
            switch (myCamera.camMode)
            {
                case CameraControler.cameraMode.Fixed:
                    currentMovDir = RotateVector(-facingAngle, temp);
                    break;
                case CameraControler.cameraMode.Shoulder:
                    currentMovDir = RotateVector(-facingAngle, temp);
                    break;
                case CameraControler.cameraMode.Free:
                    Vector3 camDir = (transform.position-myCamera.transform.GetChild(0).position).normalized;
                    camDir.y = 0;
                    // ANGLE OF JOYSTICK
                    joystickAngle = Mathf.Acos(((0 * currentMovDir.x) + (1 * currentMovDir.z)) / (1 * currentMovDir.magnitude)) * Mathf.Rad2Deg;
                    joystickAngle = (horiz > 0) ? -joystickAngle : joystickAngle;
                    //rotate camDir joystickAngle degrees
                    currentMovDir=RotateVector(joystickAngle, camDir);
                    //print("joystickAngle= " + joystickAngle + "; camDir= " + camDir.ToString("F4") + "; currentMovDir = " + currentMovDir.ToString("F4"));
                    RotateCharacter();
                    break;
            }
        }
        else
        {
            moveSt = MoveState.NotMoving;
        }
    }

    void HorizontalMovement()
    {
        //------------------------------------------------ Direccion Joystick, aceleracion, maxima velocidad y velocidad ---------------------------------
        if (moveSt != MoveState.Knockback)
        {
            CalculateMoveDir();//Movement direction
        }
        if (!myPlayerCombat.LTPulsado && !myPlayerCombat.RTPulsado && Input.GetButtonDown(contName + "RB"))
        {
            myPlayerCombat.RTPulsado = true;
            StartBoost();
        }
        ProcessBoost();

        //------------------------------- Max Move Speed -------------------------------
        maxMoveSpeed2 = maxMoveSpeed;
        ProcessWater();
        if (joystickSens >= 0.88 || joystickSens > 1) joystickSens = 1;
        currentMaxMoveSpeed = (joystickSens / 1) * maxMoveSpeed2;

        //------------------------------- Acceleration -------------------------------
        float actAccel;
        if (moveSt == MoveState.Moving && currentSpeed < currentMaxMoveSpeed)
        {
            actAccel = initialAcc;
        }
        else if (moveSt == MoveState.MovingBreaking)
        {
            actAccel = breakAcc * 2;
        }
        else
        {
            actAccel = breakAcc;
        }
        //------------------------------- Speed ------------------------------ -
        currentSpeed = currentSpeed + actAccel * Time.deltaTime;
        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxKnockbackSpeed);
        Vector3 horizontalVel = new Vector3(currentVel.x, 0, currentVel.z);
        if (moveSt == MoveState.Moving && horizontalVel.magnitude > currentMaxMoveSpeed)
        {
            moveSt = MoveState.MovingBreaking;
        }
        //------------------------------------------------ DIRECCION CON VELOCIDAD ---------------------------------
        switch (moveSt)
        {
            case MoveState.Moving:
                currentVel = currentVel + currentMovDir * movingAcc;
                horizontalVel = new Vector3(currentVel.x, 0, currentVel.z);
                if (horizontalVel.magnitude > currentMaxMoveSpeed)
                {
                    horizontalVel = horizontalVel.normalized * currentMaxMoveSpeed;
                    currentVel = new Vector3(horizontalVel.x, currentVel.y, horizontalVel.z);
                }
                break;
            case MoveState.NotMoving:
                Vector3 aux = currentVel.normalized * currentSpeed;
                currentVel = new Vector3(aux.x, currentVel.y, aux.z);
                break;
            case MoveState.Boost:
                if (controller.collisions.collisionHorizontal)//BOOST CONTRA PARED
                {
                    WallBoost();
                }
                else//BOOST NORMAL
                {
                    currentVel = currentVel + currentMovDir * movingAcc;
                    horizontalVel = new Vector3(currentVel.x, 0, currentVel.z);
                    horizontalVel = horizontalVel.normalized * boostSpeed;
                    currentVel = new Vector3(horizontalVel.x, 0, horizontalVel.z);
                    currentSpeed = boostSpeed;
                }
                break;
            case MoveState.Knockback:
                currentVel = currentVel + knockback;
                currentSpeed = currentVel.magnitude;
                currentSpeed = Mathf.Clamp(currentSpeed, 0, maxKnockbackSpeed);
                moveSt = MoveState.NotMoving;
                break;
            case MoveState.MovingBreaking:
                Vector3 finalDir = currentVel + currentMovDir * movingAcc;
                horizontalVel = new Vector3(finalDir.x, 0, finalDir.z);
                currentVel = horizontalVel.normalized * currentSpeed;
                currentVel.y = finalDir.y;
                break;
        }
    }

    void VerticalMovement()
    {
        if(lastWall!=null && controller.collisions.below)
        {
            lastWall = null;
        }
        if (Input.GetButtonDown(contName + "A"))
        {
            //print("JUMP");
            StartJump();
        }

        switch (jumpSt)
        {
            case JumpState.none:
                currentVel.y += gravity * Time.deltaTime;
                break;
            case JumpState.Jumping:
                currentVel.y += gravity * Time.deltaTime;
                timePressingJump += Time.deltaTime;
                if (timePressingJump >= maxTimePressingJump-maxTimePressingJump/3)
                {
                    StopJump();
                }
                else
                {
                    if (Input.GetButtonUp(contName + "A"))
                    {
                        jumpSt = JumpState.Breaking;
                    }
                }
                break;
            case JumpState.Breaking:
                currentVel.y += (gravity*breakJumpForce) * Time.deltaTime;
                if (currentVel.y <= 0)
                {
                    jumpSt = JumpState.none;
                }

                break;


        }
        if (inWater)
        {
            currentVel.y = Mathf.Clamp(currentVel.y, -maxVerticalSpeedInWater, float.MaxValue);
        }

    }

    void StartJump()
    {
        if (controller.collisions.below && (!inWater || inWater && controller.collisions.around))
        {
            print("JUMP");
            currentVel.y = jumpVelocity;
            jumpSt = JumpState.Jumping;
            timePressingJump = 0;
        }
        else
        {
            StartWallJump();
        }
    }

    void StopJump()
    {
        jumpSt = JumpState.none;
        timePressingJump = 0;
    }

    void StartWallJump()
    {
        if(!controller.collisions.below && (!inWater || inWater && controller.collisions.around) && controller.collisions.collisionHorizontal && lastWall!= controller.collisions.wall)
        {
            print("WallJump");
            //wallJumped = true;
            stopWallTime = 0;
            currentVel = Vector3.zero;
            wallJumping = true;
            anchorPoint = transform.position;
            wallNormal = controller.collisions.wallNormal;
            wallNormal.y = 0;
            lastWall = controller.collisions.wall;
        }
    }

    void ProcessWallJump()
    {
        if (wallJumping)
        {
            currentVel = Vector3.zero;
            currentSpeed = 0;
            stopWallTime += Time.deltaTime;
            if (stopWallTime >= stopWallMaxTime)
            {
                EndWallJump();
            }
        }
    }

    void EndWallJump()
    {

        wallJumping = false;
        //CALCULATE JUMP DIR
        //LEFT OR RIGHT ORIENTATION?
        //Angle
        Vector3 circleCenter = anchorPoint + Vector3.up * walJumpConeHeight;
        Vector3 circumfPoint = CalculateReflectPoint(1, wallNormal, circleCenter);
        Vector3 finalDir = (circumfPoint - anchorPoint).normalized;
        Debug.LogWarning("FINAL DIR= " + finalDir.ToString("F4"));

        currentVel = finalDir * wallJumpVelocity;
        currentSpeed = currentVel.magnitude;
        currentMovDir = new Vector3(finalDir.x, 0, finalDir.z);
        RotateCharacter();


        Debug.DrawLine(anchorPoint, circleCenter, Color.white, 20);
        Debug.DrawLine(anchorPoint, circumfPoint, Color.yellow, 20);
    }

    void WallBoost()
    {
        //CALCULATE JUMP DIR
        Vector3 circleCenter = transform.position;
        Vector3 circumfPoint = CalculateReflectPoint(1, controller.collisions.wallNormal, circleCenter);
        Vector3 finalDir = (circumfPoint - circleCenter).normalized;
        Debug.LogWarning("FINAL DIR= " + finalDir.ToString("F4"));

        currentVel = finalDir * currentVel.magnitude;
        currentSpeed = currentVel.magnitude;
        currentMovDir = new Vector3(finalDir.x, 0, finalDir.z);
        RotateCharacter();
    }

    void StartBoost()
    {
        if (!noInput && boostReady && !haveFlag&& !inWater)
        {
            moveSt = MoveState.Boost;
            boostTime = 0f;
            boostReady = false;
        }

    }

    void ProcessBoost()
    {
        if (!boostReady)
        {
            boostTime += Time.deltaTime;
            if(boostTime < boostDuration)
            {
                moveSt = MoveState.Boost;
            }
            if (boostTime >= boostCD)
            {
                boostReady = true;
            }
        }
    }

    [HideInInspector]
    public Vector3 currentFacingDir = Vector3.forward;
    [HideInInspector]
    public float facingAngle = 0;

    void UpdateFacingDir()//change so that only rotateObj rotates, not whole body
    {
        switch (myCamera.camMode)
        {
            case CameraControler.cameraMode.Fixed:
                facingAngle = rotateObj.localRotation.eulerAngles.y;
                //Calculate looking dir of camera
                Vector3 camPos = myCamera.transform.GetChild(0).position;
                Vector3 myPos = transform.position;
                currentFacingDir = new Vector3(myPos.x - camPos.x, 0, myPos.z - camPos.z).normalized;
                break;
            case CameraControler.cameraMode.Shoulder:
                facingAngle = rotateObj.localRotation.eulerAngles.y;
                currentFacingDir = RotateVector(-myCamera.transform.localRotation.eulerAngles.y, Vector3.forward).normalized;
                //print("CurrentFacingDir = " + currentFacingDir);
                break;
            case CameraControler.cameraMode.Free:
                currentFacingDir = RotateVector(-rotateObj.localRotation.eulerAngles.y, Vector3.forward).normalized;
                facingAngle = rotateObj.localRotation.eulerAngles.y;
                break;
        }

    }

    public void RotateCharacter(float rotSpeed=0)
    {
        switch (myCamera.camMode)
        {
            case CameraControler.cameraMode.Fixed:
                Vector3 point1 = transform.position;
                Vector3 point2 = new Vector3(point1.x, point1.y + 1, point1.z);
                Vector3 dir = new Vector3(point2.x - point1.x, point2.y - point1.y, point2.z - point1.z);
                rotateObj.Rotate(dir, rotSpeed * Time.deltaTime);
                break;
            case CameraControler.cameraMode.Shoulder:
                point1 = transform.position;
                point2 = new Vector3(point1.x, point1.y + 1, point1.z);
                dir = new Vector3(point2.x - point1.x, point2.y - point1.y, point2.z - point1.z);
                rotateObj.Rotate(dir, rotSpeed * Time.deltaTime);
                break;
            case CameraControler.cameraMode.Free:
                float angle = Mathf.Acos(((0 * currentMovDir.x) + (1 * currentMovDir.z)) / (1 * currentMovDir.magnitude)) * Mathf.Rad2Deg;
                angle = currentMovDir.x < 0 ? -angle : angle;
                //print("ANGULO = " + angle);
                rotateObj.localRotation = Quaternion.Euler(0, angle, 0);
                break;
        }

    }

    [HideInInspector]
    public float maxTimeStun = 0.6f;
    float timeStun = 0;
    Vector3 knockback;

    public void StartRecieveHit(Vector3 _knockback, PlayerMovement attacker, float _maxTimeStun)
    {
        print("Recieve hit");
        moveSt = MoveState.Knockback;
        maxTimeStun = _maxTimeStun;
        timeStun = 0;
        noInput = true;
        knockback = _knockback;

        //Give FLAG
        if (haveFlag)
        {
            print("ROBA BANDERA");
            attacker.PickFlag(flag);
            flag = null;
            haveFlag = false;
        }

        print("STUNNED");
    }

    void ProcessStun()
    {
        timeStun += Time.deltaTime;
        if (timeStun >= maxTimeStun && noInput)
        {
            noInput = false;
            print("STUN END");
        }
    }

    [HideInInspector]
    public bool haveFlag = false;
    [HideInInspector]
    public GameObject flag = null;

    public void PickFlag(GameObject _flag)
    {
        if (!haveFlag)
        {
            flag = _flag;
            flag.transform.SetParent(rotateObj);
            flag.transform.localPosition = new Vector3(0, 0, -0.5f);
            flag.transform.localRotation = Quaternion.Euler(0,-90,0);
            haveFlag = true;
            flag.GetComponent<Flag>().currentOwner = gameObject;
        }
    }

    public void Die()
    {
        if (haveFlag)
        {
            GameController.instance.RespawnFlag(flag.GetComponent<Flag>());
            flag.GetComponent<Flag>().currentOwner = null;
            flag = null;
            haveFlag = false;
        }
        GameController.instance.RespawnPlayer(this);
    }

    [HideInInspector]
    public bool inWater = false;

    void EnterWater()
    {
        inWater = true;
        if (haveFlag)
        {
            GameController.instance.RespawnFlag(flag.GetComponent<Flag>());
            flag.GetComponent<Flag>().currentOwner = null;
            flag = null;
            haveFlag = false;
            print("CURRENT OWNER = NULL");
        }
    }

    void ProcessWater()
    {
        if (inWater)
        {
            controller.AroundCollisions();
            maxMoveSpeed2 = maxSpeedInWater;
        }
    }

    void ExitWater()
    {
        inWater = false;
    }

    void CheckWinGame(Respawn respawn)
    {
        if (haveFlag && team == respawn.team)
        {
            GameController.instance.GameOver(team);
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        switch (col.tag)
        {
            case "KillTrigger":
                Die();
                break;
            case "Flag":
                if(col.gameObject.GetComponent<Flag>().currentOwner==null)
                PickFlag(col.gameObject);
                break;
            case "Respawn":
                //print("I'm " + name + " and I touched a respawn");
                CheckWinGame(col.GetComponent<Respawn>());
                break;
            case "Water":
                EnterWater();
                break;
        }
    }
    private void OnTriggerExit(Collider col)
    {
        switch (col.tag)
        {
            case "Water":
                ExitWater();
                break;
        }
    }

    //Auxiliar functions
    public Vector3 RotateVector(float angle, Vector3 vector)
    {
        //rotate angle -90 degrees
        float theta = angle * Mathf.Deg2Rad;
        float cs = Mathf.Cos(theta);
        float sn = Mathf.Sin(theta);
        float px = vector.x * cs - vector.z * sn;
        float py = vector.x * sn + vector.z * cs;
        return  new Vector3(px, 0, py).normalized;
    }
    public Vector3 CalculateReflectPoint(float radius, Vector3 _wallNormal, Vector3 circleCenter)//needs wallJumpRadius and wallNormal
    {
        //LEFT OR RIGHT ORIENTATION?
        Vector3 wallDirLeft = Vector3.Cross(_wallNormal, Vector3.down).normalized;
        float ang = Vector3.Angle(wallDirLeft, currentFacingDir);
        float direction = ang > 90 ? -1 : 1;
        //Angle
        float angle = Vector3.Angle(currentFacingDir, _wallNormal);
        if (angle >= 90)
        {
            angle -= 90;
        }
        else
        {
            angle = 90 - angle;
        }
        angle = Mathf.Clamp(angle, wallJumpMinHorizAngle, 90);
        if (direction == -1)
        {
            float complementaryAng = 90 - angle;
            angle += complementaryAng * 2;
        }
        Debug.LogWarning("ANGLE = " + angle);
        float offsetAngleDir = Vector3.Angle(wallDirLeft, Vector3.forward) > 90 ? -1 : 1;
        float offsetAngle = Vector3.Angle(Vector3.right, wallDirLeft) * offsetAngleDir;
        angle += offsetAngle;
        //CALCULATE CIRCUMFERENCE POINT
        float px = circleCenter.x + (radius * Mathf.Cos(angle * Mathf.Deg2Rad));
        float pz = circleCenter.z + (radius * Mathf.Sin(angle * Mathf.Deg2Rad));
        Vector3 circumfPoint = new Vector3(px, circleCenter.y, pz);

        Debug.LogWarning("; circleCenter= " + circleCenter + "; circumfPoint = " + circumfPoint + "; angle = " + angle + "; offsetAngle = " + offsetAngle + "; offsetAngleDir = " + offsetAngleDir
    + ";wallDirLeft = " + wallDirLeft);
        Debug.DrawLine(circleCenter, circumfPoint, Color.white, 20);

        return circumfPoint;
    }
}

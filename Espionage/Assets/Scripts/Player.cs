using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviourPunCallbacks
{
    #region Variables
    public float speed;
    public float sprintModifier;
    public float crouchModifier; 
    public float jumpForce;
    public int maxHealth;
    public Camera normalCam;
    public GameObject cameraParent;
    public Transform weaponParent;
    public Transform groundDetector;
    public LayerMask ground;

    public float crouchAmount; 
    public GameObject standingCollider;
    public GameObject crouchCollider; 

    private Transform ui_Healthbar;
    private Text ui_Ammo;

    private Rigidbody rig;

    private Vector3 targetWeaponBobPosition;
    private Vector3 weaponParentOrigin;

    private float movementCounter;
    public float idleCounter;

    private float baseFOV;
    private float sprintFOVModifier = 1.5f;

    private int currentHealth;

    private Manager manager;
    private Weapon weapon;

    private bool crouched;

    #endregion

    #region Monobehavior Callbacks
    private void Start()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        weapon = GetComponent<Weapon>();
        currentHealth = maxHealth;

        cameraParent.SetActive(photonView.IsMine);
        if (!photonView.IsMine)
        {
            gameObject.layer = 11;
        }

        baseFOV = normalCam.fieldOfView;

        if(Camera.main) Camera.main.enabled = false;

        rig = GetComponent<Rigidbody>();
        weaponParentOrigin = weaponParent.localPosition;

        if (photonView.IsMine)
        {
            ui_Healthbar = GameObject.Find("HUD/Health/Bar").transform;
            ui_Ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
            RefreshHealthBar();
        }
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        //Axis
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool crouch = Input.GetKey(KeyCode.LeftControl);

        //States
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.15f, ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded;
        bool isCrouching = crouch && !isJumping && !isSprinting && isGrounded;

        //Crouching
        if (isCrouching)
        {
            photonView.RPC("SetCrouch", RpcTarget.All, !crouched);
        }


        //Jumping
        if (isJumping)
        {
            if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
            rig.AddForce(Vector3.up * jumpForce);
        }


        //Head Bob
        if(t_hmove == 0 && t_vmove == 0)
        {
            //Idle
            HeadBob(idleCounter, 0.025f, 0.025f);
            idleCounter += Time.deltaTime;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
        }
        else if (!isSprinting && !crouched)
        {
            //Walking
            HeadBob(movementCounter, 0.035f, 0.035f);
            movementCounter += Time.deltaTime * 3f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
        }
        else if (crouched)
        {
            //Crouching
            HeadBob(movementCounter, 0.02f, 0.02f);
            movementCounter += Time.deltaTime * 1.75f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
        }
        else
        {
            //Sprinting
            HeadBob(movementCounter, 0.15f, 0.075f);
            movementCounter += Time.deltaTime * 7f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
        }

        //UI Refreshes
        RefreshHealthBar();
        weapon.RefreshAmmo(ui_Ammo);
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        //Axis
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);

        //State
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded;

        //Movement
        Vector3 t_direction = new Vector3(t_hmove, 0, t_vmove);
        t_direction.Normalize();

        float t_adjustedSpeed = speed;
        if (isSprinting)
        {
            if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
            t_adjustedSpeed *= sprintModifier;
        }
        else if (crouched)
        {
            t_adjustedSpeed *= crouchModifier;
        }

        Vector3 t_targetVelocity = transform.TransformDirection(t_direction) * t_adjustedSpeed * Time.deltaTime;
        t_targetVelocity.y = rig.velocity.y;
        rig.velocity = t_targetVelocity;

        //Camera Stuff
        /*if (isSprinting)
        {
            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f);
        }
        else
        {
            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
        }
        */

        if (crouched) normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, weaponParentOrigin + Vector3.down * crouchAmount, Time.deltaTime * 8f);

    }
    #endregion

    #region Private Methods

    void HeadBob(float p_z, float p_x_intensity, float p_y_intensity)
    {
        float t_aim_adjust = 1f;
        if (weapon.isAiming) t_aim_adjust = 0.1f;
        targetWeaponBobPosition = weaponParentOrigin + new Vector3(Mathf.Cos(p_z) * p_x_intensity * t_aim_adjust, Mathf.Sin(p_z * 2) * p_y_intensity * t_aim_adjust, 0);
    }

    void RefreshHealthBar()
    {
        float t_health_ratio = (float)currentHealth / (float)maxHealth; // (float) casts an integer to a float
        ui_Healthbar.localScale = Vector3.Lerp(ui_Healthbar.localScale, new Vector3(t_health_ratio, 1, 1), Time.deltaTime * 8f);
    }

    [PunRPC]
    /* void SetCrouch(bool p_state)
    {
        if (crouched == p_state) return;
        crouched = p_state;

        if (crouched)
        {
            standingCollider.SetActive(false);
            crouchCollider.SetActive(true);
            weaponParent.position += Vector3.down * crouchAmount;
        }

        else
        {
            standingCollider.SetActive(true);
            crouchCollider.SetActive(false);
            weaponParent.position -= Vector3.down * crouchAmount;
        }
    }
    */

    #endregion

    #region Public Methods

    public void TakeDamage(int p_damage)
    {
        if (photonView.IsMine)
        {
            currentHealth -= p_damage;
            RefreshHealthBar();

            if(currentHealth <= 0)
            {
                manager.Spawn();
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Look : MonoBehaviourPunCallbacks
{
    #region Variables

    public Transform player;
    public Transform cams;
    public Transform weapon;

    public static bool cursorLocked = true;

    public float xSensitivity;
    public float ySensitivity;
    public float maxAngle;

    private Quaternion camCenter;

    #endregion

    #region Monobehavior Callbacks
    void Start()
    {
        camCenter = cams.localRotation; //Set rotation origin for camera to camCenter
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        UpdateCursorLock();
        SetY();
        SetX();
    }
    #endregion

    #region Private Methods
    void SetY()
    {
        float t_input = Input.GetAxis("Mouse Y") * ySensitivity * Time.fixedDeltaTime;
        Quaternion t_adj = Quaternion.AngleAxis(t_input, -Vector3.right);
        Quaternion t_delta = cams.localRotation * t_adj;

        if (Quaternion.Angle(camCenter, t_delta) < maxAngle)
        {
            cams.localRotation = t_delta;
        }
        weapon.rotation = cams.rotation;
    }

    void SetX()
    {
        float t_input = Input.GetAxis("Mouse X") * xSensitivity * Time.fixedDeltaTime;
        Quaternion t_adj = Quaternion.AngleAxis(t_input, Vector3.up);
        Quaternion t_delta = player.localRotation * t_adj;
        player.localRotation = t_delta;
    }

    void UpdateCursorLock()
    {
        if (cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cursorLocked = false;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cursorLocked = true;
            }
        }
    }
    #endregion
}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public enum CameraVFXType
{
    None,
    Dash,
    RecieveHit,
    Water,
    PickFlag
}
public class PlayerHUD : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Offset de la pantalla al que se bloquea la flecha de la bola")]
    float offsetPantalla;

    [Header("Referencias")]
    [HideInInspector]
    public GameControllerBase gC;
    [HideInInspector]
    public PlayerMovement myPlayerMov;
    [HideInInspector]
    public Camera myCamera;
    [HideInInspector]
    public Camera myUICamera;
    Canvas myCanvas;

    public RectTransform contador;
    public RectTransform powerUpPanel;
    public Image crosshair;
    public Image crosshairReduced;
    public Image Hook;
    public Image Boost;
    public TMPro.TextMeshProUGUI redTeamScoreText;
    public TMPro.TextMeshProUGUI blueTeamScoreText;
    public TMPro.TextMeshProUGUI timeText;
    public Text attackNameText;

    //pickup weapon
    public GameObject Interaction_Message;
    public Text changeYourWeaponToText;
    //public Text pressText;
    public Image interactButtonImage;

    //Flag Slider
    [Header("Flag Slider")]
    public RectTransform flagSlider;
    public RectTransform flagSliderStart;
    public RectTransform flagSliderEnd;
    float flagSliderStartX;
    float flagSliderEndX;
    Vector3 blueFlagHomePos;
    Vector3 redFlagHomePos;
    float sliderTotalDist;
    Transform flag;
    Flag flagForSlider;
    Vector3 flagPos;

    [Header("Flag Arrow")]
    [SerializeField]
    Transform Arrow;
    [SerializeField]
    Transform Wale;

    //Grapple
    [Header("AutoGrapple")]
    public Transform hookPointsParent;
    public GameObject hookPointHUDPrefab;
    public GameObject cameraCenterPrefab;
    public GameObject pressButtonToGrappleMessage;
    [HideInInspector]
    public Vector2 cameraCenterPix;
    List<HookPointHUDInfo> hookPointHUDInfoList;

    //CameraVFX
    [Header("Camera VFX")]
    public CameraVFX[] cameraVFX;
    CameraVFX currentCameraVFX;
    public RectTransform CameraVFXParent;


    //DASH HUD
    [Header("DASH HUD")]
    public RectTransform dashHUDParent;
    public RectTransform dashHUDStartPos;
    public RectTransform dashHUDEndPos;
    public RectTransform dashHUDWater;
    float dashHUDTotalDist = 0;
    public Image background, minLine;
    public Color[] backgroundColor, minLineColor;//0 -> COLOR WHEN BOOST READY; 1 -> COLOR WHEN BOOST NOT READY


    public void KonoAwake()
    {
        hookPointHUDInfoList = new List<HookPointHUDInfo>();
        myCanvas = GetComponent<Canvas>();
        CameraVFXAwakes();
    }

    public void KonoStart()
    {
        if (gC.gameMode == GameMode.CaptureTheFlag)
        {
            flagForSlider = (gC as GameController_FlagMode).flags[0];
        }
        if (gC.gameMode == GameMode.Tutorial)
        {
            Arrow.gameObject.SetActive(false);
            Wale.gameObject.SetActive(false);
            contador.gameObject.SetActive(false);


        }
        Interaction_Message.SetActive(false);
        crosshair.enabled = false;
        crosshairReduced.enabled = false;
        if (gC.gameMode == GameMode.CaptureTheFlag)// && !PhotonNetwork.IsConnected)
        {
            SetupFlagSlider();
        }
        pressButtonToGrappleMessage.SetActive(false);
        SetUpCameraCenter();
        SetupDashHUD();
    }

    private void Update()
    {
        if (gC.gameMode == GameMode.CaptureTheFlag)// && !PhotonNetwork.IsConnected)
        {
            UpdateFlagSlider();
        }
        UpdateAllHookPointHUDs();
        UpdateDashHUDFuel();
    }

    public void AdaptCanvasHeightScale()
    {
        float scale = myUICamera.rect.width - myUICamera.rect.height;
        if (scale != 0)
        {
            float scaleValue = scale;
            float invScaleValue = 1 + (1 - scale);
            contador.localScale = new Vector3(contador.localScale.x * scaleValue, contador.localScale.y, 1);
            powerUpPanel.localScale = new Vector3(powerUpPanel.localScale.x * scaleValue, powerUpPanel.localScale.y, 1);
            crosshair.rectTransform.localScale = new Vector3(crosshair.rectTransform.localScale.x * scaleValue, crosshair.rectTransform.localScale.y, 1);
            crosshairReduced.rectTransform.localScale = new Vector3(crosshairReduced.rectTransform.localScale.x * scaleValue, crosshairReduced.rectTransform.localScale.y, 1);
            redTeamScoreText.rectTransform.localScale = new Vector3(redTeamScoreText.rectTransform.localScale.x * invScaleValue, redTeamScoreText.rectTransform.localScale.y, 1);
            blueTeamScoreText.rectTransform.localScale = new Vector3(blueTeamScoreText.rectTransform.localScale.x * invScaleValue, blueTeamScoreText.rectTransform.localScale.y, 1);
            timeText.rectTransform.localScale = new Vector3(timeText.rectTransform.localScale.x, timeText.rectTransform.localScale.y * scaleValue, 1);
            dashHUDParent.localScale = new Vector3(dashHUDParent.localScale.x * scaleValue, dashHUDParent.localScale.y, 1);
            //CameraVFXParent.localScale = new Vector3(CameraVFXParent.localScale.x * scaleValue, CameraVFXParent.localScale.y, 1);
        }
    }

    public void SetUpCameraCenter()
    {
        cameraCenterPix = new Vector2(myCamera.pixelWidth / 2, myCamera.pixelHeight / 2);
        cameraCenterPix += new Vector2(myCamera.pixelWidth / myCamera.rect.width * myCamera.rect.x, myCamera.pixelHeight / myCamera.rect.height * myCamera.rect.y);
        //Debug.Log("I'm player " + myPlayerMov.name +" and my center in the camera is  = " + cameraCenterPix.ToString("F4"));
        cameraCenterPrefab.SetActive(false);
        //cameraCenterPrefab.GetComponent<RectTransform>().anchoredPosition = cameraCenterPix;
        //Debug.Log("cameraCenterPrefab.rectTransform.localPosition = " + cameraCenterPrefab.GetComponent<RectTransform>().localPosition);
        //cameraCenterPrefab.GetComponent<RectTransform>().position = cameraCenterPix;
    }

    public void SetPickupWeaponTextMessage(WeaponData _weap)
    {
        //if (!changeYourWeaponToText.enabled) changeYourWeaponToText.enabled = true;
        //if (!pressText.enabled) pressText.enabled = true;
        //if (!interactButtonImage.enabled) interactButtonImage.enabled = true;

        if (!Interaction_Message.activeInHierarchy) Interaction_Message.SetActive(true);
        //print("Setting Pickup Weapon Text Message");
        changeYourWeaponToText.text = "to change your weapon to " + _weap.weaponName;
    }

    public void DisablePickupWeaponTextMessage()
    {
        //changeYourWeaponToText.enabled = false;
        //pressText.enabled = false;
        //interactButtonImage.enabled = false;
        if (Interaction_Message.activeInHierarchy) Interaction_Message.SetActive(false);
    }

    void SetupFlagSlider()
    {
        flag = (gC as GameController_FlagMode).flags[0].transform;
        blueFlagHomePos = (gC as GameController_FlagMode).FlagHome_TeamA.position;
        redFlagHomePos = (gC as GameController_FlagMode).FlagHome_TeamB.position;
        blueFlagHomePos.y = 0;
        redFlagHomePos.y = 0;
        flagSliderStartX = flagSliderStart.localPosition.x;
        flagSliderEndX = flagSliderEnd.localPosition.x;
        sliderTotalDist = (flagSliderStart.localPosition - flagSliderEnd.localPosition).magnitude;
        flagSlider.localPosition = (flagSliderStart.localPosition - flagSliderEnd.localPosition) / 2;

        //Flecha bola
        Arrow.gameObject.SetActive(false);
        Wale.gameObject.SetActive(false);
    }

    Vector3 ArrowPointing;
    void UpdateFlagSlider()
    {
        float progress = 0.5f;
        if (!flagForSlider.respawning)
        {
            flagPos = flag.position;
            flagPos.y = 0;
            Vector3 blueToFlagDir = blueFlagHomePos - flagPos;
            float distFromBlue = blueToFlagDir.magnitude;
            Vector3 union = (blueFlagHomePos - redFlagHomePos).normalized;
            float angle = Vector3.Angle(blueToFlagDir.normalized, union);
            float currentDist = Mathf.Cos(angle * Mathf.Deg2Rad) * distFromBlue;
            float totalDist = (blueFlagHomePos - redFlagHomePos).magnitude;
            progress = Mathf.Clamp01(currentDist / totalDist);
        }

        float currentX = progress * sliderTotalDist;
        Vector3 newPos = flagSliderStart.localPosition;
        newPos.x += currentX;
        flagSlider.localPosition = newPos;

        // Flecha a Bola
        ArrowToFlag();
    }

    void ArrowToFlag()
    {

        Vector3 dir = myCamera.WorldToScreenPoint(flag.position);

        if (dir.x > offsetPantalla && dir.x < Screen.width - offsetPantalla && dir.y > offsetPantalla && dir.y < Screen.height - offsetPantalla)
        {
            Arrow.gameObject.SetActive(false);
            Wale.gameObject.SetActive(false);
        }
        else
        {
            Arrow.gameObject.SetActive(true);
            Wale.gameObject.SetActive(true);

            ArrowPointing.z = Mathf.Atan2((Arrow.transform.position.y - dir.y), (Arrow.transform.position.x - dir.x)) * Mathf.Rad2Deg - 90;

            Arrow.transform.rotation = Quaternion.Euler(ArrowPointing);
            Arrow.transform.position = new Vector3(Mathf.Clamp(dir.x, offsetPantalla, Screen.width - offsetPantalla), Mathf.Clamp(dir.y, offsetPantalla, Screen.height - offsetPantalla), 0);

            Wale.transform.position = Arrow.transform.position;
        }
    }

    // --- Hook ---

    public void StartAim()
    {
        crosshair.enabled = true;
        crosshairReduced.enabled = false;
    }

    public void StartThrowHook()
    {
        crosshair.enabled = false;
        crosshairReduced.enabled = true;
    }

    public void StopThrowHook()
    {
        crosshair.enabled = true;
        crosshairReduced.enabled = false;
    }

    public void StopAim()
    {
        crosshair.enabled = false;
        crosshairReduced.enabled = false;
    }

    // --- Hook CD and Boost CD ---

    public void setHookUI(float f)
    {
        //print("SetHookUI: fillAmout= " + f);
        Hook.fillAmount = Mathf.Clamp01(f);
    }

    public void setBoostUI(float f)
    {
        //print("SetBoostUI: fillAmout= " + f);
        Boost.fillAmount = Mathf.Clamp01(f);
    }


    // --- GRAPPLE//Hook Point ---
    public void ShowGrappleMessage()
    {
        //Debug.LogWarning("SHOW GRAPPLE MESSAGE");
        pressButtonToGrappleMessage.SetActive(true);
    }

    public void HideGrappleMessage()
    {
        pressButtonToGrappleMessage.SetActive(false);
    }

    //public void ShowGrapplePoints(List<HookPoint> hookPoints)
    //{
    //    for (int i = 0; i < hookPoints.Count; i++)
    //    {
    //        bool found = false;
    //        for (int j = 0; j < hookPointHUDInfoList.Count && !found; j++)
    //        {
    //            if (hookPointHUDInfoList[j].hookPointHUD.myHookPoint == hookPoints[i])
    //            {
    //                found = true;
    //                if ()
    //                {

    //                }
    //            }
    //        }
    //        if (!found)
    //        {
    //            AddHookPointHUD(hookPoints[i]);
    //        }
    //    }
    //}

    void UpdateAllHookPointHUDs()
    {
        for (int i = 0; i < hookPointHUDInfoList.Count; i++)
        {
            if (hookPointHUDInfoList[i].showing)
            {
                hookPointHUDInfoList[i].hookPointHUD.KonoUpdate();
            }
        }
    }

    public void HideHookPointHUD(HookPoint hookPoint)
    {
        for (int i = 0; i < hookPointHUDInfoList.Count; i++)
        {
            if (hookPointHUDInfoList[i].hookPointHUD.myHookPoint == hookPoint)
            {
                if (hookPointHUDInfoList[i].showing)
                {
                    hookPointHUDInfoList[i].showing = false;
                    if (hookPointHUDInfoList[i].chosenOne)
                    {
                        hookPointHUDInfoList[i].SwitchChosenOne();
                    }
                    hookPointHUDInfoList[i].hookPointHUD.gameObject.SetActive(false);
                }
            }
        }
    }

    public void ShowHookPointHUD(HookPoint hookPoint)
    {
        bool found = false;
        for (int i = 0; i < hookPointHUDInfoList.Count && !found; i++)
        {
            if (hookPointHUDInfoList[i].hookPointHUD.myHookPoint == hookPoint)
            {
                if (!hookPointHUDInfoList[i].showing)
                {
                    hookPointHUDInfoList[i].showing = true;
                    hookPointHUDInfoList[i].hookPointHUD.gameObject.SetActive(true);
                    found = true;
                }
            }
        }
        if (!found)//Create new hookpointHUD
        {
            AddHookPointHUD(hookPoint);
        }

    }

    void AddHookPointHUD(HookPoint hookPoint)
    {
        print("CREATE NEW hookPointHUDObject");
        GameObject hookPointHUDObject = Instantiate(hookPointHUDPrefab, transform, false);
        hookPointHUDObject.transform.SetParent(hookPointsParent, true);
        HookPointHUDInfo auxHookPointHUDInfo = new HookPointHUDInfo(hookPointHUDObject.GetComponent<HookPointHUD>());
        auxHookPointHUDInfo.hookPointHUD.KonoAwake(hookPoint, myUICamera, myCanvas);
        auxHookPointHUDInfo.showing = true;
        hookPointHUDInfoList.Add(auxHookPointHUDInfo);
    }

    public void SetChosenHookPointHUD(HookPoint hookPoint)
    {
        print("SetChosenHookPointHUD(" + hookPoint.name + ")");
        for (int i = 0; i < hookPointHUDInfoList.Count; i++)
        {
            if (hookPointHUDInfoList[i].hookPointHUD.myHookPoint == hookPoint && !hookPointHUDInfoList[i].chosenOne)
            {
                hookPointHUDInfoList[i].SwitchChosenOne();
            }
            else
            {
                if (hookPointHUDInfoList[i].chosenOne)
                {
                    hookPointHUDInfoList[i].SwitchChosenOne();
                }
            }
        }
    }


    // --- EFECTOS DE CÁMARA ---

    void CameraVFXAwakes()
    {
        for(int i=0; i<cameraVFX.Length; i++)
        {
            cameraVFX[i].KonoAwake();
        }
    }

    public void StartCamVFX(CameraVFXType camVFXType)
    {
        for(int i = 0; i < cameraVFX.Length; i++)
        {
            if(cameraVFX[i].effectType == camVFXType)
            {
                if (currentCameraVFX != null)
                {
                    if (cameraVFX[i].priority >= currentCameraVFX.priority)
                    {
                        StopCamVFX(currentCameraVFX);
                        StartCamVFX(cameraVFX[i]);
                    }
                }
                else
                {
                    StartCamVFX(cameraVFX[i]);
                }
            }
        }
    }

    void StartCamVFX(CameraVFX camVFX)
    {
        camVFX.Activate();
        currentCameraVFX = camVFX;
    }

    public void StopCamVFX(CameraVFXType camVFXType)
    {
        for (int i = 0; i < cameraVFX.Length; i++)
        {
            if (cameraVFX[i].effectType == camVFXType)
            {
                cameraVFX[i].Deactivate();
            }
        }
        currentCameraVFX = null;
    }

    public void StopCamVFX(CameraVFX camVFX)
    {
        for (int i = 0; i < cameraVFX.Length; i++)
        {
            if(cameraVFX[i]== camVFX)
            {
                cameraVFX[i].Deactivate();
            }
        }
        currentCameraVFX = null;
    }

    // --- DASH HUD --- 

    void SetupDashHUD()
    {
        dashHUDTotalDist = Mathf.Abs(dashHUDStartPos.localPosition.y - dashHUDEndPos.localPosition.y);
        SetDashHUDProgress(1);
        background.color = backgroundColor[0];
        minLine.color = minLineColor[0];
    }

    void UpdateDashHUDFuel()
    {
        if (myPlayerMov.boostReady)
        {
            background.color = backgroundColor[0];
            minLine.color = backgroundColor[0];
        }
        else
        {
            background.color = backgroundColor[1];
            if (myPlayerMov.boostCurrentFuel < myPlayerMov.boostMinFuelNeeded * myPlayerMov.boostCapacity)
            {
                minLine.color = backgroundColor[1];
            }
            else
            {
                minLine.color = backgroundColor[0];
            }
        }
        float progress = myPlayerMov.boostCurrentFuel / myPlayerMov.boostCapacity;
        SetDashHUDProgress(progress);
    }

    void SetDashHUDProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        float currentY = progress * dashHUDTotalDist;
        dashHUDWater.localPosition = new Vector3(dashHUDStartPos.localPosition.x, dashHUDStartPos.localPosition.y + currentY, 1);
    }
}
[System.Serializable]
public class CameraVFX
{
    public CameraVFXType effectType;
    public GameObject prefab;
    [Tooltip("The higher, the bigger prority")]
    public int priority;
    public float speed = 1;

    public void KonoAwake()
    {
        prefab.GetComponent<Animator>().speed = speed;
        prefab.SetActive(false);
    }

    public void Activate()
    {
        if (!prefab.activeInHierarchy)
        {
            Debug.Log("ACTIVATE CAMERA VFX : " + effectType);
            prefab.SetActive(true);
            //prefab.GetComponent<Animator>().Play("dashCameraVFX",0);
        }
    }

    public void Deactivate()
    {
        if (prefab.activeInHierarchy)
        {
            prefab.SetActive(false);
        }
    }

}

public class HookPointHUDInfo
{
    public HookPointHUD hookPointHUD;
    public bool showing = false;
    public bool chosenOne = false;
    Color green = new Color(0.46f, 1, 0, 0.75f);
    Color gray = new Color(0.5f, 0.5f, 0.5f, 0.75f);

    public HookPointHUDInfo()
    {
        hookPointHUD = null;
        showing = false;
        chosenOne = false;
        hookPointHUD.GetComponent<Image>().color = gray;
    }

    public HookPointHUDInfo(HookPointHUD _hookPointHUD)
    {
        hookPointHUD = _hookPointHUD;
        showing = true;
        chosenOne = false;
        hookPointHUD.GetComponent<Image>().color = gray;
    }

    public void SwitchChosenOne()
    {
        if (chosenOne)
        {
            chosenOne = false;
            hookPointHUD.GetComponent<Image>().color = gray;
        }
        else
        {
            chosenOne = true;
            hookPointHUD.GetComponent<Image>().color = green;
        }
    }
}

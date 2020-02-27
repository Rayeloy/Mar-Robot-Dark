﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControllerCMF_Housing : GameControllerCMF
{
    [Header("--- HOUSING ---")]
    public bool showSlotMeshes = false;
    public Transform housingParent;
    public Transform housingFurnituresParent;
    public GameObject housingGridPrefab;
    public GameObject housingSlotPrefab;
    public GameObject wallPrefab;
    public HousingHouseData houseMeta;
    public Vector3 houseSpawnPos = Vector3.zero;
    public bool showWallMeshes = false;
    public Material[] wallMaterials; // length = 2
    public HousingFurnitureData testFurniture;

    GameObject currentGridObject;
    HousingGrid currentGrid;

    //EDIT MODE
    bool editHouseOn = false;
    JoyStickControls myLeftJoyStickControls;
    [Header(" - Edit Mode - ")]
    [Range(0, 1)]
    public float leftJoyStickDeadzone = 0.2f;
    public HousingEditModeCameraController editModeCameraController;
    float cameraHeightOffset = 0;
    public float cameraDistValue = 0;
    public Material[] highlightedSlotMat;



    protected override void SpecificAwake()
    {
        myLeftJoyStickControls = new JoyStickControls(allPlayers[0].actions.LeftJoystick, leftJoyStickDeadzone);
        currentGridObject = Instantiate(housingGridPrefab, houseSpawnPos, Quaternion.identity, housingParent);
        currentGrid = currentGridObject.GetComponent<HousingGrid>();
        currentGrid.KonoAwake(houseMeta, housingFurnituresParent, housingSlotPrefab, wallPrefab, houseSpawnPos, highlightedSlotMat, editModeCameraController);
        //Spawn House
        SpawnHouse(houseMeta);
        editModeCameraController.KonoAwake(currentGrid, houseSpawnPos);
    }

    protected override void SpecificUpdate()
    {
        //Camera Update
        if(editHouseOn) editModeCameraController.KonoUpdate();

        if (allPlayers[0].actions.Select.WasPressed)
        {
            SwitchPlayAndHousingMode();
        }
        if (editHouseOn)
        {
            if (myLeftJoyStickControls.LeftWasPressed)
            {
                currentGrid.MoveSelectSlot(Direction.Left);
            }
            else if (myLeftJoyStickControls.RightWasPressed)
            {
                currentGrid.MoveSelectSlot(Direction.Right);
            }
            else if (myLeftJoyStickControls.UpWasPressed)
            {
                currentGrid.MoveSelectSlot(Direction.Up);
            }
            else if (myLeftJoyStickControls.DownWasPressed)
            {
                currentGrid.MoveSelectSlot(Direction.Down);
            }
        }
        myLeftJoyStickControls.ResetJoyStick();

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            HousingFurniture aux;
            currentGrid.SpawnFurniture(testFurniture, out aux);
        }
    }

    protected override void SpecificLateUpdate()
    {
        if (editHouseOn)
        {
            //move camera
            editModeCameraController.KonoLateUpdate();
        }
    }

    void SpawnHouse(HousingHouseData houseMeta)
    {
        if (currentGrid != null)
        {
            currentGrid.CreateGrid(showSlotMeshes);
            currentGrid.CreateWalls(showWallMeshes, wallMaterials);
            Vector3 playerSpawnPos; Quaternion playerSpawnRot;
            if (currentGrid.CreateDoor(out playerSpawnPos, out playerSpawnRot))//Change spawn to the result pos of this, and tp players to here.)
            {
                for (int i = 0; i < allPlayers.Count; i++)
                {
                    allPlayers[i].mySpawnInfo.position = playerSpawnPos;
                    allPlayers[i].mySpawnInfo.rotation = playerSpawnRot;
                }
                Debug.Log(" playerSpawnPos = " + playerSpawnPos + "; playerSpawnRot = " + playerSpawnRot);
            }
            else
            {
                Debug.LogError("Can't create door");
            }
        }
    }

    void SwitchPlayAndHousingMode()
    {
        if (!editHouseOn) //ACTIVATE EDIT MODE
        {
            //FOR ONLINE:
            /*if(people in room>1)//can only edit room if alone in it.
             * {
             * set room to private
             * return; 
            }*/

            //set player away (invisible)
            DeactivateHostPlayer();

            //Set new Edit Camera
            cameraHeightOffset = ((currentGrid.myHouseMeta.height / 2) + 1) * currentGrid.myHouseMeta.housingSlotSize;
            Vector3 cameraBaseCenterPos = currentGrid.worldCenter + Vector3.up * cameraHeightOffset;
            Vector3 houseFloorCenter = new Vector3(cameraBaseCenterPos.x, houseSpawnPos.y, cameraBaseCenterPos.z + (currentGrid.myHouseMeta.depth / 3 * currentGrid.myHouseMeta.housingSlotSize));
            float volume = currentGrid.myHouseMeta.width * currentGrid.myHouseMeta.depth * currentGrid.myHouseMeta.height * currentGrid.myHouseMeta.housingSlotSize;
            float cameraMaxZoomDist = volume * cameraDistValue;
            Debug.Log(" cameraBaseCenterPos = " + cameraBaseCenterPos.ToString("F4") + "; houseFloorCenter = " + houseFloorCenter.ToString("F4") + "; cameraMaxZoomDist = " + cameraMaxZoomDist.ToString("F4"));
            editModeCameraController.Activate(cameraBaseCenterPos, houseFloorCenter, -cameraMaxZoomDist);

            //Highlight center Slot
            HousingGridCoordinates coord = new HousingGridCoordinates(0, currentGrid.myHouseMeta.depth / 2, currentGrid.myHouseMeta.width / 2);
            currentGrid.SelectSlotAt(coord);
        }
        else // DEACTIVE EDIT MODE
        {
            //Save room disposition to Database
            //Spawn player at door
            ActivateHostPlayer();

            //Deactivate Edit Camera
            editModeCameraController.DeActivate();

            //stop hightlight current slot
            currentGrid.StopHighlightSlot(currentGrid.currentSlotCoord);
        }
        editHouseOn = !editHouseOn;
        playing = !playing;
        //Call Interface
    }

    void DeactivateHostPlayer()
    {
        allPlayers[0].transform.position = new Vector3(-200, -200, -200);
        allPlayers[0].myCamera.myCamera.GetComponent<Camera>().enabled = false;
        AudioListener audioListener = allPlayers[0].myCamera.myCamera.GetComponent<AudioListener>();
        if (audioListener != null) audioListener.enabled = false;
    }

    void ActivateHostPlayer()
    {
        RespawnPlayer(allPlayers[0]);
        allPlayers[0].myCamera.myCamera.GetComponent<Camera>().enabled = true;
        AudioListener audioListener = allPlayers[0].myCamera.myCamera.GetComponent<AudioListener>();
        if (audioListener != null) audioListener.enabled = true;
    }

}
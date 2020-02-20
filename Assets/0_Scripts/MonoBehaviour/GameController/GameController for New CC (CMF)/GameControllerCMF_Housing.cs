﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControllerCMF_Housing : GameControllerCMF
{
    [Header("--- HOUSING ---")]
    public Transform housingParent;
    public GameObject housingGridPrefab;
    public GameObject housingSlotPrefab;
    public HousingHouseData houseMeta;
    public Vector3 houseSpawnPos = Vector3.zero;
    GameObject currentGridObject;
    HousingGrid currentGrid;
    HousingGridCoordinates currentGridCoord;

    protected override void SpecificAwake()
    {
        currentGridCoord = new HousingGridCoordinates();

        currentGridObject = Instantiate(housingGridPrefab, houseSpawnPos, Quaternion.identity, housingParent);
        currentGrid = currentGridObject.GetComponent<HousingGrid>();
        currentGrid.KonoAwake(houseMeta, housingSlotPrefab, houseSpawnPos);
        //Spawn House
        SpawnHouse(houseMeta);
    }

    void SpawnHouse(HousingHouseData houseMeta)
    {
        if(currentGrid != null)
        {
            currentGrid.CreateGrid();
        }
    }
}

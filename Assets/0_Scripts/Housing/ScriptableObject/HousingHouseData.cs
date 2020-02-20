﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum Direction
{
    Left,
    Right,
    Up,
    Down
}

[ExecuteInEditMode]
[CreateAssetMenu(fileName = "New housing house", menuName = "Housing/House")]
public class HousingHouseData : ScriptableObject
{
    public string houseName;
    public float housingSlotSize = 1;

    public HouseDoor door;
    public HouseWindow[] windows;

    [Header(" --- House Space ---")]
    public int width = 10;
    public int depth = 10;
    public int height = 10;

    public bool reset = false;
    public HouseSpace houseSpace;
    public bool validHouseSpacing
    {
        get
        {
            return (houseSpace != null && houseSpace.houseLevels.Length > 0 && houseSpace.houseLevels[0] != null && houseSpace.houseLevels[0].houseLevelRows.Length > 0
                && houseSpace.houseLevels[0].houseLevelRows[0] != null && houseSpace.houseLevels[0].houseLevelRows[0].row.Length > 0);
        }
    }
    HouseSpace oldHouseSpace;
    private void OnValidate()
    {
        if (reset)
        {
            reset = false;
            houseSpace = new HouseSpace(height, depth, width);
            oldHouseSpace = new HouseSpace(height, depth, width);
            CopyHouseSpace(houseSpace, ref oldHouseSpace);
        }
        //CHECK IF houseSpacing CHANGED
        bool houseSpacingChanging = (oldHouseSpace != null && oldHouseSpace.houseLevels.Length != height);

        for (int k = 0; k < houseSpace.houseLevels.Length && !houseSpacingChanging; k++)
        {
            houseSpacingChanging = (houseSpace.houseLevels[k] != null && houseSpace.houseLevels[k].houseLevelRows.Length != depth);
            for (int i = 0; i < houseSpace.houseLevels[k].houseLevelRows.Length && !houseSpacingChanging; i++)
            {
                houseSpacingChanging = (houseSpace.houseLevels[k].houseLevelRows[i] != null && houseSpace.houseLevels[k].houseLevelRows[i].row.Length != width);
            }
        }

        //CREATE NEW SPACE INFO, AND COPY , IF POSSIBLE, PREVIOUS DATA
        if (houseSpacingChanging)
        {
            height = Mathf.Clamp(height, 0, 15);
            depth = Mathf.Clamp(depth, 0, 100);
            width = Mathf.Clamp(width, 0, 100);

            houseSpace = new HouseSpace(height, depth, width);
            CopyHouseSpace(oldHouseSpace, ref houseSpace);
        }

        #region --- Door and Windows Check ---
        //Door and Windows
        bool wrongDoor = false;
        if(door != null && door.validDoor && door.x < width && door.z < depth && door.height <= height)
        {
            for (int i = 0; i < door.height && !wrongDoor; i++)
            {
                for (int j = 0; j < door.width && !wrongDoor; j++)
                {
                    int doorXExtension = door.orientation == Direction.Up ? +j : door.orientation == Direction.Down ? -j : 0;
                    int doorZExtension = door.orientation == Direction.Right ? +j : door.orientation == Direction.Left ? -j : 0;
                    if (!houseSpace.houseLevels[i].houseLevelRows[door.x + doorXExtension].row[door.z + doorZExtension])
                    {
                        Debug.LogError("HousingHouseData -> Error: The door can't be placed in (" + door.x + "," + door.z + ") in level"+i+" !");
                        door.x = -1;
                        door.z = -1;
                        wrongDoor = true;
                    }
                }
            }
        }
        else
        {
            Debug.LogError("HousingHouseData -> Error: door is not valid");
        }

        bool wrongWindow = false;
        for (int w = 0; w < windows.Length && !wrongWindow; w++)
        {
            if (windows[w].validWindow && windows[w].x < width && windows[w].z < depth && windows[w].y < height && windows[w].height <= height && windows[w].width <= width)
            {
                for (int i = windows[w].y; i < (windows[w].y + windows[w].height) && !wrongWindow; i++)
                {
                    if (i >= houseSpace.houseLevels.Length)
                    {
                        Debug.LogError("HousingHouseData -> Error: The window " + w + " can't be placed in (" + windows[w].x + "," + windows[w].z + ") in level " + (windows[w].y + i) + " !");
                        continue;
                    }
                    for (int j = 0; j < windows[w].width && !wrongWindow; j++)
                    {
                        int windowXExtension = windows[w].orientation == Direction.Up ? +j :windows[w].orientation == Direction.Down ? -j : 0;
                        int windowZExtension = windows[w].orientation == Direction.Right ? +j : windows[w].orientation== Direction.Left ? -j : 0;
                        if(windows[w].x + windowXExtension >= houseSpace.houseLevels[i].houseLevelRows.Length)
                        {
                            Debug.LogError("HousingHouseData -> Error: The window " + w + " can't be placed in (" + windows[w].x + "," + windows[w].z + ") in level " + (windows[w].y + i) + " !");
                            continue;
                        }

                        if(windows[w].z + windowZExtension >= houseSpace.houseLevels[i].houseLevelRows[windows[w].x + windowXExtension].row.Length)
                        {
                            Debug.LogError("HousingHouseData -> Error: The window " + w + " can't be placed in (" + windows[w].x + "," + windows[w].z + ") in level " + (windows[w].y + i) + " !");
                            continue;
                        }
                        if (!houseSpace.houseLevels[i].houseLevelRows[windows[w].x + windowXExtension].row[windows[w].z + windowZExtension])
                        {
                            Debug.LogError("HousingHouseData -> Error: The window "+w+" can't be placed in (" + windows[w].x + "," + windows[w].z + ") in level "+(windows[w].y + i) +" !");
                            windows[w].x = -1;
                            windows[w].z = -1;
                            wrongWindow = true;
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("HousingHouseData -> Error: window "+w+" is not valid");
            }
        }
        #endregion

        //STORE COPY
        oldHouseSpace = new HouseSpace(height, depth, width);
        CopyHouseSpace(houseSpace, ref oldHouseSpace);
    }

    void CopyHouseSpace(HouseSpace origHouseSpace, ref HouseSpace targetHouseSpace)
    {
        if(origHouseSpace==null || targetHouseSpace == null)
        {
            Debug.LogError("HousingHouseData -> CopyHouseSpace: Error: origHouseSpace or targetHouseSpace are null");
            return;
        }

        for (int i = 0; i < origHouseSpace.houseLevels.Length && i < targetHouseSpace.houseLevels.Length; i++)
        {
            CopyHouseLevel(origHouseSpace.houseLevels[i], ref targetHouseSpace.houseLevels[i]);
        }
    }

    void CopyHouseLevel(HouseLevel origHouseLevel, ref HouseLevel targetHouseLevel)
    {
        if(origHouseLevel == null || targetHouseLevel == null)
        {
            Debug.LogError("HousingHouseData -> CopyHouseLevel: Error: origHouseLevel or targetHouseLevel are null");
            return;
        }

        for (int i = 0; i < origHouseLevel.houseLevelRows.Length && i < targetHouseLevel.houseLevelRows.Length; i++)
        {
            Row originRow = origHouseLevel.houseLevelRows[i];
            Row targetRow = targetHouseLevel.houseLevelRows[i];
            for (int j = 0; j < originRow.row.Length && j < targetRow.row.Length; j++)
            {
                //Debug.Log("Copying parameter from old ["+i+","+j+"]("+ originRow.row[j] + ") to new ["+i+","+j+ "](" + targetRow.row[j] + ")");
                targetRow.row[j] = originRow.row[j];
            }
        }
    }
}

[System.Serializable]
public class HouseSpace
{
    public HouseLevel[] houseLevels;
    public int height;
    public int width;
    public int depth;
    public HouseSpace(int _height, int _depth, int _width)
    {
        height = _height;
        depth = _depth;
        width = _width;
        houseLevels = new HouseLevel[height];
        for (int i = 0; i < houseLevels.Length; i++)
        {
            houseLevels[i] = new HouseLevel(depth, width);
        }
    }
}

[System.Serializable]
public class HouseLevel
{
    public int width;
    public int depth;
    public Row[] houseLevelRows;
    public HouseLevel(int _depth, int _width)
    {
        depth = _depth;
        width = _width;
        houseLevelRows = new Row[depth];
        for (int i = 0; i < houseLevelRows.Length; i++)
        {
            houseLevelRows[i] = new Row(width, true);
        }
    }
}

[System.Serializable]
public class HouseDoor
{
    public GameObject prefab;
    public int x = 0;
    public int z = 0;
    public int height = 2;
    public int width = 1;
    public Direction orientation = Direction.Up;

    public HouseDoor(GameObject _prefab, int _x = 0, int _z = 0, int _height = 2, int _width = 1,  Direction _orientation = Direction.Up)
    {
        prefab = _prefab;
        x = _x;
        z = _z;
        height = _height;
        width = _width;
        orientation = _orientation;
    }

    public bool validDoor
    {
        get
        {
            return x>=0 && z>=0 && height>=2 && width>=1;
        }
    }
}

[System.Serializable]
public class HouseWindow
{
    public GameObject prefab;
    public int x = 0;
    public int z = 0;
    public int y = 1;
    public int height = 1;
    public int width = 1;
    public Direction orientation = Direction.Up;

    public HouseWindow(GameObject _prefab, int _x = 0, int _z = 0, int _y = 1, int _height = 2, int _width = 1, Direction _orientation = Direction.Up)
    {
        prefab = _prefab;
        x = _x;
        z = _z;
        y = _y;
        height = _height;
        width = _width;
        orientation = _orientation;
    }

    public bool validWindow
    {
        get
        {
            return x>=0 && z >=0 && y >= 0 && height>=1 && width >=1;
        }
    }
}

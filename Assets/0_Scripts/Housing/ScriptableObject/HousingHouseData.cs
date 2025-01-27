﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum Direction
{//DONT CHANGE NUMBERS/ORDER
    Up = 0,
    Right = 1,
    Down = 2,
    Left = 3
}

[ExecuteInEditMode]
[CreateAssetMenu(fileName = "New housing house", menuName = "Housing/House")]
public class HousingHouseData : ScriptableObject
{
    public string houseName;

    public HouseDoor door;
    public HouseWindow[] windows;

    [Header(" --- House Space ---")]
    [Range(0,100)]
    public int width = 10;
    [Range(0, 100)]
    public int depth = 10;
    [Range(0, 15)]
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

    private void Awake()
    {
        if(houseSpace == null)
        {
            Debug.Log("Creating New House Meta");
            houseSpace = new HouseSpace(height, depth, width);
            oldHouseSpace = new HouseSpace(height, depth, width);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }
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
        //if(door == null)
        //{
        //    door = new HouseDoor();
        //}
        bool wrongDoor = false;
        if(door != null && door.validDoor && door.x < width && door.z < depth && door.doorMeta.height <= height)
        {
            for (int i = 0; i < door.doorMeta.height && !wrongDoor; i++)
            {
                for (int j = 0; j < door.doorMeta.width && !wrongDoor; j++)
                {
                    int doorXExtension = door.doorMeta.orientation == Direction.Up ? +j : door.doorMeta.orientation == Direction.Down ? -j : 0;
                    int doorZExtension = door.doorMeta.orientation == Direction.Right ? +j : door.doorMeta.orientation == Direction.Left ? -j : 0;
                    if (!houseSpace.houseLevels[i].houseLevelRows[door.x + doorXExtension].row[door.z + doorZExtension])
                    {
                        Debug.LogError("HousingHouseData -> Error: The door can't be placed in (" + door.x + "," + door.z + ") in level"+i+" !");
                        door.x = -1;
                        door.z = -1;
                        wrongDoor = true;
                    }
                    else
                    {
                        Debug.Log("HousingHouseData -> Door is ok!");
                    }
                }
            }
        }
        else
        {
            string error = "HousingHouseData -> Error: door is not valid for reason: ";
            if (door != null) error += "door = null";
            else if (!door.validDoor) error += "door.validDoor = false";
            else if (door.x >= width) error += "door.x is out of limits";
            else if (door.z >= depth) error += "door.z is out of limits";
            else if (door.doorMeta.height > height) error += "door is too high";
            else error += "This is strange and should not be happening";
            Debug.LogError(error);
        }

        bool wrongWindow = false;
        for (int w = 0; w < windows.Length && !wrongWindow; w++)
        {
            if (windows[w].validWindow && windows[w].x < width && windows[w].z < depth && windows[w].y < height && windows[w].windowMeta.height <= height 
                && windows[w].windowMeta.width <= width)
            {
                for (int i = windows[w].y; i < (windows[w].y + windows[w].windowMeta.height) && !wrongWindow; i++)
                {
                    if (i >= houseSpace.houseLevels.Length)
                    {
                        Debug.LogError("HousingHouseData -> Error: The window " + w + " can't be placed in (" + windows[w].x + "," + windows[w].z + ") in level " + (windows[w].y + i) + " !");
                        continue;
                    }
                    for (int j = 0; j < windows[w].windowMeta.width && !wrongWindow; j++)
                    {
                        int windowXExtension = windows[w].windowMeta.orientation == Direction.Up ? +j :windows[w].windowMeta.orientation == Direction.Down ? -j : 0;
                        int windowZExtension = windows[w].windowMeta.orientation == Direction.Right ? +j : windows[w].windowMeta.orientation == Direction.Left ? -j : 0;
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

    public bool GetAtIndex(int k, int i, int j)
    {
        return houseSpace.houseLevels[k].houseLevelRows[i].row[j];
    }
    public Row GetAtIndex(int k, int i)
    {
        return houseSpace.houseLevels[k].houseLevelRows[i];
    }
    public HouseLevel GetAtIndex(int k)
    {
        return houseSpace.houseLevels[k];
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

    public bool valid
    {
        get
        {
            return height > 0 && width > 0 && depth > 0;
        }
    }
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

    public bool GetAtIndex(int k, int i, int j)
    {
        return houseLevels[k].houseLevelRows[i].row[j];
    }
    public Row GetAtIndex(int k, int i)
    {
        return houseLevels[k].houseLevelRows[i];
    }
    public HouseLevel GetAtIndex(int k)
    {
        return houseLevels[k];
    }

    public int minX
    {
        get
        {
            int auxMinX = -1;
            if (valid)
            {
                auxMinX = width - 1;
                for (int k = 0; k < height && auxMinX != 0; k++)
                {
                    for (int i = 0; i < depth && auxMinX != 0; i++)
                    {
                        for (int j = 0; j < width && auxMinX != 0; j++)
                        {
                            if (GetAtIndex(k, i, j) && j < auxMinX) auxMinX = j;
                        }
                    }
                }
            }
            return auxMinX;
        }
    }
    public int maxX
    {
        get
        {
            int auxMaxX = -1;
            if (valid)
            {
                auxMaxX = 0;
                for (int k = 0; k < height && auxMaxX != width - 1; k++)
                {
                    for (int i = 0; i < depth && auxMaxX != width - 1; i++)
                    {
                        for (int j = width - 1; j >= 0 && auxMaxX != width - 1; j--)
                        {
                            if (GetAtIndex(k, i, j) && j > auxMaxX) auxMaxX = j;
                        }
                    }
                }
            }
            return auxMaxX;
        }
    }
    public int minZ
    {
        get
        {
            int auxMinZ = -1;
            if (valid)
            {
                auxMinZ = depth - 1;
                for (int k = 0; k < height && auxMinZ != 0; k++)
                {
                    for (int i = 0; i < depth && auxMinZ != 0; i++)
                    {
                        for (int j = 0; j < width && auxMinZ != 0; j++)
                        {
                            if (GetAtIndex(k, i, j) && i < auxMinZ) auxMinZ = i;
                        }
                    }
                }
            }
            return auxMinZ;
        }
    }
    public int maxZ
    {
        get
        {
            int auxMaxZ = -1;
            if (valid)
            {
                auxMaxZ = 0;
                for (int k = 0; k < height && auxMaxZ == 0; k++)
                {
                    for (int i = depth - 1; i >= 0 && auxMaxZ == 0; i--)
                    {
                        for (int j = 0; j < width && auxMaxZ == 0; j++)
                        {
                            if (GetAtIndex(k, i, j) && i > auxMaxZ) auxMaxZ = i;
                        }
                    }
                }
            }
            return auxMaxZ;
        }
    }
    public int minY
    {
        get
        {
            int auxMinY = -1;
            if (valid)
            {
                auxMinY = height - 1;
                for (int k = 0; k < height && auxMinY != 0; k++)
                {
                    for (int i = 0; i < depth && auxMinY != 0; i++)
                    {
                        for (int j = 0; j < width && auxMinY != 0; j++)
                        {
                            if (GetAtIndex(k, i, j) && k < auxMinY) auxMinY = k;
                        }
                    }
                }
            }
            return auxMinY;
        }
    }
    public int maxY
    {
        get
        {
            int auxMaxY = -1;
            if (valid)
            {
                auxMaxY = 0;
                for (int k = height - 1; k >= 0 && auxMaxY != height - 1; k--)
                {
                    for (int i = 0; i < depth && auxMaxY != height - 1; i++)
                    {
                        for (int j = 0; j < width && auxMaxY != height - 1; j++)
                        {
                            if (GetAtIndex(k, i, j) && k > auxMaxY) auxMaxY = k;
                        }
                    }
                }
            }
            return auxMaxY;
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
    public HousingFurnitureData doorMeta;
    public int x = 0;
    public int z = 0;

    public HouseDoor(HousingFurnitureData _doorMeta, int _x = 0, int _z = 0)
    {
        doorMeta = _doorMeta;
        x = _x;
        z = _z;
    }

    public bool validDoor
    {
        get
        {
            return doorMeta != null && x>=0 && z>=0 && doorMeta.height>=2 && doorMeta.width >= 1;
        }
    }
}

[System.Serializable]
public class HouseWindow
{
    public HousingFurnitureData windowMeta;
    public int x = 0;
    public int z = 0;
    public int y = 1;

    public HouseWindow(HousingFurnitureData _windowMeta, int _x = 0, int _z = 0, int _y = 1)
    {
        windowMeta = _windowMeta;
        x = _x;
        z = _z;
        y = _y;
    }

    public bool validWindow
    {
        get
        {
            return x>=0 && z >=0 && y >= 0 && windowMeta.height>=1 && windowMeta.width >= 1;
        }
    }
}

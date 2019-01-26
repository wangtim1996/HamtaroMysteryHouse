using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomMgr : MonoBehaviour
{
    [SerializeField]
    private RoomPool roomPool;
    public int numRoomsToPlace = 3;

    public IntVector dimensions = new IntVector(10,10);

    // 2d array map
    private MapTile[,] map;

    private List<MapTile> nodesToLink = new List<MapTile>();
    

    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlaceRooms();
            DebugMap();
        }
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            LinkRooms();
        }
    }

    private void PlaceRooms()
    {
        ResetMap();
        for (int i = 0; i < numRoomsToPlace; i++)
        {
            GameObject roomObj = roomPool.Get();
            bool placed = false;
            int tries = 0;
            while(!placed && tries < 10)
            {
                tries++;
                IntVector randLoc = new IntVector(Random.Range(0, dimensions.x), Random.Range(0, dimensions.y));
                // Get random rotation
                IntVector.Rotation rot = (IntVector.Rotation)Random.Range(0, (int)IntVector.Rotation.eNumElem);

                Debug.Log("Placing at " + randLoc.x +","+randLoc.y);

                // if it succeeded the map changed and we need to add the game object
                placed = TryPlaceRoom(randLoc, rot, roomObj.GetComponent<Room>().data);
                if(placed)
                    Instantiate(roomObj, new Vector3(randLoc.x, 0, randLoc.y), Quaternion.Euler(0, IntVector.GetRotationAngle(rot), 0), transform);
            }

        }
    }

    bool TryPlaceRoom(IntVector location, IntVector.Rotation rot, RoomData data)
    {
        // temp copy in case of fail
        MapTile[,] newMap = MakeMapCopy(map);
        

        // add in the room
        for(int x = 0; x < data.dimensions.x; x++)
        {
            for(int y = 0; y < data.dimensions.y; y++)
            {
                IntVector newCoords = new IntVector(location.x + x, location.y + y);
                newCoords = newCoords.RotateCentered(rot, location);

                // oob
                if (!newCoords.IsInBounds(dimensions))
                    return false;
                // overlap with another room
                if (newMap[newCoords.x, newCoords.y].type == MapTile.Type.eRoom)
                    return false;

                newMap[newCoords.x, newCoords.y].type = MapTile.Type.eRoom;
            }
        }

        // add in the room entrance hallways
        foreach(IntVector entranceLoc in data.entrances)
        {
            IntVector newCoords = new IntVector(location.x + entranceLoc.x, location.y + entranceLoc.y);
            newCoords = newCoords.RotateCentered(rot, location);

            // oob
            if (!newCoords.IsInBounds(dimensions))
                return false;
            // overlap with another room
            if (newMap[newCoords.x, newCoords.y].type == MapTile.Type.eRoom)
                return false;

            newMap[newCoords.x, newCoords.y].type = MapTile.Type.eHallway;
            nodesToLink.Add(newMap[newCoords.x, newCoords.y]);
        }

        map = MakeMapCopy(newMap);
        return true;
    }

    void ResetMap()
    {
        map = new MapTile[dimensions.x, dimensions.y];
        for(int x = 0; x < dimensions.x; x++)
        {
            for(int y = 0; y < dimensions.y; y++)
            {
                map[x, y] = new MapTile(x, y);
            }
        }

        //clear out old children
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    void DebugMap()
    {
        string mesg = "";
        for(int y = 0; y < dimensions.y; y++)
        {
            for(int x = 0; x < dimensions.x; x++)
            {
                switch(map[x,y].type)
                {
                    case MapTile.Type.eRoom:
                        mesg += 'R';
                        break;
                    case MapTile.Type.eHallway:
                        mesg += 'H';
                        break;
                    case MapTile.Type.eEntrance:
                        mesg += 'E';
                        break;
                    case MapTile.Type.eNone:
                        mesg += 'O';
                        break;
                    default:
                        break;
                }
            }
            mesg += '\n';
        }

        Debug.Log(mesg);
    }

    MapTile[,] MakeMapCopy(MapTile[,] origMap)
    {
        MapTile[,] retmap = new MapTile[dimensions.x, dimensions.y];
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                retmap[x, y] = new MapTile(origMap[x,y]);
            }
        }
        return retmap;
    }

    private void LinkRooms()
    {

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomMgr : MonoBehaviour
{
    [SerializeField]
    private RoomPool roomPool;
    [SerializeField]
    private TileGenAssets tileGenAssets;
    public int numRoomsToPlace = 3;

    public IntVector dimensions = new IntVector(10,10);

    // 2d array map
    private MapTile[,] map;

    private List<MapTile> nodesToLink = new List<MapTile>();

    public static RoomMgr Instance;

    public int playerCurrRoomId = -1;
    int currId;

    public static readonly IntVector[] DIRS = new[]
    {
            new IntVector(0, 1),
            new IntVector(1, 0),
            new IntVector(0, -1),
            new IntVector(-1, 0)
        };


    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("RoomMgr singleton error");
            Destroy(this);
        }
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            GenerateMap();
        }
    }

    public void GenerateMap()
    {
        PlaceRooms();
        LinkRooms();
        GenerateHallways();
        NavMeshMgr.Instance.CreateNavMesh(dimensions);
    }

    private void PlaceRooms()
    {
        ResetMap();
        nodesToLink.Clear();

        //Place saved rooms
        foreach(Transform child in transform)
        {
            Room room = child.GetComponent<Room>();
            if(room == null)
            {
                continue;
            }
            if (room.toBeDeleted)
                continue;
            IntVector loc = room.pos;
            IntVector.Rotation rot = room.rot;

            //Debug.Log("Placing at " + loc.x + "," + loc.y);

            // if it succeeded the map changed and we need to add the game object
            bool placed = TryPlaceRoom(loc, rot, room.data, room.id);
            if (!placed)
            {
                Debug.LogError("HOW COULD HAVE THIS HAPPENED pt2");
                continue;
            }
        }

        for (int i = 0; i < numRoomsToPlace; i++)
        {
            GameObject roomObj = roomPool.Get();
            bool placed = false;
            int tries = 0;
            currId++;

            while(!placed && tries < 10)
            {
                tries++;
                IntVector randLoc = new IntVector(Random.Range(0, dimensions.x), Random.Range(0, dimensions.y));
                // Get random rotation
                IntVector.Rotation rot = (IntVector.Rotation)Random.Range(0, (int)IntVector.Rotation.eNumElem);

                //Debug.Log("Placing at " + randLoc.x +","+randLoc.y);

                // if it succeeded the map changed and we need to add the game object
                placed = TryPlaceRoom(randLoc, rot, roomObj.GetComponent<Room>().data, currId);
                if(placed)
                {
                    GameObject obj = Instantiate(roomObj, new Vector3(randLoc.x, 0, randLoc.y) * tileGenAssets.scale, Quaternion.Euler(0, IntVector.GetRotationAngle(rot), 0), transform);
                    Room room = obj.GetComponent<Room>();
                    room.id = currId;
                    room.pos = randLoc;
                    room.rot = rot;

                    if(i == 0)
                    {
                        room.saved = true;
                    }
                }
            }

        }
    }

    bool TryPlaceRoom(IntVector location, IntVector.Rotation rot, RoomData data, int id)
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
                if (newMap[newCoords.x, newCoords.y].type != MapTile.Type.eNone)
                    return false;

                newMap[newCoords.x, newCoords.y].type = MapTile.Type.eRoom;
                newMap[newCoords.x, newCoords.y].id = id;
            }
        }


        // Add spacers to paths are guaranteed to exist
        for (int x = -1; x < data.dimensions.x; x++)
        {
            IntVector newCoords = new IntVector(location.x + x, location.y - 1);
            newCoords = newCoords.RotateCentered(rot, location);

            // oob
            if (!newCoords.IsInBounds(dimensions))
                return false;
            // overlap with another room
            if (newMap[newCoords.x, newCoords.y].type == MapTile.Type.eRoom)
                return false;

            // only add spacers to empty space
            if (newMap[newCoords.x, newCoords.y].type == MapTile.Type.eNone)
            {
                newMap[newCoords.x, newCoords.y].type = MapTile.Type.eSpacer;
            }
        }
        for (int y = -1; y < data.dimensions.y; y++)
        {
            IntVector newCoords = new IntVector(location.x - 1, location.y + y);
            newCoords = newCoords.RotateCentered(rot, location);

            // oob
            if (!newCoords.IsInBounds(dimensions))
                return false;
            // overlap with another room
            if (newMap[newCoords.x, newCoords.y].type == MapTile.Type.eRoom)
                return false;

            // only add spacers to empty space
            if (newMap[newCoords.x, newCoords.y].type == MapTile.Type.eNone)
            {
                newMap[newCoords.x, newCoords.y].type = MapTile.Type.eSpacer;
            }
        }
        for (int x = -1; x < data.dimensions.x; x++)
        {
            IntVector newCoords = new IntVector(location.x + x, location.y + data.dimensions.y);
            newCoords = newCoords.RotateCentered(rot, location);

            // oob
            if (!newCoords.IsInBounds(dimensions))
                return false;
            // overlap with another room
            if (newMap[newCoords.x, newCoords.y].type == MapTile.Type.eRoom)
                return false;

            // only add spacers to empty space
            if (newMap[newCoords.x, newCoords.y].type == MapTile.Type.eNone)
            {
                newMap[newCoords.x, newCoords.y].type = MapTile.Type.eSpacer;
            }
        }
        for (int y = -1; y < data.dimensions.y + 1; y++)// +1 to get the last spot
        {
            IntVector newCoords = new IntVector(location.x + data.dimensions.x, location.y + y);
            newCoords = newCoords.RotateCentered(rot, location);

            // oob
            if (!newCoords.IsInBounds(dimensions))
                return false;
            // overlap with another room
            if (newMap[newCoords.x, newCoords.y].type == MapTile.Type.eRoom)
                return false;

            // only add spacers to empty space
            if (newMap[newCoords.x, newCoords.y].type == MapTile.Type.eNone)
            {
                newMap[newCoords.x, newCoords.y].type = MapTile.Type.eSpacer;
            }
        }


        // add in the room entrance hallways
        foreach (IntVector entranceLoc in data.entrances)
        {
            IntVector newCoords = new IntVector(location.x + entranceLoc.x, location.y + entranceLoc.y);
            newCoords = newCoords.RotateCentered(rot, location);

            //guaranteed by spacer
            //// oob
            //if (!newCoords.IsInBounds(dimensions))
            //    return false;
            //// overlap with another room
            // No overlapping entrances because I'm lazy
            if (newMap[newCoords.x, newCoords.y].type == MapTile.Type.eEntrance)
                return false;

            newMap[newCoords.x, newCoords.y].type = MapTile.Type.eEntrance;
            newMap[newCoords.x, newCoords.y].id = id;
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
            Room room = child.gameObject.GetComponent<Room>();
            // Don't delete saved rooms
            if (room && (room.saved || room.id == playerCurrRoomId))
                continue;

            if (room)
                room.toBeDeleted = true;
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
                        mesg += map[x,y].id;
                        break;
                    case MapTile.Type.eHallway:
                        mesg += map[x,y].id;
                        //mesg += 'H';
                        break;
                    case MapTile.Type.eEntrance:
                        mesg += 'E';
                        break;
                    case MapTile.Type.eNone:
                        mesg += 'O';
                        break;
                    case MapTile.Type.eSpacer:
                        mesg += 'S';
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
        while(nodesToLink.Count > 1)
        {
            // get two random nodes
            MapTile node0 = GetRandomNode();
            MapTile node1 = GetRandomNode();

            PathNode path = GetPath(node0, node1);

            //Debug.Log("Linking " + node0.coords.x + "," + node0.coords.y + " and " + node1.coords.x + "," + node1.coords.y);

            if (path == null)
            {
                Debug.LogError("No path could be found");
                // can use id to delete room

                return;
            }

            // get new node from hallway
            int newNodeIndex = Random.Range(0, path.totalCost-1);

            // link with hallway
            PathNode currNode = path;
            while(currNode.prevNode != null)
            {

                if (currNode.tile.type == MapTile.Type.eNone || currNode.tile.type == MapTile.Type.eSpacer)
                    currNode.tile.type = MapTile.Type.eHallway;

                if (newNodeIndex == 0)
                    nodesToLink.Add(currNode.tile);
                newNodeIndex--;

                currNode = currNode.prevNode;
            }


            //DebugMap();

        }
    }

    // Get and remove node from list
    private MapTile GetRandomNode()
    {
        int index = Random.Range(0, nodesToLink.Count);
        MapTile ret = nodesToLink[index];
        nodesToLink.RemoveAt(index);
        return ret;
    }

    private PathNode GetPath(MapTile start, MapTile end)
    {
        PathNode startNode = new PathNode(null, start, 0);
        HashSet<MapTile> visited = new HashSet<MapTile>();

        Priority_Queue.SimplePriorityQueue<PathNode> frontier = new Priority_Queue.SimplePriorityQueue<PathNode>();
        frontier.Enqueue(startNode, 0);

        while (frontier.Count != 0)
        {
            PathNode current = frontier.Dequeue();

            if(current.tile.coords == end.coords)
            {
                return current;
            }

            foreach(IntVector dir in DIRS)
            {
                IntVector newCoords = current.tile.coords + dir;
                if (!newCoords.IsInBounds(dimensions))
                    continue;

                MapTile newTile = map[newCoords.x, newCoords.y];
                if (visited.Contains(newTile))
                    continue;

                if (newTile.type == MapTile.Type.eRoom)
                    continue;
                int newCost = current.totalCost;
                //piggy back off of existing hallways
                if (newTile.type != MapTile.Type.eHallway)
                {
                    newCost++;
                }
                PathNode neighbor = new PathNode(current, newTile, newCost);
                visited.Add(newTile);
                frontier.Enqueue(neighbor, newCost);

            }
        }

        return null;
    }

    void GenerateHallways()
    {
        for(int x = 0; x < dimensions.x; x++)
        {
            for(int y = 0; y < dimensions.y; y++)
            {
                MapTile tile = map[x, y];
                if (tile.type == MapTile.Type.eHallway || tile.type == MapTile.Type.eEntrance)
                {
                    byte code = 0;
                    for(int i = 0; i < 4; i++)
                    {
                        IntVector newCoords = tile.coords + DIRS[i];
                        if (!newCoords.IsInBounds(dimensions))
                            continue;

                        MapTile neighbor = map[newCoords.x, newCoords.y];
                        bool isHallway = neighbor.type == MapTile.Type.eHallway || neighbor.type == MapTile.Type.eEntrance;
                        // if curr tile type is entrance, include rooms
                        if (tile.type == MapTile.Type.eEntrance)
                            isHallway = isHallway || (neighbor.type == MapTile.Type.eRoom && neighbor.id == tile.id); 

                        if(isHallway)
                        {
                            code |= (byte)(1 << i);
                        }
                    }
                    TileGen(code, tile.coords);
                }
            }
        }
    }

    void TileGen(byte x, IntVector pos)
    {
        switch (x)
        {
            case 0x1:
                Instantiate(tileGenAssets.deadend, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, 180, 0), transform);
                break;
            case 0x2:
                Instantiate(tileGenAssets.deadend, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, -90, 0), transform);
                break;
            case 0x3:
                Instantiate(tileGenAssets.turn, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, -90, 0), transform);
                break;
            case 0x4:
                Instantiate(tileGenAssets.deadend, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, 0, 0), transform);
                break;
            case 0x5:
                Instantiate(tileGenAssets.line, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, 0, 0), transform);
                break;
            case 0x6:
                Instantiate(tileGenAssets.turn, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, 0, 0), transform);
                break;
            case 0x7:
                Instantiate(tileGenAssets.tIntersect, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, -90, 0), transform);
                break;
            case 0x8:
                Instantiate(tileGenAssets.deadend, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, 90, 0), transform);
                break;
            case 0x9:
                Instantiate(tileGenAssets.turn, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, 180, 0), transform);
                break;
            case 0xA:
                Instantiate(tileGenAssets.line, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, 90, 0), transform);
                break;
            case 0xB:
                Instantiate(tileGenAssets.tIntersect, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, 180, 0), transform);
                break;
            case 0xC:
                Instantiate(tileGenAssets.turn, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, 90, 0), transform);
                break;
            case 0xD:
                Instantiate(tileGenAssets.tIntersect, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, -270, 0), transform);
                break;
            case 0xE:
                Instantiate(tileGenAssets.tIntersect, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, 0, 0), transform);
                break;
            case 0xF:
                Instantiate(tileGenAssets.fourway, new Vector3(pos.x, 0, pos.y) * tileGenAssets.scale, Quaternion.Euler(0, 0, 0), transform);
                break;
            default:
                Debug.LogError("MARCHING SQUARES ERROR");
                break;

        }
    }

    void TestTileGen()
    {
        Debug.Log("gen tiles");
        byte count = 0;
        for(int i = 0; i < 4; i++)
        {
            for(int j = 0; j < 4; j++)
            {
                TileGen(count++, new IntVector(j, i));
            }
        }
    }

    public GameObject GetFirstRoom()
    {
        foreach(Transform child in transform)
        {
            Room room = child.gameObject.GetComponent<Room>();
            if(room & room.toBeDeleted == false)
            {
                return child.gameObject;
            }
        }
        return null;
    }

}

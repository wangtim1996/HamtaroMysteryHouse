using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using K_PathFinder.Samples;

public class AIManager : MonoBehaviour, MapListener
{

    public int maxAI = 3;
    public int roomsPerAI = 2;
    public GameObject AIPrefab;

    private List<GameObject> AI;
    private RoomMgr mgr;

    bool added = false;

    void Start()
    {
        AI = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        mgr = RoomMgr.Instance;
        if (mgr == null)
        {
            added = false;
            return;
        }

        if (!added)
        {
            mgr.listeners.Add(this);
            added = true;
        }
    }

    public void OnMapGenerated()
    {
        Debug.LogWarning("Rooms regenerated");
        foreach(var ai in AI)
        {
            DestroyImmediate(ai);
        }
        AI.Clear();

        List<GameObject> rooms = mgr.GetAllRooms();
        List<GameObject> availableRooms = new List<GameObject>(rooms);
        availableRooms.Remove(mgr.playerCurrRoom.gameObject); // Makes sure starting room is not shared with player or other ai
        for(int i = 0; i < maxAI; ++i)
        {
            var ai = Instantiate(AIPrefab);
            GameObject path = new GameObject("Path");
            SimplePatrolPath p = path.AddComponent<SimplePatrolPath>();
            p.points = new List<Vector3>();
            for(int j = 0; j < roomsPerAI; ++j)
            {
                GameObject room;
                if (j == 0)
                {
                    room = availableRooms[Random.Range(0, availableRooms.Count)];
                    ai.transform.position = room.transform.position;
                    availableRooms.Remove(room);
                }
                else
                {
                    room = rooms[Random.Range(0, rooms.Count)];
                }
                p.points.Add(room.transform.position);
            }
            ai.GetComponent<BasicAI>().patrol = p;
            AI.Add(ai);
            AI.Add(path);
        }
        Debug.Log("Done");
    }
}

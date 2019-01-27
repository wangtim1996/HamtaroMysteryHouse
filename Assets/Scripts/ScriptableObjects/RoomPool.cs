using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Rooms", order = 2)]
public class RoomPool : ScriptableObject
{
    public List<GameObject> rooms;
    
    public GameObject Get()
    {
        if(rooms.Count == 0)
        {
            Debug.LogError("Room Pool is empty");
            return null;
        }
        int randIndex = Random.Range(0, rooms.Count);
        return rooms[randIndex];

    }
}

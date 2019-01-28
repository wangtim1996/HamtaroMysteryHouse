using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public RoomData data;
    public int id;
    public IntVector pos;
    public IntVector.Rotation rot;
    public bool saved = false;
    public bool toBeDeleted = false;
    public bool containsPlayer = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

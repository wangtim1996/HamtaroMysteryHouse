using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public RoomData data;
    public int id;
    public IntVector pos;
    public IntVector.Rotation rot;
    //public bool saved = false;
    public bool toBeDeleted = false;
    [SerializeField]
    private GameObject light;

    private bool _saved = false;
    public bool saved
    {
        get
        {
            return _saved;
        }
        set
        {
            if (_saved && !value)
            {
                _saved = value;
                Debug.LogError("Room got unsaved");
            }
            else if (!_saved && value)
            {
                _saved = value;
                // turn on light
                if(light != null)
                {
                    light.SetActive(true);
                }
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Generate clear trigger
        //IntVector adjVector = new IntVector(pos) + new IntVector(data.dimensions.x-1, data.dimensions.y-1);
        IntVector adjVector = new IntVector(Random.Range(pos.x, pos.x + data.dimensions.x - 1), Random.Range(pos.y, pos.y + data.dimensions.y - 1));
        adjVector = adjVector.RotateCentered(rot, pos);
        Instantiate(data.clearTrigger, new Vector3(adjVector.x, 0, adjVector.y) * 5, Quaternion.identity, transform);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

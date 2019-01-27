using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomClearTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            //set saved
            Room room = null;
            Transform currTransform = transform.parent;
            while(currTransform != null)
            {
                room = currTransform.gameObject.GetComponent<Room>();
                if (room != null)
                {
                    room.saved = true;
                    Environment.instance.nice = true;
                    Destroy(gameObject);
                    return;
                }
                currTransform = currTransform.parent;
            }
            Debug.LogError("Room could not be found");
        }
    }
    
}

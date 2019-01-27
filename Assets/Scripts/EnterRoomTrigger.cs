using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnterRoomTrigger : MonoBehaviour
{
    Room owner;

    private void Awake()
    {
        owner = transform.parent.gameObject.GetComponent<Room>();
    }

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
        if (other.CompareTag("Player"))
        {
            if(owner.id != RoomMgr.Instance.playerCurrRoomId)
            {
                RoomMgr.Instance.playerCurrRoomId = owner.id;
                RoomMgr.Instance.DelayGenerateMap();
            }
            Environment.instance.nice = owner.saved;
        }
    }
}

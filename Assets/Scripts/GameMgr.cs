using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMgr : MonoBehaviour
{
    public static GameMgr Instance;
    public GameObject player;
    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("GameMgr singleton error");
            Destroy(this);
        }
        Instance = this;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform.parent.gameObject;
    }

    private void Start()
    {
        RoomMgr.Instance.GenerateMap();
        GameObject roomGO = RoomMgr.Instance.GetFirstRoom();
        player.transform.position = roomGO.transform.position + new Vector3(0, 0.5f, 0);
        RoomMgr.Instance.playerCurrRoomId = roomGO.GetComponent<Room>().id;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Caught()
    {
        SceneManager.LoadScene("GameOver");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavMeshMgr : MonoBehaviour
{
    public static NavMeshMgr Instance;

    public K_PathFinder.AgentProperties properties; //are necesary to tell PathFinder what it need to generate
    private bool _havmesh = false;


    // Start is called before the first frame update
    void Awake()
    {
        if(Instance != null)
        {
            Debug.LogError("NavMeshMgr singleton error");
            Destroy(this);
        }
        Instance = this;
    }

    public void CreateNavMesh(IntVector intBounds)
    {
        Bounds bounds = new Bounds();
        bounds.SetMinMax(new Vector3(-1, -2, -1), new Vector3(intBounds.x, 0.01f, intBounds.y) * 5);
        if (!_havmesh)
        {
            K_PathFinder.PathFinder.QueueGraph(bounds, properties); //if button pressed first. Generate navmesh
            _havmesh = true;
        }
        else
            K_PathFinder.PathFinder.RemoveGraph(bounds, properties, true);//if navmesh exist. Update navmesh at sellected space            
    }
}

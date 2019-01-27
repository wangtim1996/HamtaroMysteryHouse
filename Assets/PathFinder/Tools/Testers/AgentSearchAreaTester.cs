using K_PathFinder;
using K_PathFinder.Graphs;
using K_PathFinder.PFDebuger;
using K_PathFinder.Samples;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(PathFinderAgent))]
public class AgentSearchAreaTester : MonoBehaviour {
    LineRenderer _line;
    PathFinderAgent _agent;

    [Area()]public int targetArea;

    void Start () {
        _line = GetComponent<LineRenderer>();
        _agent = GetComponent<PathFinderAgent>();
        _agent.SetRecievePathDelegate(RecivePathDelegate, AgentDelegateMode.ThreadSafe);

    }
	
	void Update () {
        PathFinder.QueueGraph(new Bounds(transform.position, Vector3.one * 30), _agent.properties);
        _agent.SetGoalFindNearestArea(targetArea, 100);
    }

    private void RecivePathDelegate(Path path) {
#if UNITY_EDITOR
        Debuger_K.AddLabelFormat(transform.position, "owner: {0}, valid: {1}", path.owner == _agent, path.pathType);
#endif
        ExampleThings.PathToLineRenderer(_line, path, 0.2f);
    }
}

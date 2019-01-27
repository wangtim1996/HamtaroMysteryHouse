using K_PathFinder;
using K_PathFinder.Graphs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathFinderAgent), typeof(CharacterController))]
public class AgentControlerThatPressButton : MonoBehaviour {
    public GameObject moveTarget; //move target for Agent
    [Range(1f, 5f)] public float speed = 2; //speed

    private PathFinderAgent agent;    //reference to agent
    private CharacterController controler; //reference to controler
    private CellPathContentPassControl passControl = null; //reference to current control
    private Vector3 currentMoveTarget; //target move position

    // Use this for initialization
    void Start () {
        controler = GetComponent<CharacterController>();
        agent = GetComponent<PathFinderAgent>();
        agent.SetRecievePathDelegate(PathRecieveDelegate, AgentDelegateMode.ThreadSafe);
        PathFinder.QueueGraph(new Bounds(transform.position, Vector3.one * 40), agent.properties);
        currentMoveTarget = moveTarget.transform.position;
    }

	
	void Update () {    
        if (passControl != null && passControl.state == false)    //if there is pass control and if it not pressed
            currentMoveTarget = passControl.position; //then move where it leads
        else
            currentMoveTarget = moveTarget.transform.position;//else go to current target

        Debug.DrawRay(currentMoveTarget, Vector3.up, Color.green, 1f);

        agent.SetGoalMoveHere(currentMoveTarget, true, true);//order path to current target

        //execute path to current target
        if (agent.haveNextNode == false)
            return;

        //remove next node if closer than radius in top projection. there is other variants of this function
        agent.RemoveNextNodeIfCloserThanRadiusVector2();

        //if next point still exist then move towards it
        if (agent.haveNextNode) {
            Vector2 moveDirection = agent.nextNodeDirectionVector2.normalized;
            controler.SimpleMove(new Vector3(moveDirection.x, 0, moveDirection.y) * speed);
        }
    }

    //in this delegate recieved path are checked to its content
    void PathRecieveDelegate(Path path) {
        var pathContent = path.pathContent;

        for (int i = 0; i < pathContent.Count; i++) {
            if (pathContent[i] is CellPathContentPassControl) {
                passControl = pathContent[i] as CellPathContentPassControl;

                if (passControl.state == false) {
                    currentMoveTarget = passControl.position;
                }

                break;
            }
        }
    }
}

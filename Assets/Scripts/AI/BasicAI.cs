using K_PathFinder.Graphs;
using K_PathFinder;
using K_PathFinder.Samples;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathFinderAgent), typeof(CharacterController), typeof(BasicAISounds))]
public class BasicAI : MonoBehaviour {
    public SimplePatrolPath patrol;
    [Range(0f, 5f)]
    public float speed = 3;
    public float pauseTime = 1;
    public float minimumPursueDistance = 5;
    public float catchRadius = 2;

    private PathFinderAgent agent;
    private CharacterController controler;
    private Animator anim;
    private int currentPoint;

    private float currT = 0;
    private float pauseT = 0;
    private float waitTime;

    void Start() {
        if (patrol == null || patrol.Count == 0)
            Debug.LogError("Not valid patrol path");

        controler = GetComponent<CharacterController>();
        agent = GetComponent<PathFinderAgent>();     
        anim = GetComponent<Animator>();

        //find nearest point
        float sqrDist = float.MaxValue;
        Vector3 pos = transform.position;

        for (int i = 0; i < patrol.Count; i++) {
            float curSqrDist = (patrol[i] - pos).sqrMagnitude;
            if (curSqrDist < sqrDist) {
                sqrDist = curSqrDist;
                currentPoint = i;
            }
        }

        //agent.SetRecievePathDelegate((Path path) => { Debug.Log(path.pathType); });

        //queue navmesh
        PathFinder.QueueGraph(new Bounds(transform.position, Vector3.one * 20), agent.properties);

        waitTime = Random.Range(0.0f, 1.0f);
    }
    
    void Update() {
        if(waitTime > 0)
        {
            waitTime -= Time.deltaTime;
            return;
        }

        var playerpos = GameMgr.Instance.player.transform.Find("RollerBall").transform.position;
        float dist = Vector3.Distance(playerpos, this.transform.position);
        if(dist < catchRadius)
        {
            GameMgr.Instance.Caught();
            return;
        }
        else if (dist < minimumPursueDistance)
        {
            agent.SetGoalMoveHere(playerpos); //queue new path
        }
        else
        {
            agent.SetGoalMoveHere(patrol[currentPoint]);
        }



        //if patrol not valid return
        if (patrol.Count == 0) {
            Debug.Log("patrol.Count == 0");
            return;
        }

        if(currT < pauseT){
            currT += Time.deltaTime;
            anim.SetFloat("MoveSpeed", 0);
            return;
        }

        //if we have points left in path
        if (agent.haveNextNode) {
            //remove point if it is closer than agent radius. return true if removed. there is other versions of that function
            if (agent.RemoveNextNodeIfCloserThanRadiusVector2()) {
                //before that there was point. if after it removed here no point mean we reach end of current path
                //if no points left then path no longer valid and agent get another path
                if (agent.haveNextNode == false) {
                    currentPoint++;//move to next point on patrol   
                    if (currentPoint >= patrol.Count)
                        currentPoint = 0;

                    agent.SetGoalMoveHere(patrol[currentPoint]); //queue new path
                }
                StartPause(pauseTime);
            }

            //if next point still exist then we move towards it
            if (agent.haveNextNode) {
                Vector2 moveDirection = agent.nextNodeDirectionVector2.normalized;
                //Debug.Log(moveDirection);
                controler.SimpleMove(new Vector3(moveDirection.x, 0, moveDirection.y) * speed);

                //Visuals
                this.transform.LookAt(this.transform.position + new Vector3(moveDirection.x, 0, moveDirection.y) * speed); //Look where going
                anim.SetFloat("MoveSpeed", speed);
            }
        }

    }

    void StartPause(float t){
        pauseT = t;
        currT = 0;
    }   
}

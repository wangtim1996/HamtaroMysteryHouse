using K_PathFinder.Graphs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Samples {
    [RequireComponent(typeof(PathFinderAgent), typeof(CharacterController))]
    public class Example6 : MonoBehaviour {
        public SimplePatrolPath patrol;
        [Range(0f, 5f)]
        public float speed = 3;

        private PathFinderAgent agent;
        private CharacterController controler;
        private int currentPoint;

        void Start() {
            if (patrol == null || patrol.Count == 0)
                Debug.LogError("Not valid patrol path");

            controler = GetComponent<CharacterController>();
            agent = GetComponent<PathFinderAgent>();     

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

            agent.SetRecievePathDelegate((Path path) => { Debug.Log(path.pathType); });

            //queue navmesh
            PathFinder.QueueGraph(new Bounds(transform.position, Vector3.one * 20), agent.properties);
        }
        
        void Update() {
            //if patrol not valid return
            if (patrol.Count == 0) {
                Debug.Log("patrol.Count == 0");
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
                }

                //if next point still exist then we move towards it
                if (agent.haveNextNode) {
                    Vector2 moveDirection = agent.nextNodeDirectionVector2.normalized;
                    controler.SimpleMove(new Vector3(moveDirection.x, 0, moveDirection.y) * speed);
                }
            }
            else {
                //get path to current point
                agent.SetGoalMoveHere(patrol[currentPoint]);
            }

        }        
    }
}
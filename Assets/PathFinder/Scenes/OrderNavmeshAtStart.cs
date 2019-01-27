using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Samples {
    //component to just queue some NavMesh at target area
    public class OrderNavmeshAtStart : MonoBehaviour {
        //note Pathfinder operates in Chunks. So it is not exact number and only tell how far from this point chunks should be used      
        [Range(0f, 100f)] public float rangeX = 10f; 
        [Range(0f, 100f)] public float rangeZ = 10f;
        public AgentProperties targetProperties;

        private void Start() {
            //function that tell Pathfinder - generate navmesh here and use this settings to generate
            //it dont destroy old navmesh if it exist. and if there holes in navmesh chunks they will be filled in this case. if navmesh already exist it do nothing
            PathFinder.QueueGraph(
                new Bounds(//bounds to tell chich space should be putet to navmesh queue. This function have more overloads. 
                    transform.position, //current gameObject position
                    new Vector3(rangeX * 2, 0, rangeZ * 2)//Y in this case are used for nothing and can be zero
                ),
                targetProperties);//which properties should be used for generation. properties are settings to tell how navmesh should be generated
        }

        //draw rectangle around this object to see values above 
        void OnDrawGizmosSelected() {
            Color color = Gizmos.color;
            Gizmos.color = new Color(0, 0, 1);
            Vector3 pos = transform.position;
            Gizmos.DrawLine(new Vector3(pos.x - rangeX, pos.y, pos.z - rangeZ), new Vector3(pos.x + rangeX, pos.y, pos.z - rangeZ));
            Gizmos.DrawLine(new Vector3(pos.x - rangeX, pos.y, pos.z + rangeZ), new Vector3(pos.x + rangeX, pos.y, pos.z + rangeZ));
            Gizmos.DrawLine(new Vector3(pos.x - rangeX, pos.y, pos.z - rangeZ), new Vector3(pos.x - rangeX, pos.y, pos.z + rangeZ));
            Gizmos.DrawLine(new Vector3(pos.x + rangeX, pos.y, pos.z - rangeZ), new Vector3(pos.x + rangeX, pos.y, pos.z + rangeZ));
            Gizmos.color = color;
        }
    }
}
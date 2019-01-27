using K_PathFinder.Graphs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Samples {    
    public class Example3 : MonoBehaviour {
        public GameObject 
            cameraGameObject, 
            agentGameObject, 
            targetGameObject;

        PathFinderAgent _agent;
        Camera _camera;
        LineRenderer _line;
        bool update; //used as flag. if true then update path
        
        void Start() {   
            _camera = cameraGameObject.GetComponent<Camera>();
            _line = GetComponent<LineRenderer>();
            _agent = agentGameObject.GetComponent<PathFinderAgent>();           
            _agent.SetRecievePathDelegate(RecivePathDelegate, AgentDelegateMode.ThreadSafe); //setting here delegate to update line renderrer
            update = true;
        }
        
        void Update() {
            RaycastHit hit;
            if (Input.GetMouseButtonDown(0) && Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out hit, 10000f, 1)) {
                agentGameObject.transform.position = hit.point;
                update = true;
            }

            if (Input.GetMouseButtonDown(1) && Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out hit, 10000f, 1)) {
                targetGameObject.transform.position = hit.point;
                update = true;
            }

            if (update) {
                update = false;
                _agent.Update(); //call this so agent update it's position
                _agent.SetGoalMoveHere(targetGameObject.transform.position, true); //here we requesting path
            }
        }

        //Debug and checks handling
        private void RecivePathDelegate(Path path) {
            if(path.pathType != PathResultType.Valid) 
                Debug.LogWarningFormat("path is not valid. reason: {0}", path.pathType);            

            ExampleThings.PathToLineRenderer(_line, path, 0.2f);
        }
    }
}
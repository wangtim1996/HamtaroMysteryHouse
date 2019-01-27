using K_PathFinder.Graphs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Samples {
    [RequireComponent(typeof(LineRenderer), typeof(PathFinderAgent))]
    public class Example12 : MonoBehaviour {
        LineRenderer _line;
        PathFinderAgent _agent;
        Camera _camera;

        public GameObject cameraGameObject;

        [Area]//used attribute to draw int as global dictionary index
        public int targetArea;//target area in global dictionary

        // Use this for initialization
        void Start() {
            _camera = cameraGameObject.GetComponent<Camera>();
            _line = GetComponent<LineRenderer>();
            _agent = GetComponent<PathFinderAgent>();
            _agent.SetRecievePathDelegate(RecivePathDelegate, AgentDelegateMode.ThreadSafe); //setting here delegate to update line renderrer
        }

        // Update is called once per frame
        void Update() {
            RaycastHit hit;
            if (Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out hit, 10000f, 1)) {
                transform.position = hit.point;
                _agent.Update(); //call this so agent update it's position
                _agent.SetGoalFindNearestArea(targetArea, 100, true);
            }
        }

        //Debug and checks handling
        private void RecivePathDelegate(Path path) {
            if (path.pathType != PathResultType.Valid)
                Debug.LogWarningFormat("path is not valid. reason: {0}", path.pathType);

            ExampleThings.PathToLineRenderer(_line, path, 0.5f);
        }
    }
}
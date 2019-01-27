using K_PathFinder.Graphs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Samples {
    public class Example4 : MonoBehaviour {
        public GameObject cameraGameObject, targetGameObject, linePrefab;
        public GameObject[] agents;

        private LineRenderer[] _lines;
        private PathFinderAgent[] _agents;
        private Camera _camera;
        private bool update; //used as flag. if true then update

        // Use this for initialization
        void Start() {
            _camera = cameraGameObject.GetComponent<Camera>();
            _agents = new PathFinderAgent[agents.Length];
            _lines = new LineRenderer[agents.Length];

            for (int i = 0; i < agents.Length; i++) {         
                GameObject lineGameObject = Instantiate(linePrefab);
                _lines[i] = lineGameObject.GetComponent<LineRenderer>();
                _agents[i] = agents[i].GetComponent<PathFinderAgent>();
                int tempValue = i;//or else delegates wound work as expected
                _agents[i].SetRecievePathDelegate((Path path) => { RecivePathDlegate(path, tempValue); }, AgentDelegateMode.ThreadSafe);
                //simple way to queue graph around agents
                PathFinder.QueueGraph(new Bounds(_agents[i].transform.position, Vector3.one * 20), _agents[i].properties);
            }

            update = true;
        }

        // Update is called once per frame
        void Update() {
            RaycastHit hit;
            if (Input.GetMouseButtonDown(0) && Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out hit, 10000f, 1)) {
                targetGameObject.transform.position = hit.point;
                update = true;
            }

            if (update) {
                update = false;
                for (int i = 0; i < _agents.Length; i++) {
                    _agents[i].SetGoalMoveHere(targetGameObject.transform.position, true);
                }
            }
        }

        //simple debug to show path
        private void RecivePathDlegate(Path path, int index) {
            ExampleThings.PathToLineRenderer(_lines[index], path, 0.2f);
        }
    }
}
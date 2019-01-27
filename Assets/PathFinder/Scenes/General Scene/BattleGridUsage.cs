using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace K_PathFinder.Samples {
    [RequireComponent(typeof(CharacterController))]
    public class BattleGridUsage : MonoBehaviour {
        public GameObject target;
        [Range(1, 5)]
        public float speed = 2;
        public Camera cam;

        public Material debugMaterial;
        [Range(1, 7)]
        public int sightSubdivisions = 3;

        [Range(0.025f, 0.15f)]
        public float debugWidth = 0.05f;
        [Range(0.1f, 2f)]
        public float debugHeight = 1f;

        private CharacterController _controler;
        private PathFinderAgent _agent;

        private List<float> _heightTests = new List<float>();

        private GameObject _debugGO;
        private MeshRenderer _debugMeshRenderer;
        private MeshFilter _meshFilter;
        public AgentProperties _properties;

        void Start() {
            _agent = GetComponent<PathFinderAgent>();
            _properties = _agent.properties;

            PathFinder.QueueGraph(new Bounds(transform.position, Vector3.one * 20), _properties); //queue some space in advance
            _controler = GetComponent<CharacterController>();

            //this will happen every time agent recieve new information about grid
            //Idea:
            //1) Take some point that for sure accessible and in close proximity to agent
            //2) Test point visibility at some height
            //3) Take closest one and order path to it
            //this way agent know it can hide in accessible place from something else
            _agent.SetRecieveBattleGridDelegate((IEnumerable<BattleGridPoint> points) => {
                //take some values
                float agentHeight = _properties.height;
                int layers = _properties.includedLayers;
                Vector3 testTarget = target.transform.position;

                //update small list with subdivisions
                _heightTests.Clear();
                for (int i = 0; i < sightSubdivisions; i++) {
                    _heightTests.Add(1f - (1f / sightSubdivisions * i));
                }

                BattleGridPoint targetPoint = null;
                float highestValue = 0;

                //debug
                int pointsLength = points.Count();
                Vector3[] meshVerts = new Vector3[pointsLength * 4];
                int[] meshTris = new int[pointsLength * 6];
                int debugIndex = 0;      
     
                foreach (var point in points) {
                    //take point position and raycast to it in different heights to test visibility
                    float value = 1f;
                    for (int v = 0; v < _heightTests.Count; v++) {
                        if (Physics.Linecast(testTarget, point.positionV3 + (Vector3.up * _heightTests[v] * agentHeight), layers) == false) {
                            value = _heightTests[v];
                        }
                        else
                            break;
                    }

                    //multipy it by normalized distance to point. 1f is closest and 0f is farthest
                    value *= Mathf.Clamp01((10 - Vector3.Distance(transform.position, point.positionV3)) / 10);

                    if (value > highestValue) {
                        highestValue = value;
                        targetPoint = point;
                    }

                    //debug
                    //drawing lots of small lines to debug this value. 
                    //cause Debuger_K dont exist in builded project for reasons
                    Vector3 cameraKeyPoint = Vector3.Cross(cam.transform.position - point.positionV3, Vector3.up).normalized;
                    meshVerts[debugIndex * 4] = point.positionV3 + (cameraKeyPoint * -debugWidth); //LB
                    meshVerts[debugIndex * 4 + 1] = point.positionV3 + (cameraKeyPoint * debugWidth); //RB
                    meshVerts[debugIndex * 4 + 2] = point.positionV3 + (cameraKeyPoint * -debugWidth) + new Vector3(0, value * debugHeight, 0); //LT
                    meshVerts[debugIndex * 4 + 3] = point.positionV3 + (cameraKeyPoint * debugWidth) + new Vector3(0, value * debugHeight, 0); //RT

                    meshTris[debugIndex * 6 + 0] = debugIndex * 4 + 1;
                    meshTris[debugIndex * 6 + 1] = debugIndex * 4 + 0;
                    meshTris[debugIndex * 6 + 2] = debugIndex * 4 + 2;
                    meshTris[debugIndex * 6 + 3] = debugIndex * 4 + 1;
                    meshTris[debugIndex * 6 + 4] = debugIndex * 4 + 2;
                    meshTris[debugIndex * 6 + 5] = debugIndex * 4 + 3;
                    debugIndex++;          
                }

                //also debug
                Mesh mesh = new Mesh();
                mesh.vertices = meshVerts;
                mesh.triangles = meshTris;
                _meshFilter.mesh = mesh;

                //send agent to this point
                if(targetPoint != null)
                    _agent.SetGoalMoveHere(targetPoint.positionV3);                
            }, 
            AgentDelegateMode.ThreadSafe);//to sure it's in main thread

            StartCoroutine(UpdateBattleGridCall());

            _debugGO = new GameObject("debug battle grid GO");
            _debugMeshRenderer = _debugGO.AddComponent<MeshRenderer>();
            _meshFilter = _debugGO.AddComponent<MeshFilter>();
            _debugMeshRenderer.material = debugMaterial;
        }

        void Update() {
            //simple usage of agent next point
            if (_agent.haveNextNode == false)
                return;

            //if next point near agent then remove it
            _agent.RemoveNextNodeIfCloserThanRadiusVector2();

            //it next point still exist then move in that direction with that speed
            if (_agent.haveNextNode) {
                Vector2 moveDirection = _agent.nextNodeDirectionVector2.normalized;
                _controler.SimpleMove(new Vector3(moveDirection.x, 0, moveDirection.y) * speed);
            }
        }

        IEnumerator UpdateBattleGridCall() {
            while (true) {
                _agent.SetGoalGetBattleGrid(10);
                yield return new WaitForSeconds(0.25f);
            }
        }
    }
}

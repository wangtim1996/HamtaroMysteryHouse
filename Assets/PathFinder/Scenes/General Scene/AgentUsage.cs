using K_PathFinder;
using K_PathFinder.Graphs;
using K_PathFinder.PFDebuger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//example of basic agent
//it creates two pathfinder agents in process. one for get actual path and one for debug path
namespace K_PathFinder.Samples {
    [RequireComponent(typeof(CharacterController))]
    public class AgentUsage : MonoBehaviour {
        const float cameraMaxDistance = 50f, cameraMinDistance = 5f;

        //camera control
        public Camera myCamera;
        [Range(10f, 90f)]
        public float cameraAngle = 45;
        [Range(cameraMinDistance, cameraMaxDistance)]
        public float cameraDistance = 10;
        private float cameraTargetDistance;

        [Range(1f, 5f)] public float speed = 2;
        public AgentProperties properties;
        PathFinderAgent 
            _agentForPath, //this agent for actual navigation
            _agentForDebugPath; //this agent for debuging path
        CharacterController _controler;

        //debug values 
        public Material debugMaterial, pathMaterial;
        [Range(0f, 1f)] public float debugWidth = 0.1f;
        private LineRenderer _lineDebuger, _linePath;

        //some stuff for ignoring UI
        PointerEventData pointerEventData;
        List<RaycastResult> eventHits = new List<RaycastResult>();

        void Start() {
            //creating agents
            //In normal case you probably want to add PathFinderAgent as normal component
            //But in that case one agent for continuous path requests and other request path just when left click performed and actual movement           

            //add agent for actual movement
            _agentForPath = gameObject.AddComponent<PathFinderAgent>();
            _agentForPath.properties = properties;

            //add agent which used just for showing path
            _agentForDebugPath = gameObject.AddComponent<PathFinderAgent>();
            _agentForDebugPath.properties = properties;

            _controler = GetComponent<CharacterController>();

            transform.rotation = Quaternion.Euler(Vector3.zero);
            cameraTargetDistance = cameraDistance;

            //getting gameobjects with lineRenderer
            _linePath = ExampleThings.GetLineRenderer(pathMaterial, debugWidth);
            _lineDebuger = ExampleThings.GetLineRenderer(debugMaterial, debugWidth);


            //handling of path debug and messages in case something went wrong

            //delegate for normal path
            _agentForPath.SetRecievePathDelegate((Path path) => {
                if (path.pathType != PathResultType.Valid) Debug.LogWarningFormat("path is not valid. reason: {0}", path.pathType);
                ExampleThings.PathToLineRenderer(_linePath, path, 0.3f); //it drawed slightly higher
            }, AgentDelegateMode.ThreadSafe);

            //delegate for path debug
            _agentForDebugPath.SetRecievePathDelegate((Path path) => {
                if (path.pathType != PathResultType.Valid) {
                    switch (path.pathType) {       
                        case PathResultType.InvalidAgentOutsideNavmesh:
                            Debug.Log("No path. Or navmesh generating where agent are");
                            break;
                        case PathResultType.InvalidTargetOutsideNavmesh:
                            Debug.Log("No path. Or navmesh are generating where your mouse cursor are");
                            break;
                        case PathResultType.InvalidNoPath:
                            Debug.Log("No path. Or navmesh are generating here right now");
                            break;      
                        default:
                            Debug.LogWarningFormat("path is not valid. reason: {0}", path.pathType);
                            break;
                    }          
                }
                ExampleThings.PathToLineRenderer(_lineDebuger, path, 0.2f);//it drawed slightly lower
            }, AgentDelegateMode.ThreadSafe);

            pointerEventData = new PointerEventData(EventSystem.current);
        }

        void Update() {
            //fancy camera
            cameraTargetDistance -= Input.GetAxis("Mouse ScrollWheel") * 5;//add mouse wheel
            cameraTargetDistance = Mathf.Clamp(cameraTargetDistance, cameraMinDistance, cameraMaxDistance);//clamp camera distance
            cameraDistance = Mathf.Lerp(cameraDistance, cameraTargetDistance, 20 * Time.deltaTime);//lerp camera to avoid jiggling
            myCamera.transform.position = transform.position + (Quaternion.Euler(cameraAngle, 0, 0) * Vector3.back) * cameraDistance;//our position + camera direction * camera distance 
            myCamera.transform.LookAt(transform.position, Vector3.up);
            
            //detecting if over button (not like it's lots of buttons but still)      
            pointerEventData.position = Input.mousePosition;
            EventSystem.current.RaycastAll(pointerEventData, eventHits);

            //raycasting
            Ray ray = myCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            //exclude anything that include buttons
            if (eventHits.Exists(x => x.gameObject.GetComponent<Button>() != null) == false && Physics.Raycast(ray, out hit, 10000f, _agentForPath.properties.includedLayers)) {         
                PathFinder.QueueGraph(transform.position, hit.point, _agentForDebugPath.properties); //queue some graph in straight line
                _agentForDebugPath.SetGoalMoveHere(hit.point, true, true);
                //setting goal
                if (Input.GetMouseButtonDown(0))
                    _agentForPath.SetGoalMoveHere(hit.point, true, true);
            }

            if (_agentForPath.haveNextNode == false)
                return;

            //remove next node if closer than radius in top projection. there is other variants of this function
            _agentForPath.RemoveNextNodeIfCloserThanRadiusVector2();

            //if next point still exist then we move towards it
            if (_agentForPath.haveNextNode) {
                Vector2 moveDirection = _agentForPath.nextNodeDirectionVector2.normalized;
                _controler.SimpleMove(new Vector3(moveDirection.x, 0, moveDirection.y) * speed);
            }

            PathFinder.QueueGraph(new Bounds(transform.position, Vector3.one * 20), properties);
        }

        

    }
}

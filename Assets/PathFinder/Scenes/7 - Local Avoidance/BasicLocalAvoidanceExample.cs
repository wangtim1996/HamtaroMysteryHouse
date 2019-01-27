using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace K_PathFinder.Samples {
    //Example 7: Local Avoidance

    //In order magic to happen you need to give PathFinderAgent 2 values:
    //1) Agents should know it's own current velocity. It is done by passing it to PathFinderAgent.velocity. It is Vector2 in top projection
    //2) Agents should know prefered velocity. PathFinderAgent.prefVelocity. Also Vector2 in top projection
    //Also agent should be near navmesh (this is actualy work in progress. agent need to be at least in chunk where some sort of navmesh data exist)
    //THEN there is magical function PathFinder.UpdateRVO() you need to call. this will flip flag that tell pathfinder to calculate safe velocity to all agents
    //suggest you to put it into your main game cycle. (cause i have no clue how often you need this)
    //It's happening in sepparated thread so result is not instant. After it's done agent will recieve new value into PathFinderAgent.safeVelocity
    //If agent maintain this velocity it will not collide with other agents or navmesh
    //All velocity is in units/second

    [RequireComponent(typeof(PathFinderAgent), typeof(CharacterController))]
    public class BasicLocalAvoidanceExample : MonoBehaviour {
        public Vector3 targetPosition;
        [Range(0.1f, 3f)] public float velocity = 3; //target valocity

        //reference to key things
        private PathFinderAgent _agent;
        private CharacterController _controler;
        private TrailRenderer _trail;

        void Start() {
            _agent = GetComponent<PathFinderAgent>();
            _controler = GetComponent<CharacterController>();
            _trail = GetComponent<TrailRenderer>();
            if(_trail != null) {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
                _trail.material = mat;
            }
        }

        void Update() {
            if (_agent.properties == null)
                return;

            //queue some graph around agent so it's always exist (dont recomend to do it if there is lots of agents but okay in small scale)
            PathFinder.QueueGraph(new Bounds(transform.position, new Vector3(10, 10, 10)), _agent.properties);
            Vector3 position = transform.position;
            //expected direction agent will go
            Vector2 prefVelocity = new Vector2(targetPosition.x - position.x, targetPosition.z - position.z);

            //get lowest value between agent max velocity and current target velocity so agent wound try to go faster than it can
            float maxPrefVelocityLength = Mathf.Min(_agent.maxAgentVelocity, velocity);
            //if length of prefered velocity vector is greater than max velocity then now it's not
            if (prefVelocity.magnitude > maxPrefVelocityLength)
                prefVelocity = prefVelocity.normalized * maxPrefVelocityLength;

            //setting agent prefered velocity you expect it to go
            _agent.preferableVelocity = prefVelocity;

            //taking agent last safe velocity
            Vector2 safeVelocity = _agent.safeVelocity;

            //move agent to the direction of safe velocity
            _controler.SimpleMove(new Vector3(safeVelocity.x, 0, safeVelocity.y));

            //cause there is no calculation of agent actual velocity in this example it's just set it to safe velocity cause agent just follow it anyway
            _agent.velocity = safeVelocity;       
        }
    }

#if UNITY_EDITOR
    //simple extention to show handle of targetPosition
    [CustomEditor(typeof(BasicLocalAvoidanceExample)), CanEditMultipleObjects]
    public class BasicLocalAvoidanceExampleEditor : Editor {
        protected virtual void OnSceneGUI() {
            BasicLocalAvoidanceExample t = (BasicLocalAvoidanceExample)target;

            EditorGUI.BeginChangeCheck();
            Vector3 newTargetPosition = Handles.PositionHandle(t.targetPosition, Quaternion.identity);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(t, "Change target");
                t.targetPosition = newTargetPosition;
            }
        }
    }
#endif
}
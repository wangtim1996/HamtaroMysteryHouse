#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
using System.Collections.Generic;
#endif
using UnityEngine;

namespace K_PathFinder {
//#if UNITY_EDITOR
//    [ExecuteInEditMode()]
//#endif
    public class RaycastTest : MonoBehaviour {
#if UNITY_EDITOR
        public AgentProperties properties;
        public float range = 5f;
        public int tests = 30;
        public bool doForward = true;
        
        [SerializeField]
        public ParticularData[] dataList;
        Vector2[] testDirections;

   
        private const int circleThings = 50;
        private Vector3[] circle;
        private RaycastHitNavMesh2[] hits;

        void Start() {
            circle = new Vector3[circleThings];
            for (int i = 0; i < circleThings; i++) {
                float x = Mathf.Cos((i / (float)circleThings) * 2 * Mathf.PI);
                float z = Mathf.Sin((i / (float)circleThings) * 2 * Mathf.PI);
                circle[i] = new Vector3(x, 0, z);
            }
        }

        void Update() {
            if (properties == null) {
                Debug.LogWarning("no properties");
                return;
            }

            RaycastHit raycastHit;
            if (!Physics.Raycast(transform.position, Vector3.down, out raycastHit, 10)) {
                Debug.LogWarning("no raycast hit");
                return;
            }

            Vector3 p = raycastHit.point;
            Debug.DrawLine(transform.position, p, Color.red);

            Debuger_K.ClearGeneric();

            //PathFinder.Raycast2(p, new Vector3(forward.x, 0, forward.z), properties);

            foreach (var d in dataList) {
                if (d.enabled) {
                    RaycastHitNavMesh2 hit;
                    PathFinder.Raycast(d.position, d.direction, properties, out hit);
                    DrawLine(d.position, hit);
                }
            }


            if (doForward) {
                RaycastHitNavMesh2 hit;
                PathFinder.Raycast(p, transform.forward, range, properties, out hit);
                DrawLine(p, hit);
            }

           if(tests > 0) {
                if (testDirections == null)
                    testDirections = new Vector2[0];

                if(testDirections.Length != tests) {
                    testDirections = new Vector2[tests];
                    for (int i = 0; i < tests; i++) {
                        float x = Mathf.Cos((i / (float)tests) * 2 * Mathf.PI);
                        float z = Mathf.Sin((i / (float)tests) * 2 * Mathf.PI);
                        testDirections[i] = new Vector2(x, z);
                    }
                }

                PathFinder.Raycast(p.x , p.y, p.z, testDirections, properties, range,  ref hits);
                for (int i = 0; i < hits.Length; i++) {
                    DrawLine(p, hits[i]);
                }
            }

           if(doForward | tests > 0) {
                for (int i = 0; i < circleThings - 1; i++) {
                    Debug.DrawLine(p + (circle[i] * range), p + (circle[i + 1] * range), Color.blue);
                }
                Debug.DrawLine(p + (circle[circleThings - 1] * range), p + (circle[0] * range), Color.blue);
            }


            //RaycastHitNavMesh raycastHitNavMesh;
            //for (int i = 0; i < tests; i++) {
            //    float x = Mathf.Cos((i / (float)tests) * 2 * Mathf.PI);
            //    float z = Mathf.Sin((i / (float)tests) * 2 * Mathf.PI);

            //    //var q = Quaternion.LookRotation(transform.forward + new Vector3(x, 0, z), Vector3.up);



            //    //if (PathFinder.Raycast(p, new Vector3(x, 0, z), properties, out raycastHitNavMesh) && raycastHitNavMesh.resultType != NavmeshRaycastResultType.OutsideGraph) {
            //    //    Debuger_K.AddLine(p, raycastHitNavMesh.point, Color.blue);
            //    //    Debuger_K.AddLabel(raycastHitNavMesh.point, "H");
            //    //}

            //    PathFinder.Raycast2(p, new Vector3(x, 0, z), properties);
            //}
        }

        private void DrawLine(Vector3 start, RaycastHitNavMesh2 hit) {
            Color color;
            switch (hit.resultType) {
                case NavmeshRaycastResultType2.Nothing:
                    color = Color.red;
                    break;
                case NavmeshRaycastResultType2.NavmeshBorderHit:
                    color = Color.green;
                    break;
                case NavmeshRaycastResultType2.ReachMaxDistance:
                    color = Color.blue;
                    break;
                default:
                    color = Color.magenta;
                    break;
            }
            Debug.DrawLine(start, hit.point, color);
            //Debuger_K.AddLabel(hit.point, hit.resultType.ToString());
        }
#endif
    }

    [System.Serializable]
    public struct ParticularData {
        public bool enabled;
        public Vector3 position;
        public Vector2 direction;
    }
}
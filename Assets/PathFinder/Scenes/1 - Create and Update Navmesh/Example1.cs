using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace K_PathFinder.Samples {
    public class Example1 : MonoBehaviour {
        public AgentProperties properties; //are necesary to tell PathFinder what it need to generate

        public int drops = 20;
        public float platformSize = 20;
        public float dropHeightMin = 10f;
        public float dropHeightMax = 20f;
        public float minSize = 0.5f;
        public float maxSize = 2f;
        public PrimitiveType[] allowedTypes;

        private List<GameObject> _existedPrimitives = new List<GameObject>();
        private List<Bounds> _existedPrimitivesBounds = new List<Bounds>();
        private bool _havmesh = false;

        void Start() {
            DropThings();
        }
        
        /// <summary>
        /// Generate some GameObjects with random size and position
        /// </summary>
        public void DropThings() {
            for (int i = 0; i < _existedPrimitives.Count; i++) {
                Destroy(_existedPrimitives[i]);
            }
            _existedPrimitives.Clear();
            _existedPrimitivesBounds.Clear();

            for (int i = 0; i < drops; i++) {
                GameObject primitive = GameObject.CreatePrimitive(allowedTypes[Random.Range(0, allowedTypes.Length)]);
                primitive.transform.rotation = Random.rotation;
                primitive.transform.localScale = new Vector3(
                        Random.Range(minSize, maxSize),
                        Random.Range(minSize, maxSize),
                        Random.Range(minSize, maxSize));

                Collider collider = primitive.GetComponent<Collider>();
                Vector3 boundsSize = collider.bounds.size;        
                Vector3? position = null;

                for (int r = 0; r < 10; r++) {
                    Vector3 randomPos = new Vector3(
                        Random.Range(-platformSize, platformSize), 
                        Random.Range(dropHeightMin, dropHeightMax), 
                        Random.Range(-platformSize, platformSize));

                    bool flag = true;

                    foreach (var item in _existedPrimitivesBounds) {
                        if (item.Intersects(new Bounds(randomPos, boundsSize))) {
                            flag = false;
                            break;
                        }
                    }
                    if(flag) {
                        position = randomPos;
                        break;
                    }
                }
                if (position.HasValue) {
                    primitive.transform.position = position.Value;
                    _existedPrimitives.Add(primitive);
                    _existedPrimitivesBounds.Add(new Bounds(position.Value, boundsSize));
                    primitive.AddComponent<Rigidbody>();
                }
                else {
                    Destroy(primitive);
                }
            }
        }

        /// <summary>
        /// Create actual NavMesh at target space
        /// PathFinder.QueueGraph and PathFinder.RemoveGraph are have multiple overloads here is simple one
        /// </summary>
        public void CreateNavMesh() {
            Bounds bounds = new Bounds(transform.position, new Vector3(platformSize, platformSize, platformSize) * 2.5f);
            if (!_havmesh) {
                PathFinder.QueueGraph(bounds, properties); //if button pressed first. Generate navmesh
                _havmesh = true;
            }
            else
                PathFinder.RemoveGraph(bounds, properties, true);//if navmesh exist. Update navmesh at sellected space            
        }
    }
}
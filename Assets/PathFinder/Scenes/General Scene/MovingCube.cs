using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using K_PathFinder;


namespace K_PathFinder.Samples {
    public class MovingCube : MonoBehaviour {
        public AgentUsage tragetAgent;
        [Range(2f, 10f)]
        public float randomDistance = 5f;

        public List<GameObject> cubes = new List<GameObject>();

        private float _startX, _startZ;

        void Start() {
            _startX = transform.position.x;
            _startZ = transform.position.z;
        }

        public void Usage() {
            Bounds[] bounds = new Bounds[cubes.Count * 2];
            for (int i = 0; i < cubes.Count; i++) {
                Collider c = cubes[i].GetComponent<Collider>();
                bounds[i * 2] = c.bounds;
                cubes[i].transform.position = new Vector3(_startX + Random.Range(-randomDistance, randomDistance), cubes[i].transform.position.y, _startZ + Random.Range(-randomDistance, randomDistance));
                bounds[i * 2 + 1] = c.bounds;
            }
            PathFinder.RemoveGraph(tragetAgent.properties, true, bounds);
        }
    }
}
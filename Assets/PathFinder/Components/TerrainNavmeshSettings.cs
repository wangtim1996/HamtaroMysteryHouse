using UnityEngine;
using System.Collections;

namespace K_PathFinder {
    [RequireComponent(typeof(Terrain))][ExecuteInEditMode()]
    public class TerrainNavmeshSettings : MonoBehaviour {
        public Terrain terrain;
        [SerializeField]public int[] data; //index is splat map index. value is area dictionary value

        void OnEnable() {
            terrain = GetComponent<Terrain>();
            if (data == null) {
                Debug.Log("Creating new terrain data");
                data = new int[0];
            }
        }
    }
}
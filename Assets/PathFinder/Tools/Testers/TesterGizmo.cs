using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.PFDebuger.Helpers {
    public class TesterGizmo : MonoBehaviour {
        public Color color = new Color(1, 0, 0, 0.3f);
        public float size = 0.5f;

        void OnDrawGizmos() {
            Gizmos.color = color;
            Gizmos.DrawCube(transform.position, new Vector3(size, size, size));
        }
    }
}
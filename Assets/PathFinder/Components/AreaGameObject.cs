using UnityEngine;
using System.Collections;

namespace K_PathFinder {
    public class AreaGameObject : MonoBehaviour {     
        [Area(false)] public int areaInt = 0;
        public Area GetArea() {
            return PathFinder.GetArea(areaInt);
        }
    }
}

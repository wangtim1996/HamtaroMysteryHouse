using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Samples {
    public class ThingLikeGameMaster : MonoBehaviour {
        WaitForSeconds wait = new WaitForSeconds(0.032f);

        IEnumerator Start() {
            while (true) {
                PathFinder.UpdateRVO();
                yield return wait;
            }
        }
    }
}
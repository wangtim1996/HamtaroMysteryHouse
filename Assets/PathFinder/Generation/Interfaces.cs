using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using K_PathFinder.NodesNameSpace;

namespace K_PathFinder{
    public interface IGraphPoint {
        Vector3 positionV3 {
            get;
        }
        Vector2 positionV2 {
            get;
        }
        float x {
            get;
        }
        float y {
            get;
        }
        float z {
            get;
        }
    }
}
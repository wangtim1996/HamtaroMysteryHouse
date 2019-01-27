using UnityEngine;
using System.Collections.Generic;

using K_PathFinder.NodesNameSpace;


namespace K_PathFinder.Graphs {
    public class JumpPortalBase : NodeAbstract {
        public Dictionary<Cell, Vector3> cellMountPoints { get; private set; }
        public Vector3 normal { get; private set; }

        public JumpPortalBase(Dictionary<Cell, Vector3> cellMountPoints, Vector3 axis, Vector3 normal) : base(axis) {
            this.normal = normal;
            this.cellMountPoints = cellMountPoints;
        }      
    }
}
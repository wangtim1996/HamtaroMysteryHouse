using K_PathFinder.Graphs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder {
    public struct NavMeshTemplateDestruction {
        public readonly GeneralXZData data;
        public readonly bool queueNewGraphAfter;

        public NavMeshTemplateDestruction(GeneralXZData data, bool queueNewGraphAfter) {
            this.data = data;
            this.queueNewGraphAfter = queueNewGraphAfter;
        }

        public NavMeshTemplateDestruction(XZPosInt pos, AgentProperties properties, bool queueNewGraphAfter) : this(new GeneralXZData(pos, properties), properties) {}
    }
}
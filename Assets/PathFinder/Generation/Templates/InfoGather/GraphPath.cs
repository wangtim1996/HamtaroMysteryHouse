using System.Collections.Generic;
using System.Linq;

using K_PathFinder.Graphs;

namespace K_PathFinder.PathGeneration {
    public class GraphPathSimple {
        public float cost;
        public Graph graph;
        public GraphPathSimple(Graph graph, float cost) {
            this.graph = graph;
            this.cost = cost;
        }
    }

    public class GraphPath {
        public float cost;
        public List<Graph> path = new List<Graph>();
        public GraphPath(Graph first) {
            path.Add(first);
        }

        public GraphPath(GraphPath existedPath, Graph newSegment) {
            path = new List<Graph>(existedPath.path);
            path.Add(newSegment);
        }

        public Graph Last {
            get { return path.Last(); }
        }

        public bool Contains(Graph graph) {
            return path.Contains(graph);
        }
    }
}
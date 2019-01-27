using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using K_PathFinder;

using K_PathFinder.EdgesNameSpace;
using K_PathFinder.GraphGeneration;
using K_PathFinder.Graphs;
using K_PathFinder.NodesNameSpace;
#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.GraphGeneration {
    public class TriangulatorDataSet {
        TriangulatorNode[] _nodes;
        TriangulatorNodeData[] _data;
        List<TriangulatorEdge> _edges = new List<TriangulatorEdge>();
        List<int> _edgesUsage = new List<int>();
        int[][] visibilityField;

        public Area area;
        public int layer;
        public Passability passability;


        //List<TriangulatorEdge>[][] edgeMap;

        List<TriangulatorEdge>[] edgeMap;
        const int EDGE_MAP_SIZE = 10;
        Vector2 chunkPos;
        float edgeMapPiselSize;
        TriangulatorEdge curAddEdge;
        HashSet<TriangulatorEdge> isVisibleTemp = new HashSet<TriangulatorEdge>();

        public TriangulatorDataSet(
            NavMeshTemplateCreation template,
            List<NodeAbstract> nodes, List<EdgeAbstract> edges, Dictionary<NodeAbstract, TriangulatorNodeData> values,
            int layer, Area area, Passability passability) {
            this.area = area;
            this.layer = layer;
            this.passability = passability;

            Dictionary<NodeAbstract, int> indexes = new Dictionary<NodeAbstract, int>();

            _nodes = new TriangulatorNode[nodes.Count];
            _data = new TriangulatorNodeData[nodes.Count];

            for (int i = 0; i < nodes.Count; i++) {
                _nodes[i] = new TriangulatorNode(nodes[i], i);
                _data[i] = values[nodes[i]];
                indexes.Add(nodes[i], i);
            }

            edgeMap = new List<TriangulatorEdge>[EDGE_MAP_SIZE * EDGE_MAP_SIZE];
            for (int i = 0; i < EDGE_MAP_SIZE * EDGE_MAP_SIZE; i++) {
                edgeMap[i] = new List<TriangulatorEdge>();
            }
            chunkPos = template.chunkData.realPositionV2;
            edgeMapPiselSize = EDGE_MAP_SIZE / PathFinder.gridSize;

            foreach (var edge in edges) {
                AddEdge(indexes[edge.a], indexes[edge.b], 1, false);
            }

            //debug
            //Vector3 realNotOffsetedPosition = template.chunkData.realPositionV3;
            //for (int x = 0; x < EDGE_MAP_SIZE + 1; x++) {
            //    Vector3 A = realNotOffsetedPosition + new Vector3(x * edgeMapPiselSize, 0, 0);
            //    Vector3 B = realNotOffsetedPosition + new Vector3(x * edgeMapPiselSize, 0, PathFinder.gridSize);
            //    Debuger_K.AddLine(A, B, Color.red);
            //}

            //for (int z = 0; z < EDGE_MAP_SIZE + 1; z++) {
            //    Vector3 A = realNotOffsetedPosition + new Vector3(0, 0, z * edgeMapPiselSize);
            //    Vector3 B = realNotOffsetedPosition + new Vector3(PathFinder.gridSize, 0, z * edgeMapPiselSize);
            //    Debuger_K.AddLine(A, B, Color.red);
            //}
        }
        //TEST
        //const int GRID_TEST_SIZE = 10;


        //bool[][] lineTempMap;
        //TriangulatorEdge curTestEdge;
        //void TestDelegate(int x, int y) {
        //    //Debug.LogFormat("x: {0}, y: {1}, id: {2}", x, y, curTestEdge.id);  
        //    edgeMap[Mathf.Clamp(x, 0, GRID_TEST_SIZE - 1)][Mathf.Clamp(y, 0, GRID_TEST_SIZE - 1)].Add(curTestEdge);
        //}
        //public void GenerateEdgeMap(int librarySize, NavMeshTemplateRecast template) {
        //    //bool[][] tempMap = new bool[librarySize][];
        //    edgeMap = new List<TriangulatorEdge>[librarySize][];
        //    Vector2 chunkPos = template.chunkData.realPositionV2;

        //    float totalSize = PathFinder.gridSize;
        //    float pixelSize = totalSize / librarySize;

        //    for (int x = 0; x < librarySize; x++) {
        //        //tempMap[x] = new bool[librarySize];
        //        edgeMap[x] = new List<TriangulatorEdge>[librarySize];

        //        for (int z = 0; z < librarySize; z++) {
        //            edgeMap[x][z] = new List<TriangulatorEdge>();
        //        }
        //    }

        //    foreach (var edge in _edges) {
        //        curTestEdge = edge;               

        //        DDARasterization.DrawLine(
        //            (nodes[edge.a].x - chunkPos.x) / pixelSize, 
        //            (nodes[edge.a].z - chunkPos.y) / pixelSize, 
        //            (nodes[edge.b].x - chunkPos.x) / pixelSize, 
        //            (nodes[edge.b].z - chunkPos.y) / pixelSize,
        //            librarySize, 
        //            TestDelegate);
        //    }

        //    //debug
        //    Vector3 realNotOffsetedPosition = template.chunkData.realPositionV3;
        //    for (int x = 0; x < librarySize + 1; x++) {
        //        Vector3 A = realNotOffsetedPosition + new Vector3(x * pixelSize, 0, 0);
        //        Vector3 B = realNotOffsetedPosition + new Vector3(x * pixelSize, 0, totalSize);
        //        Debuger_K.AddLine(A, B, Color.red);
        //    }

        //    for (int z = 0; z < librarySize + 1; z++) {
        //        Vector3 A = realNotOffsetedPosition + new Vector3(0, 0, z * pixelSize);
        //        Vector3 B = realNotOffsetedPosition + new Vector3(totalSize, 0, z * pixelSize);
        //        Debuger_K.AddLine(A, B, Color.red);
        //    }

        //    for (int x = 0; x < librarySize; x++) {
        //        for (int z = 0; z < librarySize; z++) {
        //            Vector3 pixelPos = realNotOffsetedPosition + (new Vector3(x + 0.5f, 0, z + 0.5f) * pixelSize);
        //            Debuger_K.AddDot(pixelPos, Color.cyan);
        //            Debuger_K.AddLabel(pixelPos, edgeMap[x][z].Count);

        //            foreach (var edge in edgeMap[x][z]) {
        //                Debuger_K.AddLine(pixelPos, nodes[edge.a].positionV3, Color.cyan);
        //                Debuger_K.AddLine(pixelPos, nodes[edge.b].positionV3, Color.cyan);
        //            }
        //        }
        //    }
        //}

        //TEST

        //#region kD tree
        //float minX, minZ, maxX, maxZ;
        //int root;

        //private static TriangulatorNode[] members;
        //private static List<kDTreeBranch> branches = new List<kDTreeBranch>();

        //private static ComparerHolderX holderX = new ComparerHolderX();
        //private static ComparerHolderY holderY = new ComparerHolderY();

        //private struct kDTreeBranch {
        //    public readonly int start, end, depth, branchA, branchB;
        //    public readonly Bounds2D bounds;

        //    public kDTreeBranch(int start, int end, int depth, int branchA, int branchB, float minX, float minY, float maxX, float maxY) {
        //        this.start = start;
        //        this.end = end;
        //        this.depth = depth;
        //        this.branchA = branchA;
        //        this.branchB = branchB;
        //        bounds = new Bounds2D(minX, minY, maxX, maxY);
        //    }
        //}

        //public void BuildTree(NavMeshTemplateCreation template) {        
        //    minX = maxX = nodes[0].x;
        //    minZ = maxZ = nodes[0].z;

        //    members = new TriangulatorNode[nodes.Length];

        //    for (int i = 0; i < nodes.Length; i++) {
        //        members[i] = nodes[i];
        //    }

        //    root = BuildRecursive(0, nodes.Length, 0, minX, minZ, maxX, maxZ);
        //}

        ////return index of branch
        //private int BuildRecursive(int start, int end, int depth, float minX, float minY, float maxX, float maxY) {
        //    float value = 0;
        //    int count = end - start;

        //    if (count < 2) {
        //        branches.Add(new kDTreeBranch(start, end, depth, -1, -1, minX, minY, maxX, maxY));
        //    }
        //    else {
        //        if (depth % 2 == 0) {//true = X, false = Y
        //            Array.Sort(members, start, count, holderX);

        //            for (int i = start; i < end; i++) { value += members[i].x; }
        //            value /= count;

        //            int borderIndex = 0;

        //            for (int i = start; i < end; i++) {
        //                if (members[i].x > value) {
        //                    borderIndex = i;
        //                    break;
        //                }
        //            }

        //            int b1 = BuildRecursive(start, borderIndex, depth + 1, minX, minY, value, maxY);
        //            int b2 = BuildRecursive(borderIndex, end, depth + 1, value, minY, maxX, maxY);
        //            branches.Add(new kDTreeBranch(start, end, depth, b1, b2, minX, minY, maxX, maxY));
        //        }
        //        else {//y
        //            Array.Sort(members, start, count, holderY);

        //            for (int i = start; i < end; i++) { value += members[i].y; }
        //            value /= count;

        //            int borderIndex = 0;

        //            for (int i = start; i < end; i++) {
        //                if (members[i].y > value) {
        //                    borderIndex = i;
        //                    break;
        //                }
        //            }

        //            int b1 = BuildRecursive(start, borderIndex, depth + 1, minX, minY, maxX, value);
        //            int b2 = BuildRecursive(borderIndex, end, depth + 1, minX, value, maxX, maxY);
        //            branches.Add(new kDTreeBranch(start, end, depth, b1, b2, minX, minY, maxX, maxY));
        //        }
        //    }

        //    return branches.Count - 1;
        //}

        //private class ComparerHolderX : IComparer<TriangulatorNode> {
        //    public int Compare(TriangulatorNode nodeLeft, TriangulatorNode nodeRight) {
        //        if (nodeLeft.x == nodeRight.x)
        //            return 0;
        //        if (nodeLeft.x - nodeRight.x > 0)
        //            return 1;
        //        else
        //            return -1;
        //    }
        //}
        //private class ComparerHolderY : IComparer<TriangulatorNode> {
        //    public int Compare(TriangulatorNode nodeLeft, TriangulatorNode nodeRight) {
        //        if (nodeLeft.y == nodeRight.y)
        //            return 0;
        //        if (nodeLeft.y - nodeRight.y > 0)
        //            return 1;
        //        else
        //            return -1;
        //    }
        //}

        //#endregion


        public void CreateAngleVisibilityField() {
            visibilityField = new int[_nodes.Length][];
            List<KeyValuePair<float, int>> values = new List<KeyValuePair<float, int>>();//key = magnitude, value = node id
            
            for (int curNode = 0; curNode < _nodes.Length; curNode++) {
                if (_data[curNode].cross >= 0)
                    continue;

                values.Clear();
                float curNodePos_x = _nodes[curNode].x;
                float curNodePos_z = _nodes[curNode].z;
                float normal_x = _data[curNode].normalX;
                float normal_z = _data[curNode].normalZ;
                float angle = _data[curNode].angle * 0.5f * 0.95f;

                for (int testedNode = 0; testedNode < _nodes.Length; testedNode++) {
                    if (curNode == testedNode)
                        continue;

                    float testNodeDir_x = _nodes[testedNode].x - curNodePos_x;
                    float testNodeDir_z = _nodes[testedNode].z - curNodePos_z;
                    float dot = (normal_x * testNodeDir_x) + (normal_z * testNodeDir_z);

                    if (dot <= 0)
                        continue;

                    float magnitudeTestNode = Mathf.Sqrt((testNodeDir_x * testNodeDir_x) + (testNodeDir_z * testNodeDir_z));
                    //magnitude of normal is 1f anyway            
                    //there is some inacuracy but since angle are offseted by 5% it's anyway lower that that
                    if (Mathf.Acos(dot / magnitudeTestNode) * 180 / Mathf.PI < angle) {
                        //sorting by magnitude
                        int targetIndex;
                        for (targetIndex = 0; targetIndex < values.Count; targetIndex++) {
                            if (values[targetIndex].Key >= magnitudeTestNode)
                                break;
                        }
                        values.Insert(targetIndex, new KeyValuePair<float, int>(magnitudeTestNode, testedNode));
                    }
                }

                int[] v_array = new int[values.Count];
                for (int i = 0; i < values.Count; i++) {
                    v_array[i] = values[i].Value;
                }

                visibilityField[curNode] = v_array;
            }
        }

        public void MakeConnections(NavMeshTemplateCreation template) {
            if (nodes.Length == 0)
                return;

            List<int> nextIterationNodes = new List<int>();
            List<T2Helper> helpers = new List<T2Helper>();

            //GenerateEdgeMap(10, template);
            CreateAngleVisibilityField();

            for (int curNodeIndex = 0; curNodeIndex < _nodes.Length; curNodeIndex++) {
                if (data[curNodeIndex].cross >= 0)
                    continue;

                var curVisible = visibilityField[curNodeIndex];

                for (int i = 0; i < curVisible.Length; i++) {
                    if (IsVisible(curNodeIndex, curVisible[i])) {
                        AddEdge(curNodeIndex, curVisible[i], 0);
                        goto NEXT;
                    }
                }

                nextIterationNodes.Add(curNodeIndex);
                NEXT: { continue; }
            }

            foreach (var nodeIndex in nextIterationNodes) {
                Vector2 nodePos = _nodes[nodeIndex].positionV2;
                Vector2 normal = _data[nodeIndex].normal;
                float validAngle = 180f - (_data[nodeIndex].angle * 0.5f);

                helpers.Clear();
                foreach (var targetNode in Array.FindAll(_nodes, x => Vector2.Angle(normal, x.positionV2 - nodePos) < validAngle)) {
                    if (nodeIndex == targetNode.id)
                        continue;

                    Vector2 targetNodeDirection = (targetNode.positionV2 - nodePos).normalized;
                    helpers.Add(new T2Helper(targetNode.id, Vector2.Angle(normal, targetNodeDirection) * Mathf.Sign(SomeMath.V2Cross(normal, targetNodeDirection))));
                }

                helpers.Sort((x, y) => { return (int)Mathf.Sign(Math.Abs(x.angle) - Math.Abs(y.angle)); });

                //get first visible node on left
                for (int i = 0; i < helpers.Count; i++) {
                    if (helpers[i].angle < 0 && IsVisible(nodeIndex, helpers[i].node)) {
                        AddEdge(nodeIndex, helpers[i].node, 0);
                        break;
                    }
                }

                //get first visible node on right
                for (int i = 0; i < helpers.Count; i++) {
                    if (helpers[i].angle > 0 && IsVisible(nodeIndex, helpers[i].node)) {
                        AddEdge(nodeIndex, helpers[i].node, 0);
                        break;
                    }
                }
            }
        }

        private struct T2Helper {
            public readonly int node;
            public readonly float angle;

            public T2Helper(int node, float angle) {
                this.node = node;
                this.angle = angle;
            }
        }
        
        public int[] AngleVisibilityField(int node) {
            return visibilityField[node];
        }

        public Vector2 NodePosV2(int node) {
            return _nodes[node].positionV2;
        }
        public Vector3 NodePosV3(int node) {
            return _nodes[node].positionV3;
        }

        public TriangulatorNode[] nodes {
            get { return _nodes; }
        }

        public TriangulatorNodeData[] data {
            get { return _data; }
        }

        public List<TriangulatorEdge> edges {
            get { return _edges; }
        }

        public List<int> edgesUsage {
            get { return _edgesUsage; }
        }
        
        void AddEdgeDelegate(int x, int y) {
            if (x < 0)
                x = 0;
            if (x >= EDGE_MAP_SIZE)
                x = EDGE_MAP_SIZE - 1;

            if (y < 0)
                y = 0;
            if (y >= EDGE_MAP_SIZE)
                y = EDGE_MAP_SIZE - 1;

            edgeMap[x * EDGE_MAP_SIZE + y].Add(curAddEdge);

            //Vector3 pixelPos = (new Vector3(x + 0.5f, 0, y + 0.5f) * edgeMapPiselSize) + new Vector3(chunkPos.x, 0, chunkPos.y);
            //Debuger_K.AddDot(pixelPos, Color.cyan);
            //Debuger_K.AddLine(pixelPos, nodes[curAddEdge.a].positionV3, Color.cyan);
            //Debuger_K.AddLine(pixelPos, nodes[curAddEdge.b].positionV3, Color.cyan);
        }

        void IsVisibleDelegate(int x, int y) {
            if (x < 0)
                x = 0;
            if (x >= EDGE_MAP_SIZE)
                x = EDGE_MAP_SIZE - 1;

            if (y < 0)
                y = 0;
            if (y >= EDGE_MAP_SIZE)
                y = EDGE_MAP_SIZE - 1;

            foreach (var val in edgeMap[x * EDGE_MAP_SIZE + y]) {
                isVisibleTemp.Add(val);
            }
        }

        public void AddEdge(int a, int b, int usage, bool checkContains = true) {
            TriangulatorEdge edge = new TriangulatorEdge(a, b, _edges.Count);
            if (checkContains && _edges.Contains(edge))
                return;

            _edges.Add(edge);
            _edgesUsage.Add(usage);

            curAddEdge = edge;
            DDARasterization.DrawLine(
                (nodes[a].x - chunkPos.x),
                (nodes[a].z - chunkPos.y),
                (nodes[b].x - chunkPos.x),
                (nodes[b].z - chunkPos.y),
                edgeMapPiselSize,
                AddEdgeDelegate);

            //Debuger_K.AddLine(nodes[a].positionV3, nodes[b].positionV3, Color.blue, 0.1f);   
        }

        private bool IsVisible(int a, int b) {
            float ax = _nodes[a].x;
            float ay = _nodes[a].z;
            float bx = _nodes[b].x;
            float by = _nodes[b].z;
            float abx = bx - ax;
            float aby = by - ay;
            float sqrDist = SomeMath.SqrDistance(ax, ay, bx, by);
            isVisibleTemp.Clear();

            DDARasterization.DrawLine(ax - chunkPos.x, ay - chunkPos.y, bx - chunkPos.x, by - chunkPos.y, edgeMapPiselSize, IsVisibleDelegate);

            foreach (var edge in isVisibleTemp) {
                if (edge.Contains(a, b))
                    continue;

                float ix, iy;
                if (SomeMath.RayIntersectSegment(ax, ay, abx, aby, _nodes[edge.a].x, _nodes[edge.a].z, _nodes[edge.b].x, _nodes[edge.b].z, out ix, out iy) && SomeMath.SqrDistance(ax, ay, ix, iy) < sqrDist)
                    return false;
            }
            return true;

            //Vector2 aPos = _nodes[a].positionV2;
            //Vector2 bPos = _nodes[b].positionV2;
            //Vector2 dir = bPos - aPos;
            //float curSqrDist = SomeMath.SqrDistance(aPos, bPos);


            //foreach (var edge in edges) {
            //    if (edge.Contains(a, b))
            //        continue;

            //    Vector2 intersection;
            //    if (SomeMath.RayIntersectXZ(
            //        aPos, dir, //from, direction
            //        NodePosV2(edge.a), NodePosV2(edge.b), //a, b
            //        out intersection)
            //        && SomeMath.SqrDistance(aPos, intersection) < curSqrDist) {

            //        //Debuger3.AddRay(new Vector3(intersection.x, 0, intersection.y), Vector3.up, Color.magenta);
            //        //Debuger3.AddLine(ds.NodePosV3(a), new Vector3(intersection.x, 0, intersection.y), Color.magenta);
            //        //Debuger3.AddLine(ds.NodePosV3(b), new Vector3(intersection.x, 0, intersection.y), Color.magenta);
            //        return false;
            //    }
            //}

            //return true;
        }

#if UNITY_EDITOR
        public void DebugIt(XZPosInt pos, AgentProperties properties) {
            foreach (var edge in _edges) {
                Debuger_K.AddTriangulatorDebugLine(pos.x, pos.z, properties, _nodes[edge.a].positionV3, _nodes[edge.b].positionV3, Color.blue);
            }
        }
#endif
    }
}
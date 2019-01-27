using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using K_PathFinder.VectorInt;
using K_PathFinder.Graphs;
using K_PathFinder;
using System;


#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif
namespace K_PathFinder {
    public struct GeneralXZData : IEquatable<GeneralXZData> {
        public readonly XZPosInt gridPosition;
        public readonly AgentProperties properties;

        public GeneralXZData(XZPosInt chunkPos, AgentProperties properties) {
            this.gridPosition = chunkPos;
            this.properties = properties;
            if(properties == null) {
                throw new NullReferenceException("Agent properties can't be null when creating GeneralXZData");
            }
        }
        public GeneralXZData(int x, int z, AgentProperties properties) : this(new XZPosInt(x, z), properties) {}

        public override int GetHashCode() {    
            return gridPosition.GetHashCode() ^ properties.GetHashCode();
        }

        public bool Equals(GeneralXZData other) {
            return other == this;
        }

        public override bool Equals(object obj) {
            if (!(obj is GeneralXZData))
                return false;

            return (GeneralXZData)obj == this;
        }

        public static bool operator ==(GeneralXZData a, GeneralXZData b) {
            return a.properties == b.properties && a.gridPosition == b.gridPosition;
        }

        public static bool operator !=(GeneralXZData a, GeneralXZData b) {
            return !(a == b);
        }
    }   

    [Serializable]
    public struct YRangeInt{
        public int min, max;
        public YRangeInt(int min, int max) {
            this.min = min;
            this.max = max;
        }

        public override int GetHashCode() {
            return min ^ (max * 100);
        }

        public bool Equals(YRangeInt other) {
            return other == this;
        }

        public override bool Equals(object obj) {
            if (!(obj is YRangeInt))
                return false;

            return (YRangeInt)obj == this;
        }

        public static bool operator ==(YRangeInt a, YRangeInt b) {
            return a.min == b.min && a.max == b.max;
        }

        public static bool operator !=(YRangeInt a, YRangeInt b) {
            return !(a == b);
        }
    }
    [Serializable]
    public struct XZPosInt : IEquatable<XZPosInt> {
        public int x, z;
        public XZPosInt(int x, int z) {
            this.x = x;
            this.z = z;
        }

        public override int GetHashCode() {
            return x ^ (z * 100);
        }

        public bool Equals(XZPosInt other) {
            return other == this;
        }

        public override bool Equals(object obj) {
            if (!(obj is XZPosInt))
                return false;

            return (XZPosInt)obj == this;
        }

        public static bool operator ==(XZPosInt a, XZPosInt b) {
            return a.x == b.x && a.z == b.z;
        }

        public static bool operator !=(XZPosInt a, XZPosInt b) {
            return !(a == b);
        }

        public override string ToString() {
            return string.Format("x: {0}, z: {1}", x, z);
        }
    }

    public struct ChunkData {  
        public readonly int x, z, min, max;

        public ChunkData(int x, int z, int min, int max) {
            this.x = x;
            this.z = z;
            this.min = min;
            this.max = max;
        }
        public ChunkData(VectorInt.Vector2Int position, int min, int max) : this(position.x, position.y, min, max) {}

        public ChunkData(XZPosInt pos, YRangeInt range) : this(pos.x, pos.z, range.min, range.max) { }
        public ChunkData(XZPosInt pos) : this(pos.x, pos.z, 0, 0) { }

        public float realX {
            get { return x * PathFinder.gridSize; }
        }
        public float realZ {
            get { return z * PathFinder.gridSize; }
        }

        public XZPosInt xzPos {
            get { return new XZPosInt(x, z); }
        }

        public VectorInt.Vector2Int position {
            get { return new VectorInt.Vector2Int(x, z); }
        }

        public Vector3 realPositionV3 {
            get { return new Vector3(x * PathFinder.gridSize, 0, z * PathFinder.gridSize); }
        }

        public Vector2 realPositionV2 {
            get { return new Vector3(x * PathFinder.gridSize, z * PathFinder.gridSize); }
        }

        public float realMin {
            get { return min * PathFinder.gridSize; }
        }
        public float realMax {
            get { return max * PathFinder.gridSize; }
        }

        public Vector3 boundSize {
            get {
                float gridSize = PathFinder.gridSize;
                float minY = min * gridSize;
                float maxY = max * gridSize + gridSize;
                return new Vector3(gridSize, Math.Abs(minY - maxY), gridSize);
            }
        }
        public Bounds bounds {
            get { return new Bounds(centerV3, boundSize); }
        }

        public Bounds2D bounds2D {
            get { return new Bounds2D(realX + (PathFinder.gridSize * 0.5f), realZ + (PathFinder.gridSize * 0.5f), PathFinder.gridSize * 0.5f); }
        }


        public Vector3 centerV3 {
            get { return new Vector3(realX, realMin, realZ) + (boundSize * 0.5f); }
        }
        /// <summary>
        /// x = x, z = y
        /// </summary>
        public Vector2 centerV2 {
            get { return new Vector2(realX + (PathFinder.gridSize * 0.5f), realZ + (PathFinder.gridSize * 0.5f)); }
        }

        public string positionString {
            get { return "x:" + x + ", z:" + z; }
        }
        public string heightString {
            get { return "min:" + min + ", max:" + max; }
        }
        public string positionHeightString {
            get { return positionString + ", " + heightString; }
        }
    }




//    public class KTree {
//        private const int MAX_LEAF_SIZE = 10;

//        kTreeAgent[] agents_;
//        KTreeNode[] agentTree_;



//        public void BuildAgentTree() {
//            if (agents_ == null || agents_.Length != PathFinder.agents.Count) {
//                agents_ = new kTreeAgent[PathFinder.agents.Count];

//                for (int i = 0; i < agents_.Length; ++i) {
//                    agents_[i] = new kTreeAgent(PathFinder.agents[i]);
//                }

//                agentTree_ = new KTreeNode[2 * agents_.Length];
//            }

//            if (agents_.Length != 0) {
//                buildAgentTreeRecursive(0, agents_.Length, 0);
//            }
//        }

//        private void buildAgentTreeRecursive(int begin, int end, int node) {
//            agentTree_[node].begin_ = begin;
//            agentTree_[node].end_ = end;
//            agentTree_[node].minX_ = agentTree_[node].maxX_ = agents_[begin].x;
//            agentTree_[node].minY_ = agentTree_[node].maxY_ = agents_[begin].y;

//            for (int i = begin + 1; i < end; ++i) {
//                agentTree_[node].maxX_ = Math.Max(agentTree_[node].maxX_, agents_[i].x);
//                agentTree_[node].minX_ = Math.Min(agentTree_[node].minX_, agents_[i].x);
//                agentTree_[node].maxY_ = Math.Max(agentTree_[node].maxY_, agents_[i].y);
//                agentTree_[node].minY_ = Math.Min(agentTree_[node].minY_, agents_[i].y);
//            }

//            if (end - begin > MAX_LEAF_SIZE) {
//                /* No leaf node. */
//                bool isVertical = agentTree_[node].maxX_ - agentTree_[node].minX_ > agentTree_[node].maxY_ - agentTree_[node].minY_;
//                float splitValue = 0.5f * (isVertical ? agentTree_[node].maxX_ + agentTree_[node].minX_ : agentTree_[node].maxY_ + agentTree_[node].minY_);

//                int left = begin;
//                int right = end;

//                while (left < right) {
//                    while (left < right && (isVertical ? agents_[left].x : agents_[left].y) < splitValue) {
//                        ++left;
//                    }

//                    while (right > left && (isVertical ? agents_[right - 1].x : agents_[right - 1].y) >= splitValue) {
//                        --right;
//                    }

//                    if (left < right) {
//                        var tempAgent = agents_[left];
//                        agents_[left] = agents_[right - 1];
//                        agents_[right - 1] = tempAgent;
//                        ++left;
//                        --right;
//                    }
//                }

//                int leftSize = left - begin;

//                if (leftSize == 0) {
//                    ++leftSize;
//                    ++left;
//                    ++right;
//                }

//                agentTree_[node].left_ = node + 1;
//                agentTree_[node].right_ = node + 2 * leftSize;

//                buildAgentTreeRecursive(begin, left, agentTree_[node].left_);
//                buildAgentTreeRecursive(left, end, agentTree_[node].right_);
//            }
//        }
//#if UNITY_EDITOR
//        public void Debug() {
//            foreach (var agent in agents_) {
//                Vector3 pos = agent.agent.positionVector3;
//                foreach (var neighbour in agent.neighbours) {
//                    Vector3 nPos = neighbour.Value.agent.positionVector3;
//                    float dist = neighbour.Key;
//                    Debuger_K.AddLine(pos, nPos, Color.red);
//                }
//            }
//        }
//#endif

//        public void ComputeNeighbours() {
//            foreach (var agent in agents_) {
//                float sqrRange = agent.maxRangeSqr;
//                queryAgentTreeRecursive(agent, ref sqrRange, 0);
//            }
//        }

//        void insertAgentNeighbor(kTreeAgent target, kTreeAgent agent, ref float rangeSq) {
//            if (target == agent)
//                return;

//            float distSq = (target.position - agent.position).sqrMagnitude;
//            var neighbours = target.neighbours;

//            if (distSq < rangeSq) {
//                if (neighbours.Count < target.maxNeighbors) {
//                    neighbours.Add(new KeyValuePair<float, kTreeAgent>(distSq, agent));
//                }

//                int i = neighbours.Count - 1;

//                while (i != 0 && distSq < neighbours[i - 1].Key) {
//                    neighbours[i] = neighbours[i - 1];
//                    --i;
//                }

//                neighbours[i] = new KeyValuePair<float, kTreeAgent>(distSq, agent);

//                if (neighbours.Count == target.maxNeighbors) {
//                    rangeSq = neighbours[neighbours.Count - 1].Key;
//                }
//            }

//        }

//        void queryAgentTreeRecursive(kTreeAgent agent, ref float rangeSq, int node) {
//            if (agentTree_[node].end_ - agentTree_[node].begin_ <= MAX_LEAF_SIZE) {
//                for (int i = agentTree_[node].begin_; i < agentTree_[node].end_; ++i) {
//                    insertAgentNeighbor(agent, agents_[i], ref rangeSq);
//                }
//            }
//            else {
//                float distSqLeft =
//                    SomeMath.Sqr(Math.Max(0.0f, agentTree_[agentTree_[node].left_].minX_ - agent.x)) +
//                    SomeMath.Sqr(Math.Max(0.0f, agent.x - agentTree_[agentTree_[node].left_].maxX_)) +
//                    SomeMath.Sqr(Math.Max(0.0f, agentTree_[agentTree_[node].left_].minY_ - agent.y)) +
//                    SomeMath.Sqr(Math.Max(0.0f, agent.y - agentTree_[agentTree_[node].left_].maxY_));

//                float distSqRight =
//                    SomeMath.Sqr(Math.Max(0.0f, agentTree_[agentTree_[node].right_].minX_ - agent.x)) +
//                    SomeMath.Sqr(Math.Max(0.0f, agent.x - agentTree_[agentTree_[node].right_].maxX_)) +
//                    SomeMath.Sqr(Math.Max(0.0f, agentTree_[agentTree_[node].right_].minY_ - agent.y)) +
//                    SomeMath.Sqr(Math.Max(0.0f, agent.y - agentTree_[agentTree_[node].right_].maxY_));

//                if (distSqLeft < distSqRight) {
//                    if (distSqLeft < rangeSq) {
//                        queryAgentTreeRecursive(agent, ref rangeSq, agentTree_[node].left_);

//                        if (distSqRight < rangeSq) {
//                            queryAgentTreeRecursive(agent, ref rangeSq, agentTree_[node].right_);
//                        }
//                    }
//                }
//                else {
//                    if (distSqRight < rangeSq) {
//                        queryAgentTreeRecursive(agent, ref rangeSq, agentTree_[node].right_);

//                        if (distSqLeft < rangeSq) {
//                            queryAgentTreeRecursive(agent, ref rangeSq, agentTree_[node].left_);
//                        }
//                    }
//                }
//            }
//        }
//    }

//    public struct KTreeNode {
//        internal int begin_;
//        internal int end_;
//        internal int left_;
//        internal int right_;
//        internal float maxX_;
//        internal float maxY_;
//        internal float minX_;
//        internal float minY_;
//    }

//    public class kTreeAgent {
//        public PathFinderAgent agent;
//        public int maxNeighbors;
//        public float maxRangeSqr = 10 * 10;
//        public List<KeyValuePair<float, kTreeAgent>> neighbours = new List<KeyValuePair<float, kTreeAgent>>();
//        public float x, y;

//        public kTreeAgent(PathFinderAgent agent) {
//            this.agent = agent;
//            maxNeighbors = agent.maxNeighbors;
//            var pos = agent.positionVector3;
//            x = pos.x;
//            y = pos.z;
//        }

//        public Vector2 position {
//            get { return new Vector2(x, y); }
//        }


//    }


    //public class Chunk {
    //    /// <summary>
    //    /// x == x
    //    /// y == z
    //    /// </summary>
    //    public Vector2Int position { get; private set; }
    //    public int min, max;

    //    private Dictionary<AgentProperties, Graph> _graphs = new Dictionary<AgentProperties, Graph>();
    //    private Chunk[] _neighbours = new Chunk[4];

    //    public Chunk(Vector2Int position, int min, int max) {
    //        this.position = position;
    //        this.min = min;
    //        this.max = max;
    //    }

    //    public int x {
    //        get { return position.x; }
    //    }
    //    public int z {
    //        get { return position.y; }
    //    }

    //    public float realX {
    //        get { return x * PathFinder.gridSize; ; }
    //    }
    //    public float realZ {
    //        get { return z * PathFinder.gridSize; }
    //    }

    //    public Vector3 realPositionV3 {
    //        get {return new Vector3(realX, 0, realZ);}
    //    }

    //    public Vector2 realPositionV2 {
    //        get { return new Vector3(realX, realZ); }
    //    }

    //    public float realMin {
    //        get { return min * PathFinder.gridSize; ; }
    //    }
    //    public float realMax {
    //        get { return max * PathFinder.gridSize; }
    //    }

    //    public Vector3 boundSize {
    //        get {
    //            float gridSize = PathFinder.gridSize;
    //            float minY = min * gridSize;
    //            float maxY = max * gridSize + gridSize;
    //            return new Vector3(gridSize, Math.Abs(minY - maxY), gridSize);
    //        }
    //    }
    //    public Bounds bounds {
    //        get {return new Bounds(centerV3, boundSize); }
    //    }

    //    public Vector3 centerV3 {
    //        get { return new Vector3(realX, realMin, realZ) + (boundSize * 0.5f); }
    //    }
    //    /// <summary>
    //    /// x = x, z = y
    //    /// </summary>
    //    public Vector2 centerV2 {
    //        get { return new Vector2(realX + (PathFinder.gridSize * 0.5f), realZ + (PathFinder.gridSize * 0.5f)); }
    //    }

    //    public string positionString {
    //        get { return "x:" + x + ", z:" + z; }
    //    }
    //    public string heightString {
    //        get { return "min:" + min + ", max:" + max; }
    //    }
    //    public string positionHeightString {
    //        get { return positionString + ", " + heightString; }
    //    }

    //    public IEnumerable<AgentProperties> properties {
    //        get { return _graphs.Keys; }
    //    }

    //    public bool TryGetGraph(AgentProperties properties, out Graph graph) {
    //        return _graphs.TryGetValue(properties, out graph);
    //    }
    //    public void SetGraph(AgentProperties properties, Graph graph) {
    //        if (_graphs.ContainsKey(properties)) {
    //            _graphs[properties] = graph;
    //        }
    //        else {
    //            _graphs.Add(properties, graph);
    //        }
    //    }

    //    public Chunk GetNeighbour(Directions direction) {
    //        return _neighbours[(int)direction];
    //    }
    //    public bool TryGetNeighbour(Directions direction, AgentProperties properties, out Chunk chunk, out Graph graph) {
    //        chunk = _neighbours[(int)direction];
    //        if(chunk == null) {
    //            graph = null;
    //            return false;
    //        }
    //        else 
    //            return chunk.TryGetGraph(properties, out graph);            
    //    }
    //    public bool TryGetNeighbour(Directions direction, AgentProperties properties, out Graph graph) {
    //        Chunk chunk;
    //        return TryGetNeighbour(direction, properties, out chunk, out graph);
    //    }

    //    public bool TryGetNeighbour(Directions direction, out Chunk result) {
    //        result = _neighbours[(int)direction];
    //        return result != null;
    //    }

    //    public void SetNeighbour(Directions direction, Chunk chunk) {
    //        _neighbours[(int)direction] = chunk;
    //        chunk._neighbours[(int)Enums.Opposite(direction)] = this;
    //    }

    //    public void RemoveGraph(AgentProperties properties) {
    //        _graphs.Remove(properties);
    //    }

    //    public string DescribeNeightbours() {
    //        string nulls = "";
    //        string notNulls = "";
    //        for (int i = 0; i < 4; i++) {
    //            if (_neighbours[i] == null) {
    //                if (nulls != "")
    //                    nulls += ", ";
    //                nulls += ((Directions)i).ToString();
    //            }
    //            else {
    //                if (notNulls != "")
    //                    notNulls += ", ";
    //                notNulls += ((Directions)i).ToString();
    //            }
    //        }
    //        string result = "Connections:\n";
    //        result += ("Exist: " + notNulls + "\n");
    //        result += ("Not exist: " + nulls + "\n");
    //        return result;
    //    }
    //}
}

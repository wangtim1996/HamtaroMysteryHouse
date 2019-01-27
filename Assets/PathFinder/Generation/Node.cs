using UnityEngine;
using K_PathFinder.EdgesNameSpace;
using System.Collections.Generic;
using System.Linq;
using System;
using K_PathFinder.VectorInt ;

namespace K_PathFinder.NodesNameSpace {
    //abstract
    public abstract class NodeAbstract : IGraphPoint {
        private float _x, _y, _z;

        #region constructors
        public NodeAbstract(float x, float y, float z) {
            _x = x;
            _y = y;
            _z = z;
        }
        public NodeAbstract(Vector3 pos) {
            _x = pos.x;
            _y = pos.y;
            _z = pos.z;
        }
        #endregion

        //use it wisley
        public void SetPosition(Vector3 pos) {
            _x = pos.x;
            _y = pos.y;
            _z = pos.z;
        }

        #region IGraphPoint
        public Vector2 positionV2 {
            get { return new Vector2(_x, _z); }
        }
        public Vector3 positionV3 {
            get { return new Vector3(_x, _y, _z); }
        }

        public float x {
            get { return _x; }
        }
        public float y {
            get { return _y; }
        }
        public float z {
            get { return _z; }
        }
        #endregion

        #region position equals XZ
        public bool PositionEqualXZ(Vector3 position, float yError) {
            return PositionEqualXZ(this, position, yError);
        }
        public bool PositionEqualXZ(NodeAbstract node, float yError) {
            return PositionEqualXZ(this, node, yError);
        }

        public static bool PositionEqualXZ(NodeAbstract a, NodeAbstract b, float maxDifY) {
            return a.positionV2 == b.positionV2 && Math.Abs(a.y - b.y) < maxDifY;
        }
        public static bool PositionEqualXZ(NodeAbstract a, Vector3 b, float maxDifY) {
            return a.positionV2 == new Vector2(b.x, b.z) && Math.Abs(a.y - b.y) < maxDifY;
        }
        #endregion

        public override bool Equals(object obj){
            if (obj == null) 
                return false;

            NodeAbstract p = obj as NodeAbstract;
            if (p == null) 
                return false;
    
            return (_x == p._x) && (_y == p._y) && (_z == p._z);
        }

        public override int GetHashCode() {
            return positionV3.GetHashCode();
        }
        public override string ToString() {
            return string.Format("(x:{0}, y:{1}, z:{2})", _x, _y, _z);
        }
    }

    public class NodeTemp : NodeAbstract {
        private int _flags = 0;
        public int mapX, mapZ;

        private Dictionary<VectorInt.Vector2Int, EdgeTemp> edges = new Dictionary<VectorInt.Vector2Int, EdgeTemp>();
        public HashSet<VolumeArea> capturedVolumeAreas = new HashSet<VolumeArea>();

        public NodeTemp(float x, float y, float z, int xGrid, int zGrid) : base(x, y, z) {
            mapX = xGrid;
            mapZ = zGrid;
        }
        public NodeTemp(Vector3 pos, int xGrid, int zGrid) : base(pos.x, pos.y, pos.z) {
            mapX = xGrid;
            mapZ = zGrid;
        }

        public void SetPosition(int mapX, int mapZ, Vector3 pos) {
            this.mapX = mapX;
            this.mapZ = mapZ;
            SetPosition(pos);
        }

        public void SetPosition(VectorInt.Vector2Int mapPos, Vector3 pos) {
            mapX = mapPos.x;
            mapZ = mapPos.y;
            SetPosition(pos);
        }

        #region edge values
        /// <summary>
        /// create new edge if it not exist
        /// </summary>
        public EdgeTemp this[int volume, int hash] {
            get {
                VectorInt.Vector2Int key = new VectorInt.Vector2Int(volume, hash);
                EdgeTemp result;

                if (edges.TryGetValue(key, out result) == false) {
                    result = new EdgeTemp(this, volume, hash);
                    result.SetFlag(EdgeTempFlags.Directed, true);
                    edges[key] = result;
                }

                return result;
            }
            set { edges[new VectorInt.Vector2Int(volume, hash)] = value; }
        }
        #endregion

        #region edge connections
        //contains
        public bool ContainsLayer(int volume) {
            foreach (var p in edges.Keys) {
                if (p.x == volume)
                    return true;
            }
            return false;
        }

        //nodes
        public NodeTemp GetNode(int volume, int hash) {
            return this[volume, hash].connection;
        }
        public EdgeTemp SetNode(int volume, int hash, NodeTemp value) {
            EdgeTemp connection = this[volume, hash];
            connection.connection = value;
            return connection;
        }

        public List<EdgeTemp> GetConnectionsToNode(NodeTemp target) {
            List<EdgeTemp> result = new List<EdgeTemp>();

            foreach (var edge in edges.Values)
                if (edge.connection == target)
                    result.Add(edge);

            return result;
        }

        //** IMPORTANT **//
        //do not use it outside douglas peuker cause who know what consequences will be
        public void RemoveConnention(int volume, int hash) {
            edges.Remove(new VectorInt.Vector2Int(volume, hash));
        }

        /// <summary>
        /// x: layer
        /// y: hash
        /// </summary>
        public IEnumerable<KeyValuePair<VectorInt.Vector2Int, EdgeTemp>> getData {
            get { return edges; }
        }
        public IEnumerable<EdgeTemp> getEdges {
            get { return edges.Values; }
        }
        #endregion

        #region flags  
        public bool GetFlag(NodeTempFlags flag) {
            return (_flags & (int)flag) != 0;
        }
        public void SetFlag(NodeTempFlags flag, bool value) {
            _flags = value ? (_flags | (int)flag) : (_flags & ~(int)flag);
        }
        public bool border {
            get { return (_flags & 960) != 0; }
        }

        #endregion

        public override int GetHashCode() {
            return base.GetHashCode();
        }
        public override string ToString() {
            return base.ToString();
        }
    }

    //graph
    public class Node : NodeAbstract {
        public Node(float x, float y, float z) : base(x, y, z) { }
        public Node(Vector3 pos) : this(pos.x, pos.y, pos.z) { }
    }
}
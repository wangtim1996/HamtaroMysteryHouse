using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using K_PathFinder.NodesNameSpace;
using K_PathFinder.Graphs;

namespace K_PathFinder.EdgesNameSpace {
    //FIX: GraphEdge.getCells are need to be redone

    //abstract
    public abstract class EdgeAbstract {
        protected NodeAbstract _a, _b;
        public EdgeAbstract(NodeAbstract a, NodeAbstract b) {
            _a = a;
            _b = b;
        }

        #region acessors
        public Vector3 centerV3 {
            get { return (aPositionV3 + bPositionV3) * 0.5f; }
        }
        public Vector3 centerV2 {
            get { return (aPositionV2 + bPositionV2) * 0.5f; }
        }

        //a
        public NodeAbstract a {
            get { return _a; }
        }
        public Vector2 aPositionV2 {
            get { return _a.positionV2; }
        }
        public Vector3 aPositionV3 {
            get { return _a.positionV3; }
        }

        //b
        public NodeAbstract b {
            get { return _b; }
        }
        public Vector2 bPositionV2 {
            get { return _b.positionV2; }
        }
        public Vector3 bPositionV3 {
            get { return _b.positionV3; }
        }
        #endregion
        
        public NodeAbstract GetOtherNode(NodeAbstract from) {
            if (_a == from)
                return _b;
            if (_b == from)
                return _a;
            UnityEngine.Debug.LogError("node not presented");
            return null;
        }

        public bool Contains(NodeAbstract node) {
            return ReferenceEquals(_a, node) | ReferenceEquals(_b, node);
        }
        public bool Contains(params NodeAbstract[] node) {
            return node.Contains(_a) || node.Contains(_b);
        }
        public bool Match(NodeAbstract a, NodeAbstract b) {
            return (a == this.a && b == this.b) || (a == this.b && b == this.a);
        }

        #region project
        public static bool Project2(EdgeAbstract edge_A, EdgeAbstract edge_B, float maxDist, Axis axis, out Vector3 intersectionA, out Vector3 intersectionB) {  
            Vector2 a1 = new Vector2(axis == Axis.x ? edge_A.a.x : edge_A.a.z, edge_A.a.y);
            Vector2 a2 = new Vector2(axis == Axis.x ? edge_A.b.x : edge_A.b.z, edge_A.b.y);

            Vector2 b1 = new Vector2(axis == Axis.x ? edge_B.a.x : edge_B.a.z, edge_B.a.y);
            Vector2 b2 = new Vector2(axis == Axis.x ? edge_B.b.x : edge_B.b.z, edge_B.b.y);

            Vector2 i1, i2;
            if(SomeMath.TwoLinesProjectionByX(a1, a2, b1, b2, maxDist, out i1, out i2)) {
                if(axis == Axis.x) {
                    intersectionA = new Vector3(i1.x, i1.y, edge_A.a.z);
                    intersectionB = new Vector3(i2.x, i2.y, edge_A.a.z);
                }
                else{
                    intersectionA = new Vector3(edge_A.a.x, i1.y, i1.x);
                    intersectionB = new Vector3(edge_A.a.x, i2.y, i2.x);
                }

                return true;
            }
            else {
                intersectionA = Vector3.zero;
                intersectionB = Vector3.zero;
                return false;
            }          
        }   
        #endregion
    }

    public class EdgeSimple : EdgeAbstract {
        public EdgeSimple(NodeAbstract a, NodeAbstract b) : base(a, b) {}
    }

    public class EdgeTemp : EdgeAbstract {
        int _flags, _volumeIndex, _hash;
        float[] _numbers = new float[8];

        private enum NormalNumbers : int {
            normalX = 0,
            normalZ = 1,
            leftX = 2,
            leftZ = 3,
            rightX = 4,
            rightZ = 5,
            cross = 6,
            angle = 7
        }

        public EdgeTemp(NodeTemp origin, NodeTemp connection, int layerIndex, int flagIndex) : base(origin, connection) {
            _volumeIndex = layerIndex;
            _hash = flagIndex;
        }
        public EdgeTemp(NodeTemp origin, int layerIndex, int flagIndex) : this(origin, null, layerIndex, flagIndex) { }

        #region acessors
        public int volume {
            get { return _volumeIndex; }
        }

        public int hash {
            get { return _hash; }
        }

        public NodeTemp origin {
            get { return (NodeTemp)base._a; }
            set { base._a = value; }
        }
        public NodeTemp connection {
            get { return (NodeTemp)base._b; }
            set { base._b = value; }
        }

        public Vector3 originPos {
            get { return origin.positionV3; }
        }
        public Vector3 connectionPos {
            get { return connection.positionV3; }
        }


        public float normalX {
            get { return _numbers[(int)NormalNumbers.normalX]; }
            set { _numbers[(int)NormalNumbers.normalX] = value; }
        }
        public float normalZ {
            get { return _numbers[(int)NormalNumbers.normalZ]; }
            set { _numbers[(int)NormalNumbers.normalZ] = value; }
        }
        public Vector2 normal_v2 {
            get { return new Vector2(normalX, normalZ); }
        }

        public float leftX {
            get { return _numbers[(int)NormalNumbers.leftX]; }
            set { _numbers[(int)NormalNumbers.leftX] = value; }
        }
        public float rightX {
            get { return _numbers[(int)NormalNumbers.rightX]; }
            set { _numbers[(int)NormalNumbers.rightX] = value; }
        }
        public float leftZ {
            get { return _numbers[(int)NormalNumbers.leftZ]; }
            set { _numbers[(int)NormalNumbers.leftZ] = value; }
        }
        public float rightZ {
            get { return _numbers[(int)NormalNumbers.rightZ]; }
            set { _numbers[(int)NormalNumbers.rightZ] = value; }
        }
        public float cross {
            get { return _numbers[(int)NormalNumbers.cross]; }
            set { _numbers[(int)NormalNumbers.cross] = value; }
        }
        public float angle {
            get { return _numbers[(int)NormalNumbers.angle]; }
            set { _numbers[(int)NormalNumbers.angle] = value; }
        }
        #endregion

        #region flags
        public bool GetFlag(EdgeTempFlags flag) {
            return (_flags & (int)flag) != 0;
        }
        public void SetFlag(EdgeTempFlags flag, bool value) {
            _flags = value ? (_flags | (int)flag) : (_flags & ~(int)flag);
        }
        #endregion

        public NodeTemp GetOtherNodeTemp(NodeTemp node) {
            return (NodeTemp)base.GetOtherNode(node);
        }
    }

    //public class EdgeGraph : EdgeAbstract {
    //    public int direction = -1; //-1 mean none
    //    public Cell right, left;

    //    public EdgeGraph(Node a, Node b) : base(a, b) {}
    //    public EdgeGraph(Node a, Node b, int direction) : base(a, b) {
    //        this.direction = direction;
    //    }

    //    public bool CellsContains(IEnumerable<Node> input) {
    //        return (right != null && right.AreThisNodes(input)) | (left != null && left.AreThisNodes(input));
    //    }

    //    public Cell GetCell(Cell from) {
    //        if (right == from)
    //            return left;
    //        if (left == from)
    //            return right;
    //        Debug.LogWarning("edge dont contain this cell");
    //        return null;
    //    }

    //    //return cell on position side
    //    public Cell GetCell(Vector3 agentPosition) {
    //        if (right == null)
    //            return left;
    //        if (left == null)
    //            return right;

    //        if (SomeMath.LinePointSideMathf(_a.positionV2, _b.positionV2, new Vector2(agentPosition.x, agentPosition.z)) < 0)
    //            return right;
    //        else
    //            return left;
    //    }

    //    public void GetNodes(Cell from, out Node leftNode, out Node rightNode) {
    //        if (from == right) {
    //            leftNode = (Node)_b;
    //            rightNode = (Node)_a;
    //        }
    //        else if (from == left) {
    //            leftNode = (Node)_a;
    //            rightNode = (Node)_b;
    //        }
    //        else {
    //            leftNode = null;
    //            rightNode = null;
    //            UnityEngine.Debug.LogError("cell not presented in edge");
    //        }
    //    }

    //    public Cell getNotNullCell {
    //        get { return right != null ? right : left; }
    //    }
    //    public bool containsNullCell {
    //        get { return right == null | left == null; }
    //    }
    //}

    //    //graph
    //public class EdgeGraph : EdgeAbstract {
    //    public Cell right { get; private set; }
    //    public Cell left { get; private set; }

    //    public float magnitude { get; private set; }
    //    public float rightCost { get; private set; }
    //    public float leftCost { get; private set; }
    //    private Vector3 _intersection;

    //    public EdgeGraph(Node a, Node b) : base(a, b) {
    //        magnitude = Vector3.Distance(a.positionV3, b.positionV3);
    //    }

    //    public bool CellsContains(IEnumerable<Node> input) {
    //        return (right != null && right.AreThisNodes(input)) | (left != null && left.AreThisNodes(input));
    //    }

    //    //** IMPORTANT **// 
    //    //bool interconnection describe is conection local and inside chunk or if it lead outside chunk
    //    public void SetCell(Cell cell, bool interconnection) {
    //        //quickly define left and right cell     
    //        if (SomeMath.LinePointSideMathf(aPositionV2, bPositionV2, cell.centerV2) > 0) {
    //            //if (right != null)
    //            //    Debug.Log("r" + SomeMath.LinePointSideMathf(aPositionV2, bPositionV2, cell.centerV2));
    //            right = cell;
    //        }
    //        else {
    //            //if (left != null)
    //            //    Debug.Log("l" + SomeMath.LinePointSideMathf(aPositionV2, bPositionV2, cell.centerV2));
    //            left = cell;
    //        }

    //        if (right != null & left != null) {
    //            DefineCost();
    //            right.SetNeighbourConnection(this, left, interconnection);
    //            left.SetNeighbourConnection(this, right, interconnection);     
    //        }
    //    }

    //    public void GetNodes(Cell from, out Node leftNode, out Node rightNode) {
    //        if (from == right) {
    //            leftNode = (Node)_b;
    //            rightNode = (Node)_a;
    //        }
    //        else if (from == left) {
    //            leftNode = (Node)_a;
    //            rightNode = (Node)_b;
    //        }
    //        else {
    //            leftNode = null;
    //            rightNode = null;
    //            Debug.LogError("cell not presented in edge");
    //        }
    //    }

    //    public void GetCost(Cell from, Vector3 fromPos, out float costFrom, out float costTo) {
    //        Vector3 midPoint;
    //        bool isFromRight = from == right; // else its left
    //        Vector3 toPos = isFromRight ? left.centerV3 : right.centerV3;
    //        SomeMath.ClampedRayIntersectXZ(fromPos, toPos - fromPos, _a.positionV3, _b.positionV3, out midPoint);

    //        if (isFromRight) {
    //            costFrom = Vector3.Distance(fromPos, midPoint) * right.area.cost;
    //            costTo = Vector3.Distance(toPos, midPoint) * left.area.cost;
    //        }
    //        else{
    //            costFrom = Vector3.Distance(fromPos, midPoint) * left.area.cost;
    //            costTo = Vector3.Distance(toPos, midPoint) * right.area.cost;
    //        }
    //    }

    //    public void GetCost(Cell from, out float costFrom, out float costTo) {
    //        if (from == right) {
    //            costFrom = rightCost;
    //            costTo = leftCost;
    //            return;
    //        }
    //        if (from == left) {
    //            costFrom = leftCost;
    //            costTo = rightCost;
    //            return;
    //        }

    //        Debug.LogError("cell not presented in edge due cost collection");
    //        costFrom = float.MaxValue;
    //        costTo = float.MaxValue;
    //    }

    //    public Cell GetCell(Vector3 agentPosition) {
    //        if (right == null)
    //            return left;
    //        if (left == null)
    //            return right;

    //        if (SomeMath.LinePointSideMathf(_a.positionV2, _b.positionV2, new Vector2(agentPosition.x, agentPosition.z)) < 0)
    //            return right;
    //        else
    //            return left;
    //    }
    //    public Cell GetCell(Direction direction) {
    //        switch (direction) {
    //            case Direction.Left:
    //            return left;
    //            case Direction.Right:
    //            return right;
    //            default:
    //            return null;
    //        }
    //    }
    //    public Cell GetCell(Cell from) {
    //        if (from == null)
    //            return null;
    //        if (right == from)
    //            return left;
    //        if (left == from)
    //            return right;
    //        Debug.LogWarning("edge dont conrain this node");
    //        return null;
    //    }

    //    public bool Contains(Cell cell) {
    //        return left == cell | right == cell;
    //    }

    //    public bool containNullCell {
    //        get { return left == null || right == null; }
    //    }
    //    public Cell getNotNullCell {
    //        get { return left != null ? left : right; }
    //    }

    //    public List<Cell> getCells {
    //        get {
    //            List<Cell> result = new List<Cell>();
    //            if (right != null)
    //                result.Add(right);
    //            if (left != null)
    //                result.Add(left);
    //            return result;
    //        }
    //    }

    //    //cause ref and out can do properties
    //    public Vector3 intersection {
    //        get { return _intersection; }
    //    }

    //    //also define area cost
    //    private void DefineCost() {
    //        SomeMath.ClampedRayIntersectXZ(right.centerV3, left.centerV3 - right.centerV3, _a.positionV3, _b.positionV3, out _intersection);
    //        rightCost = Vector3.Distance(right.centerV3, _intersection) * right.area.cost;
    //        leftCost = Vector3.Distance(left.centerV3, _intersection) * left.area.cost;
    //    }
    //}

    public class TempEdge {
        public Cell from { get; private set; }
        public Cell to { get; private set; }

        //furthest values
        public Vector3 minus { get; private set; }
        public Vector3 plus { get; private set; }

        //sqr distance of furthest values
        private float minusVal, plusVal;

        //temp values
        private Vector2 fromV2, toV2; //cell centers
        private float intX, intZ; //intersection position

        public TempEdge(Cell from, Cell to, Vector3 a, Vector3 b) {
            this.from = from;
            this.to = to;
            fromV2 = from.centerVector2;
            toV2 = to.centerVector2;

            Vector3 intersectionV3;
            SomeMath.LineLineIntersectXZ(from.centerVector3, to.centerVector3, a, b, out intersectionV3);
            intX = intersectionV3.x;
            intZ = intersectionV3.z;

            minus = plus = a;
            minusVal = plusVal = GetSide(a);
            AddNodePos(b);
        }
        

        public void AddNodePos(Vector3 pos) {
            float val = GetSide(pos);

            if(minusVal > val) {
                minusVal = val;
                minus = pos;
            }
            if(plusVal < val) {
                plusVal = val;
                plus = pos;
            }            
        }

        private float GetSide(Vector3 pos) {
            return SomeMath.SqrDistance(intX, intZ, pos.x, pos.z) * Mathf.Sign(SomeMath.LinePointSideMath(fromV2, toV2, pos.x, pos.z));
        }
    }
}

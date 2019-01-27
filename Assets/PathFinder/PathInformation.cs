using UnityEngine;
using System.Collections.Generic;
using System;
using K_PathFinder.PFTools;
using K_PathFinder.PathGeneration;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif


namespace K_PathFinder {
    public enum MoveState : int {
        crouch = 2,
        walk = 3
    }

    public enum PathResultType {
        Valid = 1,
        InvalidAgentOutsideNavmesh = -1,
        InvalidTargetOutsideNavmesh = -2,
        InvalidNoPath = -3,
        InvalidInternalIssue = -10 //in case there is some errors. tell developer how you get this
    }

    public enum PathNodeType : int {
        Invalid = -1,
        MoveCrouch = 2,
        MoveWalk = 3,
        JumpUpFirstNode = 4,
        JumpUpSecondNode = 5,
        JumpDownFirstNode = 6,
        JumpDownSecondNode = 7
    }

    /// <summary>
    /// class for storing path
    /// it have some additional data:
    /// a) it have resultType which now tell is path valid or not
    /// b) it have owner which tells what agent recieve this path last time
    /// 
    /// Path internaly stored as Queue of structs and list of vectors internaly.
    /// Externaly you can retrive information about next node by using PeekNextNode or DequeueNextNode
    /// Since there stored Path owner it also can return distance to next node and have some other userful stuff
    /// Also in case you want to reuse this object then you can return it to object pool by calling ReturnToPool() this will reduce garbage generation
    /// </summary>
    public class Path : IObjectPoolMember {
        public static ObjectPoolGeneric<Path> pathPool = new ObjectPoolGeneric<Path>();
        public PathResultType pathType = PathResultType.InvalidInternalIssue;
        public PathFinderAgent owner;
        public List<PathNode> pathNodes = new List<PathNode>();
        int _currentIndex = 0;

        public List<CellPathContentAbstract> pathContent = new List<CellPathContentAbstract>();

        public int count {
            get { return pathNodes.Count - _currentIndex; }
        }

        public bool valid {
            get { return pathType > 0 && owner != null; }
        }

        public void Clear() {
            pathType = PathResultType.InvalidInternalIssue;
            pathNodes.Clear();
            pathContent.Clear();
             owner = null;
            _currentIndex = 0;
        }

        public PathNode this[int index] {
            get { return pathNodes[index]; }
        }

        public int currentIndex {
            get { return _currentIndex; }
        }

        /// <summary>
        /// dont use this outside path generation
        /// </summary>
        public void SetCurrentIndex(int value) {
            _currentIndex = value;
        }

        /// <summary>
        /// return current node.
        /// return owner position if no nodes left
        /// </summary>
        public PathNode currentNode {
            get {
                if (_currentIndex > pathNodes.Count)
                    return new PathNode(owner.positionVector3, PathNodeType.Invalid);
                else
                    return pathNodes[_currentIndex];
            }
        }
        public PathNode lastNode {
            get {return pathNodes[pathNodes.Count - 1];}
        }
        public Vector2 lastV2 {
            get { return lastNode.Vector2; }
        }
        public Vector3 lastV3 {
            get { return lastNode.Vector3; }
        }
        public Vector2 currentV2 {
            get { return currentNode.Vector2; }
        }
        public Vector3 currentV3 {
            get { return currentNode.Vector3; }
        }
        public bool MoveToNextNode() {
            if (_currentIndex >= pathNodes.Count) {
                return false;
            }
            else {
                _currentIndex++;
                return true;
            }
        }

        public void ReturnToPool() {
            Path.pathPool.ReturnRented(this);
        }

        #region Add
        public void AddMove(Vector3 position, MoveState state) {
            pathNodes.Add(new PathNode(position, (PathNodeType)(int)state));
        }

        public void AddJumpUp(Vector3 position, Vector3 axis) {
            pathNodes.Add(new PathNode(position, PathNodeType.JumpUpFirstNode));
            pathNodes.Add(new PathNode(axis, PathNodeType.JumpUpSecondNode));
        }
        public void AddJumpDown(Vector3 position, Vector3 landPoint) {
            pathNodes.Add(new PathNode(position, PathNodeType.JumpDownFirstNode));
            pathNodes.Add(new PathNode(position, PathNodeType.JumpDownSecondNode));
        }
        #endregion

        #region junk
        //#region Peek
        ////region where bunch of functions to peek next point

        ///// <summary>
        ///// Peek next node without removing it. Return only node type
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool PeekNextNode(out PathNodeType nodeType) {
        //    if (_currentIndex >= nodes.Count) {
        //        nodeType = PathNodeType.MoveWalk;
        //        return false;
        //    }
        //    else {
        //        nodeType = nodes[_currentIndex].nodeType;
        //        return true;
        //    }                       
        //}

        ///// <summary>
        ///// Peek next node without removing it. Return primary node position.
        ///// For moving it is actual move point. For jumps it is where jump start.
        ///// Position is Vector3.
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool PeekNextNode(out Vector3 primaryNodePosition) {
        //    if (_currentIndex >= nodes.Count) {
        //        primaryNodePosition = new Vector3();
        //        return false;
        //    }
        //    else {
        //        primaryNodePosition = vectors[nodes[_currentIndex].indexFirst];
        //        return true;
        //    }
        //}

        ///// <summary>
        ///// Peek next node without removing it. Return primary node position.
        ///// For moving it is actual move point. For jumps it is where jump start.
        ///// Position is Vector2 in top view. it is XZ of Vector3 result
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool PeekNextNode(out Vector2 primaryNodePosition) {
        //    if (_currentIndex >= nodes.Count) {
        //        primaryNodePosition = new Vector3();
        //        return false;
        //    }
        //    else {
        //        primaryNodePosition = ToVector2(vectors[nodes[_currentIndex].indexFirst]);
        //        return true;
        //    }
        //}

        ///// <summary>
        ///// Peek next node without removing it. Return node type and primary node position.
        ///// For moving it is actual move point. For jumps it is where jump start.
        ///// Position is Vector3.
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool PeekNextNode(out PathNodeType nodeType, out Vector3 primaryNodePosition) {
        //    if (_currentIndex >= nodes.Count) {
        //        nodeType = PathNodeType.MoveWalk;
        //        primaryNodePosition = new Vector3();
        //        return false;
        //    }
        //    else {
        //        PathNode node = nodes[_currentIndex];
        //        primaryNodePosition = vectors[node.indexFirst];
        //        nodeType = node.nodeType;
        //        return true;
        //    }
        //}

        ///// <summary>
        ///// Peek next node without removing it. Return node type and primary node position.
        ///// For moving it is actual move point. For jumps it is where jump start.
        ///// Position is Vector2 in top view. it is XZ of Vector3 result
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool PeekNextNode(out PathNodeType nodeType, out Vector2 primaryNodePosition) {
        //    if (_currentIndex >= nodes.Count) {
        //        nodeType = PathNodeType.MoveWalk;
        //        primaryNodePosition = new Vector3();
        //        return false;
        //    }
        //    else {
        //        PathNode node = nodes[_currentIndex];
        //        primaryNodePosition = ToVector2(vectors[node.indexFirst]);
        //        nodeType = node.nodeType;
        //        return true;
        //    }
        //}

        ///// <summary>
        ///// Peek next node without removing it. Return node type, primary and secondary node position
        ///// For moving primary position is where point is. And secondary is just (0,0,0).
        ///// For jumps primary node is where jump start and secondary is where jump end
        ///// Position is Vector3.
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool PeekNextNode(out PathNodeType nodeType, out Vector2 primaryNodePosition, out Vector3 secondaryNodePosition) {
        //    if (_currentIndex >= nodes.Count) {
        //        nodeType = PathNodeType.MoveWalk;
        //        primaryNodePosition = new Vector3();
        //        secondaryNodePosition = new Vector3();
        //        return false;
        //    }
        //    else {
        //        PathNode node = nodes[_currentIndex];
        //        primaryNodePosition = vectors[node.indexFirst];
        //        secondaryNodePosition = vectors[node.indexSecond];
        //        nodeType = node.nodeType;
        //        return true;
        //    }
        //}
        //#endregion

        //#region Dequeue
        ////region where bunch of functions to dequeue next point

        ///// <summary>
        ///// Remove next node.
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool DequeueNextNode() {
        //    if (_currentIndex >= nodes.Count) {
        //        return false;
        //    }
        //    else {
        //        _currentIndex++;
        //        return true;
        //    }
        //}

        ///// <summary>
        ///// Remove next node and return it information. Return only node type. (not very userful but whatever. it might be)
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool DequeueNextNode(out PathNodeType nodeType) {
        //    bool result = PeekNextNode(out nodeType);
        //    if (result) _currentIndex++;
        //    return result;
        //}

        ///// <summary>
        ///// Remove next node and return it information. Return primary node position.
        ///// For moving it is actual move point. For jumps it is where jump start.
        ///// Position is Vector3.
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool DequeueNextNode(out Vector3 primaryNodePosition) {
        //    bool result = PeekNextNode(out primaryNodePosition);
        //    if (result) _currentIndex++;
        //    return result;
        //}

        ///// <summary>
        ///// Remove next node and return it information. Return primary node position.
        ///// For moving it is actual move point. For jumps it is where jump start.
        ///// Position is Vector2 in top view. it is XZ of Vector3 result
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool DequeueNextNode(out Vector2 primaryNodePosition) {
        //    bool result = PeekNextNode(out primaryNodePosition);
        //    if (result) _currentIndex++;
        //    return result;
        //}

        ///// <summary>
        ///// Remove next node and return it information. Return node type and primary node position.
        ///// For moving it is actual move point. For jumps it is where jump start.
        ///// Position is Vector3.
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool DequeueNextNode(out PathNodeType nodeType, out Vector3 primaryNodePosition) {
        //    bool result = PeekNextNode(out nodeType, out primaryNodePosition);
        //    if (result) _currentIndex++;
        //    return result;
        //}

        ///// <summary>
        ///// Remove next node and return it information. Return node type and primary node position.
        ///// For moving it is actual move point. For jumps it is where jump start.
        ///// Position is Vector2 in top view. it is XZ of Vector3 result
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool DequeueNextNode(out PathNodeType nodeType, out Vector2 primaryNodePosition) {
        //    bool result = PeekNextNode(out nodeType, out primaryNodePosition);
        //    if (result) _currentIndex++;
        //    return result;
        //}

        ///// <summary>
        ///// Remove next node and return it information. Return node type, primary and secondary node position
        ///// For moving primary position is where point is. And secondary is just (0,0,0).
        ///// For jumps primary node is where jump start and secondary is where jump end
        ///// Position is Vector3.
        ///// return true if next node exist at all and false if not
        ///// </summary>
        //public bool DequeueNextNode(out PathNodeType nodeType, out Vector2 primaryNodePosition, out Vector3 secondaryNodePosition) {
        //    bool result = PeekNextNode(out nodeType, out primaryNodePosition, out secondaryNodePosition);
        //    if (result) _currentIndex++;
        //    return result;
        //}
        //#endregion
        //#region distance
        ////region where bunch of functions to return distance to next point

        ///// <summary>
        ///// Return distance from specific point to next node 
        ///// return true if next node exist at all
        ///// </summary>
        //public bool ReturnNextNodeDistance3D(Vector3 point, out float distance) {
        //    if (nodes.Count == 0) {
        //        distance = 0f;
        //        return false;
        //    }
        //    else {
        //        distance = Vector3.Distance(point, vectors[nodes[_currentIndex].indexFirst]);
        //        return true;
        //    }
        //}

        ///// <summary>
        ///// Return 2D distance from specific point to next node 
        ///// return true if next node exist at all
        ///// XZ axis from node taken to measure distance so it return top view distance
        ///// </summary>
        //public bool ReturnNextNodeDistance2D(Vector2 point, out float distance) {
        //    if (nodes.Count == 0) {
        //        distance = 0f;
        //        return false;
        //    }
        //    else {
        //        distance = Vector2.Distance(point, ToVector2(vectors[nodes[_currentIndex].indexFirst]));
        //        return true;
        //    }

        //}

        ///// <summary>
        ///// Return squared distance from specific point to next node 
        ///// return true if next node exist at all
        ///// squared distance is distance * distance. so if you want actual distance you should call Math.Sqrt on result
        ///// it is slightly faster than returning normal distance so if you want to compare some distances this is way to go
        ///// </summary>
        //public bool ReturnNextNodeSqrDistance3D(Vector3 point, out float distance) {
        //    if (nodes.Count == 0) {
        //        distance = 0f;
        //        return false;
        //    }
        //    else {
        //        distance = SomeMath.SqrDistance(point, vectors[nodes[_currentIndex].indexFirst]);
        //        return true;
        //    }
        //}

        ///// <summary>
        ///// Return squared 2D distance from specific point to next node 
        ///// return true if next node exist at all
        ///// XZ axis from node taken to measure distance so it return top view distance
        ///// it is slightly faster than returning normal distance so if you want to compare some distances this is way to go
        ///// </summary>
        //public bool ReturnNextNodeSqrDistance2D(Vector2 point, out float distance) {
        //    if (nodes.Count == 0) {
        //        distance = 0f;
        //        return false;
        //    }
        //    else {
        //        distance = SomeMath.SqrDistance(point, ToVector2(vectors[nodes[_currentIndex].indexFirst]));
        //        return true;
        //    }
        //}

        ///// <summary>
        ///// Return distance from owner position
        ///// return true if next node exist at all
        ///// </summary>
        //public bool ReturnNextNodeDistance3D(out float distance) {
        //    return ReturnNextNodeDistance3D(owner.positionVector3, out distance);
        //}

        ///// <summary>
        ///// Return 2D distance from owner position
        ///// return true if next node exist at all
        ///// XZ axis from node taken to measure distance so it return top view distance
        ///// </summary>
        //public bool ReturnNextNodeDistance2D(out float distance) {
        //    return ReturnNextNodeDistance2D(ToVector2(owner.positionVector3), out distance);
        //}

        ///// <summary>
        ///// Return squared distance from specific point to next node 
        ///// return true if next node exist at all
        ///// squared distance is distance * distance. so if you want actual distance you should call Math.Sqrt on result
        ///// it is slightly faster than returning normal distance so if you want to compare some distances this is way to go
        ///// </summary>
        //public bool ReturnNextNodeSqrDistance3D(out float distance) {
        //    return ReturnNextNodeSqrDistance3D(owner.positionVector3, out distance);
        //}

        ///// <summary>
        ///// Return squared 2D distance from specific point to next node 
        ///// return true if next node exist at all
        ///// XZ axis from node taken to measure distance so it return top view distance
        ///// it is slightly faster than returning normal distance so if you want to compare some distances this is way to go
        ///// </summary>
        //public bool ReturnNextNodeSqrDistance2D(out float distance) {
        //    return ReturnNextNodeSqrDistance2D(ToVector2(owner.positionVector3), out distance);
        //}
        //#endregion
        #endregion



        /// <summary>
        /// iterate through nodes and return if there is node other than move node. movable mean it's only when you move. not jump. so you can tell if agent about to jump
        /// </summary>
        public bool MovableDistanceLesserThan(float targetDistance, out float distance, out PathNode node, out bool reachLastPoint) {
            if (valid == false) {
                Debug.LogWarning("path are invalid");
                node = new PathNode(0, 0, 0, PathNodeType.Invalid);
                distance = 0;
                reachLastPoint = true;
                return false;
            }

            if(pathNodes.Count == _currentIndex) {
                node = new PathNode(0, 0, 0, PathNodeType.Invalid);
                distance = 0;
                reachLastPoint = true;
                return true;
            }

            int remainNodes = count;
            Vector3 ownerPos = owner.positionVector3;

            node = pathNodes[currentIndex];
            distance = SomeMath.Distance(ownerPos, node.Vector3);

            if ((int)node.type >= 4) {//4, 5, 6, 7 are jumps right now
                reachLastPoint = remainNodes == 1;
                return distance < targetDistance;
            }

            if(remainNodes == 1) {
                reachLastPoint = true;
                return distance < targetDistance;
            }

            for (int i = currentIndex + 1; i < pathNodes.Count; i++) {
                node = pathNodes[i];
                distance += SomeMath.Distance(node.Vector3, pathNodes[i - 1].Vector3);
                node = pathNodes[i];
            
                if (distance > targetDistance) {    
                    reachLastPoint = i == pathNodes.Count - 1;
                    return false;
                }

                if ((int)node.type >= 4) {
                    reachLastPoint = i == pathNodes.Count - 1;
                    return distance < targetDistance;
                }
            }

            node = pathNodes[pathNodes.Count - 1];
            reachLastPoint = true;
            return true;
        }

        public bool MovableDistanceLesserThan(float targetDistance, out float distance, out bool reachLastPoint) {
            PathNode node;
            return MovableDistanceLesserThan(targetDistance, out distance, out node, out reachLastPoint);
        }
        public bool MovableDistanceLesserThan(float targetDistance, out bool reachLastPoint) {
            float distance;
            PathNode node;
            return MovableDistanceLesserThan(targetDistance, out distance, out node, out reachLastPoint);
        }
        public bool MovableDistanceLesserThan(float targetDistance, out PathNode node) {
            float distance;
            bool reachLastPoint;
            return MovableDistanceLesserThan(targetDistance, out distance, out node, out reachLastPoint);
        }
        public bool MovableDistanceLesserThan(float targetDistance, out float distance) {
            PathNode node;
            bool reachLastPoint;
            return MovableDistanceLesserThan(targetDistance, out distance, out node, out reachLastPoint);
        }
        public bool MovableDistanceLesserThan(float targetDistance) {
            float distance;
            PathNode node;
            bool reachLastPoint;
            return MovableDistanceLesserThan(targetDistance, out distance, out node, out reachLastPoint);
        }

        private Vector2 ToVector2(Vector3 vector) {
            return new Vector2(vector.x, vector.z);
        }

        public override string ToString() {
            return string.Format("nodes {0}, index {1}", pathNodes.Count, _currentIndex);
        }

        public Path Copy() {
            Path result = pathPool.Rent();            
            for (int i = 0; i < pathNodes.Count; i++) {
                result.pathNodes.Add(pathNodes[i]);
            }
            result._currentIndex = _currentIndex;
            return result;
        }

#if UNITY_EDITOR
        public void DebugByDebuger() {
            for (int i = 0; i < pathNodes.Count; i++) {
                Debuger_K.AddDot(pathNodes[i], Color.red);
                Debuger_K.AddLabelFormat(pathNodes[i], "{0} {1}", i, pathNodes[i]);
            }
            for (int i = 0; i < pathNodes.Count - 1; i++) {
                Debuger_K.AddLine(pathNodes[i], pathNodes[i + 1], Color.red);
            }
        }
#endif
    }

public struct PathNode {
        public readonly float x, y, z;
        public readonly PathNodeType type;

        public PathNode(float X, float Y, float Z, PathNodeType Type) {
            x = X;
            y = Y;
            z = Z;
            type = Type;
        }

        public PathNode(Vector3 pos, PathNodeType Type) : this(pos.x, pos.y, pos.z, Type) { }

        public Vector3 Vector3 {
            get { return new Vector3(x, y, z); }
        }
        public Vector2 Vector2 {
            get { return new Vector2(x, z); }
        }

        public static implicit operator Vector3(PathNode obj) {
            return obj.Vector3;
        }
        public static implicit operator Vector2(PathNode obj) {
            return obj.Vector2;
        }

        public override string ToString() {
            return type.ToString();
        }
    }

}
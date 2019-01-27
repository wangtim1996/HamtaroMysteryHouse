using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using K_PathFinder.Graphs;
using K_PathFinder.CoverNamespace;
//using K_PathFinder.RVOPF;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder {
    public class PathFinderAgent : MonoBehaviour {
        //general values
        public AgentProperties properties;

        //public flags
        [HideInInspector]
        public bool ignoreCrouchCost = false;      

        //private flags
        private bool _canRecieveGoals = true;
        private bool _canRecieveResults = true;

        //recived values
        private Path _path = null;
        private IEnumerable<BattleGridPoint> _battleGrid = null;
        private List<NodeCoverPoint> _covers = null;    

        //on recive delegates not thread safe
        private Action<Path> _recievePathDelegate_NTS = null;
        private Action<IEnumerable<BattleGridPoint>> _recieveBattleGridDelegate_NTS = null;
        private Action<IEnumerable<NodeCoverPoint>> _recieveCoverPointDelegate_NTS = null;

        //on recive delegates thread safe
        private Action<Path> _recievePathDelegate_TS = null;
        private Action<IEnumerable<BattleGridPoint>> _recieveBattleGridDelegate_TS = null;
        private Action<IEnumerable<NodeCoverPoint>> _recieveCoverPointDelegate_TS = null;
        private bool _recievePathUsed = true, _recieveBattleGridUsed = true, _recieveCoverPointUsed = true;

        //general Pathfinder information
        private Vector3 _position;//since we cant call transform.position in thread we store it here
        //private Vector3? destination = null;

        public bool updateNavmeshPosition = false; //if true then it position will be updated into 3 values below
        private Cell _agentCell;                   //use acessor below to use this value
        private Vector3 _nearestNavmeshPoint;      //use acessor below to use this value
        private bool _outsideNavmesh;              //use acessor below to use this value

        public bool updateNeighbourAgents = false;                                  //if true then neighbours information will be updated into two values below
        public List<PathFinderAgent> neighbourAgents = new List<PathFinderAgent>(); //not threadsafe 
        public List<float> neighbourSqrDistances = new List<float>();               //not threadsafe

        //velocity obstacle variables
        public bool velocityObstacle = false;
        public float maxAgentVelocity = 2f;
        [Range(0f, 1f)] public float avoidanceResponsibility = 0.5f; // bigger number - move inclined agent to evade
        [Range(0.01f, 0.99f)] public float careDistance = 0.75f; //how fast object should react to obstacle. 0 - instant, 1 - only if it collide/ range is in 0.01f - 0.99f
        public int maxNeighbors = 10; //The default maximum number of other agents a new agent takes into account in the navigation.
        public float maxNeighbourDistance = 10f; //how far agent should check if object are suitable for avoidance
        
        //dead lock variables
        public bool useDeadLockFailsafe = false;
        public float deadLockVelocityThreshold = 0.025f;
        public float deadLockFailsafeVelocity = 0.3f;
        public float deadLockFailsafeTime = 2f;//in seconds
        public DateTime deadLockTriggeredTime; //last deadLock
        private Vector2 _velocity, _preferableVelocity, _safeVelocity;

        //one side evasion
        public bool preferOneSideEvasion = false;
        public float preferOneSideEvasionOffset = 2f;

        //some automation
        public bool queueNavmeshAround = false;
        public float queueNavmeshAroundCooldown = 5f;
        public float queueNavmeshAroundSize = 20f;
        public bool queueNavmeshToMoveDirection = false;
        public bool queueGetPathAgain = false;
        public float queueGetPathAgainCooldown = 0.25f;

        //There was check in PathFinder that check if agent already order some work. This prove to be bad design.
        //Cause we need to put agent to sort of HashSet. on every iteration check if this agent added to this HashSet. 
        //In case agent already added - ignore current request. No. Just no. Here 3 bools that represent this check.
        //Just dont mess with them or you might recieve results from past when they dont need already
        //if true = can send this work
        [NonSerialized]public bool canSendPathRequest = true;
        [NonSerialized]public bool canSendCoverRequest = true;
        [NonSerialized]public bool canSendGridRequest = true;   

        public virtual Vector3 positionVector3 {
            get { return _position; }
        }
        public Vector2 positionVector2 {
            get { return ToVector2(positionVector3); }
        }   

        public float radius {
            get { return properties.radius; }
        }
        
        public Vector2 velocity {
            get { lock (this) return _velocity; }
            set { lock (this) _velocity = value; }
        }
        public Vector2 preferableVelocity {
            get { lock (this)return _preferableVelocity; }
            set { lock (this)_preferableVelocity = value; }
        }
        public Vector2 safeVelocity {
            get { lock(this)return _safeVelocity; }
            set { lock (this)_safeVelocity = value; }
        }   

        //on and off
        public void On() {
            if (properties == null) {
                Debug.LogErrorFormat("properties == null on {0}", gameObject.name);
                return;
            }

            _canRecieveResults = true;
            _canRecieveGoals = true; 
        }
        public void Off() {
            _canRecieveResults = false;
            _canRecieveGoals = false;   
        }

        //more precise on and off
        public void StartRecieveGoals() {
            _canRecieveGoals = true;
        }
        public void StartRecieveResults() {
            _canRecieveResults = true;
        }
        public void StopRecieveGoals() {
            _canRecieveGoals = false;
        }
        public void StopRecieveResults() {
            _canRecieveResults = false;
        }

        //some automation

        IEnumerator QueueNavmeshAroundAgent() {
            while (queueNavmeshAround) {
                if (properties != null)
                    PathFinder.QueueGraph(
                        new Bounds(positionVector3, new Vector3(queueNavmeshAroundSize, 0, queueNavmeshAroundSize)),
                        properties);
                yield return new WaitForSeconds(queueNavmeshAroundCooldown);
            }   
        }



        IEnumerator QueueGetPathAgain() {
            while (queueNavmeshAround) {
                if (properties != null)
                    PathFinder.QueueGraph(
                        new Bounds(positionVector3, new Vector3(queueNavmeshAroundSize, 0, queueNavmeshAroundSize)),
                        properties);
                yield return new WaitForSeconds(queueNavmeshAroundCooldown);
            }
        }


        //acessors
        public Path path {
            get {
                lock (this)
                    return _path;
            }
        }

        public bool haveNextNode {
            get {
                lock (this) {
                    if (path == null)
                        return false;
                    else
                        return path.count > 0;
                }
            }
        }

        public PathNode nextNode {
            get { return path.currentNode; }
        }
        public Vector3 nextNodeDirectionVector3 {
            get {return path.currentNode - positionVector3; } //implicit convertation to Vector3
        }
        public Vector2 nextNodeDirectionVector2 {
            get {return path.currentNode - positionVector2; } //implicit convertation to Vector2
        }

        public void RemoveNextNode() {
            lock (this) {
                if (path == null)
                    return;

                path.MoveToNextNode();
            }
        }

        /// <summary>
        /// return true if node were removed
        /// sqrDistance is normal distance * distance to simplify math
        /// distance measured by Vector3
        /// </summary>
        public bool RemoveNextNodeIfCloserSqr(float sqrDistance) {
            lock (this) {
                if (path != null) Debug.Log(path.pathNodes.Count);

                if (path == null || path.count == 0)
                    return false;

                Vector3 agentPos = positionVector3;
                PathNode node = path.currentNode;
                if(SomeMath.SqrDistance(node.x, node.y, node.z, agentPos.x, agentPos.y, agentPos.z) < sqrDistance) {
                    Debug.Log("Path index moved");
                    Debug.DrawRay(node.Vector3, Vector3.up, Color.red, 1f);
                    path.MoveToNextNode();   
                    return true;
                }
                else {
                    return false;
                }
            } 
        }
        /// <summary>
        /// return true if node were removed
        /// sqrDistance is normal distance * distance to simplify math
        /// distance measured by Vector2
        /// </summary>
        public bool RemoveNextNodeIfCloserSqrVector2(float sqrDistance) {
            lock (this) {
                //if (path != null)
                //    path.DebugByDebuger();

                if (path == null || path.count == 0)
                    return false;
                
                Vector3 agentPos = positionVector3;
                PathNode node = path.currentNode;

                //Debug.Log(SomeMath.SqrDistance(node.x, node.z, agentPos.x, agentPos.z));

                if (SomeMath.SqrDistance(node.x, node.z, agentPos.x, agentPos.z) < sqrDistance) {
                    path.MoveToNextNode();              
                    return true;
                }
                else {
                    return false;
                }
            }
        }
        /// <summary>
        /// return true if node were removed
        /// distance measured by Vector3
        /// </summary>
        public bool RemoveNextNodeIfCloser(float distance) {
            return RemoveNextNodeIfCloserSqr(distance * distance);
        }
        /// <summary>
        /// return true if node were removed
        /// distance measured by Vector2
        /// </summary>
        public bool RemoveNextNodeIfCloserVector2(float distance) {
            return RemoveNextNodeIfCloserSqrVector2(distance * distance);
        }
        /// <summary>
        /// remove next node if it closer than agent radius
        /// return true if node were removed
        /// distance measured by Vector3
        /// </summary>
        public bool RemoveNextNodeIfCloserThanRadius() {
            return RemoveNextNodeIfCloserSqr(radius * radius);
        }
        /// <summary>
        /// remove next node if it closer than agent radius
        /// return true if node were removed
        /// distance measured by Vector2
        /// </summary>
        public bool RemoveNextNodeIfCloserThanRadiusVector2() {
            return RemoveNextNodeIfCloserSqrVector2(radius * radius);
        }

        public List<NodeCoverPoint> covers {
            get {
                lock (this)
                    return _covers;
            }
        }
        public IEnumerable<BattleGridPoint> battleGrid {
            get {
                lock (this)
                    return _battleGrid;
            }
        }

        void Start() {
            _position = transform.position;
        }

        //execute threadsafe delegates and update agent position
        public void Update() {
            _position = transform.position;

            if (!_recievePathUsed && _recievePathDelegate_TS != null) {
                _recievePathUsed = true;
                _recievePathDelegate_TS.Invoke(_path);
            }

            if (!_recieveBattleGridUsed && _recieveBattleGridDelegate_TS != null) {
                _recieveBattleGridUsed = true;
                _recieveBattleGridDelegate_TS.Invoke(_battleGrid);
            }

            if (!_recieveCoverPointUsed && _recieveCoverPointDelegate_TS != null) {
                _recieveCoverPointUsed = true;
                _recieveCoverPointDelegate_TS.Invoke(_covers);
            }

            //Debug.DrawLine(positionVector3, nearestNavmeshPoint, Color.magenta);
            //Cell cell = nearestCell;
            //if (cell != null) {
            //    Debug.DrawLine(positionVector3, cell.centerVector3, Color.magenta);
            //}
        }
      
        void OnEnable() {
            PathFinder.RegisterAgent(this); //add itself to agents pool 
        }

        void OnDestroy() {
            PathFinder.UnregisterAgent(this); //remove itself from agents pool
        }

        //general PathFinder information accessors
        public Cell nearestCell {
            get { lock (this) return _agentCell; }
            set { lock (this) _agentCell = value; }
        }
        public Vector3 nearestNavmeshPoint {
            get { lock (this) return _nearestNavmeshPoint; }
            set { lock (this) _nearestNavmeshPoint = value; }
        }
        public bool outsideNavmesh {
            get { lock (this) return _outsideNavmesh; }
            set { lock (this) _outsideNavmesh = value; }
        }

        //recieve delegates
        public void SetRecievePathDelegate(Action<Path> pathDelegate, AgentDelegateMode mode = AgentDelegateMode.NotThreadSafe) {
            switch (mode) {
                case AgentDelegateMode.ThreadSafe:
                    _recievePathDelegate_TS = pathDelegate;
                    break;
                case AgentDelegateMode.NotThreadSafe:
                    _recievePathDelegate_NTS = pathDelegate;
                    break;
            }  
        }
        public void RemoveRecievePathDelegate(AgentDelegateMode mode = AgentDelegateMode.NotThreadSafe) {
            switch (mode) {
                case AgentDelegateMode.ThreadSafe:
                    _recievePathDelegate_TS = null;
                    _recievePathUsed = true;
                    break;
                case AgentDelegateMode.NotThreadSafe:
                    _recievePathDelegate_NTS = null;
                    break;
            }
        }

        public void SetRecieveBattleGridDelegate(Action<IEnumerable<BattleGridPoint>> gridDelegate, AgentDelegateMode mode = AgentDelegateMode.NotThreadSafe) {
            switch (mode) {
                case AgentDelegateMode.ThreadSafe:
                    _recieveBattleGridDelegate_TS = gridDelegate;
                    _recieveBattleGridUsed = true;
                    break;
                case AgentDelegateMode.NotThreadSafe:
                    _recieveBattleGridDelegate_NTS = gridDelegate;
                    break;
            }      
        }
        public void RemoveRecieveBattleGridDelegate(AgentDelegateMode mode = AgentDelegateMode.NotThreadSafe) {
            switch (mode) {
                case AgentDelegateMode.ThreadSafe:
                    _recieveBattleGridDelegate_TS = null;
                    break;
                case AgentDelegateMode.NotThreadSafe:
                    _recieveBattleGridDelegate_NTS = null;
                    break;
            }
        }

        public void SetRecieveCoverDelegate(Action<IEnumerable<NodeCoverPoint>> coverDelegate, AgentDelegateMode mode = AgentDelegateMode.NotThreadSafe) {
            switch (mode) {
                case AgentDelegateMode.ThreadSafe:
                    _recieveCoverPointDelegate_TS = coverDelegate;
                    break;
                case AgentDelegateMode.NotThreadSafe:
                    _recieveCoverPointDelegate_NTS = coverDelegate;
                    break;
            }
        }
        public void RemoveRecieveCoverDelegate(AgentDelegateMode mode = AgentDelegateMode.NotThreadSafe) {
            switch (mode) {
                case AgentDelegateMode.ThreadSafe:
                    _recieveCoverPointDelegate_TS = null;
                    _recieveCoverPointUsed = true;
                    break;
                case AgentDelegateMode.NotThreadSafe:
                    _recieveCoverPointDelegate_NTS = null;
                    break;
            }
        }

        //set goals
        //threadsafe
        public void SetGoalMoveHere(Vector3 start, Vector3 destination, bool snapToNavMesh = false, bool applyRaycast = false) {
            if (_canRecieveGoals == false)
                return;

            if (!properties.doNavMesh) {
                Debug.LogWarning("you trying to get path when you dont even set properties to generate NavMesh");
                return;
            }

            if(properties == null) {
                Debug.LogError("Agent dont have assigned Properties");
                return;
            }

            lock (this)
                if (canSendPathRequest == false)
                    return;

            //this.destination = destination;
            PathFinder.GetPath(this, destination, start, snapToNavMesh, null, applyRaycast);
        }
        public void SetGoalMoveHere(Vector3 destination, bool snapToNavMesh = true, bool applyRaycast = false) {
            SetGoalMoveHere(positionVector3, destination, snapToNavMesh, applyRaycast);
        }

        public void SetGoalFindNearestArea(Area target, float maxSearchCost, bool snapToNavMesh = true, bool applyRaycast = false) {
            if (_canRecieveGoals == false)
                return;

            if (!properties.doNavMesh) {
                Debug.LogWarning("you trying to get path when you dont even set properties to generate NavMesh");
                return;
            }

            if (properties == null) {
                Debug.LogError("Agent dont have assigned Properties");
                return;
            }

            lock (this)
                if (canSendPathRequest == false)
                    return;

            PathFinder.GetPathAreaSearch(this, target, true, maxSearchCost, positionVector3, snapToNavMesh, null, applyRaycast);
        }
        public void SetGoalFindNearestArea(int globalDictionaryID, float maxSearchCost, bool snapToNavMesh = true, bool applyRaycast = false) {
            SetGoalFindNearestArea(PathFinder.GetArea(globalDictionaryID), maxSearchCost, snapToNavMesh, applyRaycast);
        }

        public void SetGoalGetBattleGrid(int depth, params Vector3[] positions) {
            if (_canRecieveGoals == false)
                return;


            if (properties == null) {
                Debug.LogError("Agent dont have assigned Properties");
                return;
            }

            if (!properties.battleGrid) {
                Debug.LogWarning("you trying to get battle grid when you dont even set properties to generate battle grid");
                return;
            }

            lock (this)
                if (canSendGridRequest == false)
                    return;

            if (positions.Length == 0)
                PathFinder.GetBattleGrid(this, depth, null, transform.position);
            else
                PathFinder.GetBattleGrid(this, depth, null, positions);
        }


        /// <summary>
        ///cost are usualy distance * area cost
        /// </summary>
        public void SetGoalFindCover(float maxCost) {
            if (_canRecieveGoals == false)
                return;


            if (properties == null) {
                Debug.LogError("Agent dont have assigned Properties");
                return;
            }

            if (!properties.canCover) {
                Debug.LogWarning("you trying to find cover when you dont even set properties to generate covers");
                return;
            }

            lock (this)
                if (canSendCoverRequest == false)
                    return;

            PathFinder.GetCover(this, maxCost, null, ignoreCrouchCost);
        }

        //used for recieve stuff from pathfinder
        //not threadsafe
        public void ReceivePath(Path path) {
            lock (this) {
                canSendPathRequest = true;

                if (_canRecieveResults == false)
                    return;

                if (_path != null)
                    _path.ReturnToPool();

                _path = path;

                if (_recievePathDelegate_NTS != null)
                    _recievePathDelegate_NTS.Invoke(path);

                if (_recievePathDelegate_TS != null)
                    _recievePathUsed = false;
            }
        }
        public void ReceiveCovers(IEnumerable<NodeCoverPoint> covers) {
            lock (this) {
                canSendCoverRequest = true;

                if (_canRecieveResults == false)
                    return;

                _covers.Clear();
                _covers.AddRange(covers);

                if (_recieveCoverPointDelegate_NTS != null)
                    _recieveCoverPointDelegate_NTS.Invoke(covers);

                if (_recieveCoverPointDelegate_TS != null)
                    _recieveCoverPointUsed = false;
            }             
        }
        public void RecieveBattleGrid(IEnumerable<BattleGridPoint> battleGrid) {
            lock (this) {
                canSendGridRequest = true;

                if (_canRecieveResults == false)
                    return;

                _battleGrid = battleGrid;

                if (_recieveBattleGridDelegate_NTS != null) 
                    _recieveBattleGridDelegate_NTS.Invoke(battleGrid);

                if (_recieveBattleGridDelegate_TS != null)
                    _recieveBattleGridUsed = false;
            }
        }


        /// <summary>
        /// iterate through nodes and return if there is node other than move node. movable mean it's only when you move. not jump. so you can tell if agent about to jump
        /// </summary>
        public bool MovableDistanceLesserThan(float targetDistance, out float distance, out PathNode node, out bool reachLastPoint) {
            if (path == null || path.count == 0) {
                Debug.LogWarning("path are invalid");
                node = new PathNode(0, 0, 0, PathNodeType.Invalid);
                distance = 0;
                reachLastPoint = true;
                return false;
            }

            return path.MovableDistanceLesserThan(targetDistance, out distance, out node, out reachLastPoint);
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

        //shortcuts
        private static Vector2 ToVector2(Vector3 v3) {
            return new Vector2(v3.x, v3.z);
        }
        private static Vector3 ToVector3(Vector2 v2) {
            return new Vector3(v2.x, 0, v2.y);
        } 
    }
}
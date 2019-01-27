using UnityEngine;

using K_PathFinder.NodesNameSpace;
using K_PathFinder.EdgesNameSpace;

namespace K_PathFinder.Graphs {
    public abstract class CellConnectionAbstract {
        public bool interconnection { get; private set; }
        public Cell from { get; private set; }
        public Cell connection { get; private set; }
        public CellConnectionAbstract(Cell from, Cell connection, bool interconnection) {
            this.from = from;
            this.connection = connection;
            this.interconnection = interconnection;
        }

        public abstract float Cost(AgentProperties properties);
        public abstract float Cost(Vector3 fromPos, AgentProperties properties);
    }

    public class CellNeightbourConnection : CellConnectionAbstract {
        private float _costFrom, _costTo;
        private Node _left, _right;
        private Vector3 _intersection;

        public CellNeightbourConnection(Cell from, Cell connection, Node left, Node right, float costFrom, float costTo, Vector3 intersection, bool interconnection) : base(from, connection, interconnection) {    
            _costFrom = costFrom;
            _costTo = costTo;
            _left = left;
            _right = right;
            _intersection = intersection;
        }

        public float costFrom {
            get { return _costFrom; }
        }
        public float costTo {
            get { return _costTo; }
        }
        public Vector3 intersection {
            get { return _intersection; }
        }

        public override float Cost(AgentProperties properties) {
            float result = 0;
            switch (from.passability) {
                case Passability.Crouchable:
                result += properties.crouchMod * _costFrom;
                break;
                case Passability.Walkable:
                result += properties.walkMod * _costFrom;
                break;
                default:
                    Debug.LogWarning("wrong passability in cost mod");
                break;
            }

            switch (connection.passability) {
                case Passability.Crouchable:
                result += properties.crouchMod * _costTo;
                break;
                case Passability.Walkable:
                result += properties.walkMod * _costTo;
                break;
                default:
                    UnityEngine.Debug.LogWarning("wrong passability in cost mod");
                break;
            }
            return result;
        }
        public override float Cost(Vector3 fromPos, AgentProperties properties) {
            float result = 0;
            //switch (from.passability) {
            //    case Passability.Crouchable:
            //        result += properties.crouchMod * Vector3.Distance(fromPos, edge.intersection);
            //        break;
            //    case Passability.Walkable:
            //        result += properties.walkMod * Vector3.Distance(fromPos, edge.intersection);
            //        break;
            //    default:
            //        Debug.LogWarning("wrong passability in cost mod");
            //        break;
            //}


            switch (connection.passability) {
                case Passability.Crouchable:
                    result += properties.crouchMod * _costTo;
                    break;
                case Passability.Walkable:
                    result += properties.walkMod * _costTo;
                    break;
                default:
                    UnityEngine.Debug.LogWarning("wrong passability in cost mod");
                    break;
            }
            return result;
        }
        
        //left
        public Node left {
            get { return _left; }
        }
        public Vector2 leftV2 {
            get { return _left.positionV2; }
        }
        public Vector3 leftV3 {
            get { return _left.positionV3; }
        }

        //right
        public Node right {
            get { return _right; }
        }
        public Vector2 rightV2 {
            get { return _right.positionV2; }
        }
        public Vector3 rightV3 {
            get { return _right.positionV3; }
        }
    }
    
    public abstract class CellPointedConnection : CellConnectionAbstract {
        public Vector3 enterPoint { get; private set; }
        public Vector3 exitPoint { get; private set; }
        protected float _costFrom, _costTo;

        public CellPointedConnection(Cell from, Cell connection, Vector3 enterPoint, Vector3 exitPoint, bool interconnection) : base(from, connection, interconnection) {
            this.enterPoint = enterPoint;
            this.exitPoint = exitPoint;

            _costFrom = Vector3.Distance(from.centerVector3, enterPoint);
            _costTo = Vector3.Distance(connection.centerVector3, exitPoint);
        }
    }

    public class CellJumpUpConnection : CellPointedConnection {
        public Vector3 axis { get; private set; }
        public Vector3 jumpPoint { get; private set; }

        public CellJumpUpConnection(Cell from, Cell connection, Vector3 enterPoint, Vector3 jumpPoint, Vector3 axis, Vector3 exitPoint, bool interconnection) : base(from, connection, enterPoint, exitPoint, interconnection) {
            this.jumpPoint = jumpPoint;
            this.axis = axis;
        }

        public override float Cost(AgentProperties properties) {
            float result = 0;
            switch (from.passability) {
                case Passability.Crouchable:
                result += properties.crouchMod * _costFrom;
                break;
                case Passability.Walkable:
                result += properties.walkMod * _costFrom;
                break;
                default:
                    UnityEngine.Debug.LogWarning("wrong passability in cost mod");
                break;
            }

            switch (connection.passability) {
                case Passability.Crouchable:
                result += properties.crouchMod * _costTo;
                break;
                case Passability.Walkable:
                result += properties.walkMod * _costTo;
                break;
                default:
                    UnityEngine.Debug.LogWarning("wrong passability in cost mod");
                break;
            }

            result += properties.jumpUpMod;
            return result;
        }
        public override float Cost(Vector3 fromPos, AgentProperties properties) {
            float result = 0;
            switch (from.passability) {
                case Passability.Crouchable:
                result += properties.crouchMod * Vector3.Distance(fromPos, enterPoint);
                break;
                case Passability.Walkable:
                result += properties.walkMod * Vector3.Distance(fromPos, enterPoint);
                break;
                default:
                    UnityEngine.Debug.LogWarning("wrong passability in cost mod");
                break;
            }

            switch (connection.passability) {
                case Passability.Crouchable:
                result += properties.crouchMod * _costTo;
                break;
                case Passability.Walkable:
                result += properties.walkMod * _costTo;
                break;
                default:
                    UnityEngine.Debug.LogWarning("wrong passability in cost mod");
                break;
            }

            result += properties.jumpUpMod;
            return result;
        }
    }

    public class CellJumpDownConnection : CellPointedConnection {
        public Vector3 axis { get; private set; }
        public Vector3 landPoint { get; private set; }
        public CellJumpDownConnection(Cell from, Cell connection, Vector3 enterPoint, Vector3 axis, Vector3 landPoint, Vector3 exitPoint, bool interconnection) : base(from, connection, enterPoint, exitPoint, interconnection) {
            this.axis = axis;
            this.landPoint = landPoint;
        }

        public override float Cost(AgentProperties properties) {
            float result = 0;
            switch (from.passability) {
                case Passability.Crouchable:
                result += properties.crouchMod * _costFrom;
                break;
                case Passability.Walkable:
                result += properties.walkMod * _costFrom;
                break;
                default:
                    UnityEngine.Debug.LogWarning("wrong passability in cost mod");
                break;
            }

            switch (connection.passability) {
                case Passability.Crouchable:
                result += properties.crouchMod * _costTo;
                break;
                case Passability.Walkable:
                result += properties.walkMod * _costTo;
                break;
                default:
                    UnityEngine.Debug.LogWarning("wrong passability in cost mod");
                break;
            }

            result += properties.jumpDownMod;
            return result;
        }
        public override float Cost(Vector3 fromPos, AgentProperties properties) {
            float result = 0;
            switch (from.passability) {
                case Passability.Crouchable:
                result += properties.crouchMod * Vector3.Distance(fromPos, enterPoint);
                break;
                case Passability.Walkable:
                result += properties.walkMod * Vector3.Distance(fromPos, enterPoint);
                break;
                default:
                    UnityEngine.Debug.LogWarning("wrong passability in cost mod");
                break;
            }

            switch (connection.passability) {
                case Passability.Crouchable:
                result += properties.crouchMod * _costTo;
                break;
                case Passability.Walkable:
                result += properties.walkMod * _costTo;
                break;
                default:
                    UnityEngine.Debug.LogWarning("wrong passability in cost mod");
                break;
            }

            result += properties.jumpUpMod;
            return result;
        }
    }
}

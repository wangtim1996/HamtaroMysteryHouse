using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading;
#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif
namespace K_PathFinder {
    //TODO:
    //plane intersection case with 1 and 2 VO
    //priority agents

   

    public static class PathFinderMainRVO {

        private static Queue<RVOAllocatedData> allocatedData = new Queue<RVOAllocatedData>();

        //shape
        const int SHAPE_EDGES_COUNT = 8; //default shape edges count;

        //ray
        const float TWO_PI = Mathf.PI + Mathf.PI;
        const int RAY_SUBDIVISIONS = 720; //amount of possible directions. affect next to nothing exept memory
        const int RAY_SUBDIVISIONS_MINUS_ONE = RAY_SUBDIVISIONS - 1;
        const int RAY_QUAD_SPHERE_SUBDIVISIONS = 5; //amount of ray samples * 2 + 1. affect perfomance directly
        const int RAY_COUNT = 1 + RAY_QUAD_SPHERE_SUBDIVISIONS + RAY_QUAD_SPHERE_SUBDIVISIONS;
        static RVORayDataSet[] raySamples;
        //static float[] rayLengthTemp = new float[RAY_COUNT];

        //async
        private static ManualResetEvent[] _updateAgentPositionEvents;

        static PathFinderMainRVO() {
            InitRayArray();
        }

        #region ray sets managment
        private static void InitRayArray() {
            //segments length
            float[] lengths = new float[RAY_QUAD_SPHERE_SUBDIVISIONS];
            for (int i = 0; i < RAY_QUAD_SPHERE_SUBDIVISIONS; i++) {
                lengths[i] = SomeMath.NearestPointOnLine(Vector2.zero, RadToVector((i + 1) * Mathf.PI / (RAY_QUAD_SPHERE_SUBDIVISIONS + 2) * 0.5f), Vector2.right).magnitude;
            }

            //assemble data points
            raySamples = new RVORayDataSet[RAY_SUBDIVISIONS];
            for (int i = 0; i < RAY_SUBDIVISIONS; i++) {
                int vectorArrayLength = 1 + RAY_QUAD_SPHERE_SUBDIVISIONS + RAY_QUAD_SPHERE_SUBDIVISIONS;
                Vector2[] vectorArray = new Vector2[vectorArrayLength];
                float[] lengthArray = new float[vectorArrayLength];

                float curRadian = i * TWO_PI / RAY_SUBDIVISIONS + Mathf.PI;
                vectorArray[0] = RadToVector(curRadian);
                lengthArray[0] = 1f;

                for (int s = 0; s < RAY_QUAD_SPHERE_SUBDIVISIONS; s++) {
                    float curOffset = (s + 1) * Mathf.PI / (RAY_QUAD_SPHERE_SUBDIVISIONS + 2) * 0.5f;
                    vectorArray[(s * 2) + 1] = RadToVector(curRadian + curOffset);
                    vectorArray[(s * 2) + 2] = RadToVector(curRadian - curOffset);
                    lengthArray[(s * 2) + 1] = lengths[s];
                    lengthArray[(s * 2) + 2] = lengths[s];
                }

                raySamples[i] = new RVORayDataSet(vectorArray, lengthArray);
            }
        }
        static RVORayDataSet GetNearestRayDataSet(float x, float y) {
            return raySamples[Mathf.Clamp((int)((((Mathf.Atan2(y, x) + Mathf.PI) / TWO_PI) * RAY_SUBDIVISIONS) + 0.5f), 0, RAY_SUBDIVISIONS_MINUS_ONE)];
        }
        #endregion

        #region alocated data managment
        private static RVOAllocatedData RentAllocated() {
            lock (allocatedData) {
                if (allocatedData.Count == 0)
                    return new RVOAllocatedData();
                else
                    return allocatedData.Dequeue();
            }
        }

        private static void ReturnAllocated(RVOAllocatedData data) {
            lock (allocatedData) {
                allocatedData.Enqueue(data);
            }
        }
        #endregion

        public static void UpdateAllAgents() {
#if UNITY_EDITOR
            if(Debuger_K.debugRVO && (Debuger_K.debugRVObasic | Debuger_K.debugRVOvelocityObstacles | Debuger_K.debugRVOconvexShape | Debuger_K.debugRVOplaneIntersections))
                Debuger_K.ClearGeneric();
#endif
            RVOAllocatedData data = RentAllocated();
            for (int i = 0; i < PathFinder.agents.Count; i++) {lock (PathFinder.agents[i])CalculateNewSafeVelocity(PathFinder.agents[i], data);}
            ReturnAllocated(data);
        }

        //public static void UpdateAllAgentsAsync(int maxThreads) {
        //    lock (agents) {
        //        int threads = settings.maxThreads;

        //        if (_updateAgentPositionEvents == null || _updateAgentPositionEvents.Length != threads) {
        //            _updateAgentPositionEvents = new ManualResetEvent[threads];
        //            for (int i = 0; i < settings.maxThreads; i++) {
        //                _updateAgentPositionEvents[i] = new ManualResetEvent(true);
        //            }
        //        }

        //        int curIndex = 0;
        //        int agentsPerThread = (agents.Count / threads) + 1;

        //        for (int i = 0; i < threads; i++) {
        //            int end = curIndex + agentsPerThread;

        //            if (end >= agents.Count) {
        //                end = agents.Count;
        //                _updateAgentPositionEvents[i].Reset();
        //                ThreadPool.QueueUserWorkItem(UpdateAgentNavmeshPositionThreadPoolCallback, new UpdateAgentPositionThreadContext(curIndex, end, _updateAgentPositionEvents[i]));
        //                break;
        //            }
        //            else {
        //                _updateAgentPositionEvents[i].Reset();
        //                ThreadPool.QueueUserWorkItem(UpdateAgentNavmeshPositionThreadPoolCallback, new UpdateAgentPositionThreadContext(curIndex, end, _updateAgentPositionEvents[i]));
        //            }

        //            curIndex = end;
        //        }

        //        WaitHandle.WaitAll(_updateAgentPositionEvents);

        //    }
        //}
        
        static void CalculateNewSafeVelocity(PathFinderAgent agent, RVOAllocatedData allocatedData) {
            if (agent.velocityObstacle == false)//if not obstacle then nothing to do
                return; 

            if (agent.outsideNavmesh) {//if not on navmesh then move to navmesh
                agent.safeVelocity = ToV2(agent.nearestNavmeshPoint - agent.positionVector3).normalized * agent.maxAgentVelocity;
                return;
            }

            float agentRadius = agent.radius;
            Vector2 agentPosition = agent.positionVector2;
            Vector2 agentVelocity = agent.velocity;
            List<ORCAline> ORCAlines = allocatedData.ORCAlines;
            ORCAlines.Clear();

            #region debug
#if UNITY_EDITOR
            if (Debuger_K.debugRVO && Debuger_K.debugRVObasic) {
                Debuger_K.AddLine(DrawCircle(50, agent.positionVector3, agentRadius), Color.black, true);
                Debuger_K.AddRay(agent.positionVector3, ToV3(agentVelocity), Color.white);
                Debuger_K.AddRay(agent.positionVector3, ToV3(agent.preferableVelocity), Color.yellow);
            }
#endif
            #endregion

            foreach (var neighbour in agent.neighbourAgents) {
                if (neighbour.velocityObstacle == false) continue; //ignore neighbour if it's not velocity obstacle

                float responcibility = agent.avoidanceResponsibility / (agent.avoidanceResponsibility + neighbour.avoidanceResponsibility);
                if (responcibility <= 0f) continue;
                if (responcibility > 1f) responcibility = 1f;

                Vector2 localPos = neighbour.positionVector2 - agentPosition;
                float localPosSqrMagnitude = localPos.sqrMagnitude;
                float radiusSum = agentRadius + neighbour.radius;
                float radiusSumSqr = SomeMath.Sqr(radiusSum);            

                #region collision occure
                //agents are overlaping radius
                if (localPosSqrMagnitude < radiusSumSqr) {             
                    if (localPosSqrMagnitude < 0.01f) {//agents literaly inside each others so we give it some random vector
                        System.Random random = new System.Random(agent.GetHashCode());
                        float rX = random.Next(1, 100) * 0.01f;
                        float rY = random.Next(1, 100) * 0.01f;
                        if (random.Next(1) == 1)rX *= -1;
                        if (random.Next(1) == 1)rY *= -1;
                        Vector2 randomVector = new Vector2(rX, rY).normalized;
                        ORCAlines.Add(new ORCAline(randomVector * -agentRadius, -randomVector, responcibility));
                    }
                    else {
                        ORCAlines.Add(new ORCAline(-(radiusSum - Mathf.Sqrt(localPosSqrMagnitude)) * localPos.normalized, -localPos, responcibility));
                    }
                    continue;       
                }
                #endregion

                Vector2 localVel = agentVelocity - neighbour.velocity;     

                //if object local position and local velocity are in opposite direction then it's dont matter at all
                if (SomeMath.Dot(localVel, localPos) < 0f) continue;

                float localPosMagnitude = localPos.magnitude;
                float localPosRad = Mathf.Atan2(localPos.y, localPos.x); //local position in radians   

                #region prefer one side offsets
                if (agent.preferOneSideEvasion) {
                    float offsetFactor = agent.preferOneSideEvasionOffset * ((localPosMagnitude - radiusSum) / agent.maxNeighbourDistance);
                    float radOffset = (neighbour.radius * offsetFactor) / (localPosMagnitude * 2 * Mathf.PI) * 2 * Mathf.PI;
                    localPosRad += radOffset;
                    localPos = GetTargetVector2(localPosRad, localPosMagnitude);
                    radiusSum += offsetFactor * 0.5f;
                }
                #endregion

                Vector2 localPosNormalized = localPos.normalized;            
                float localVelMagnitude = localVel.magnitude;         
                float angleRad = Mathf.Asin(radiusSum / localPosMagnitude); //offset to side in radians
                float angleDeg = angleRad * Mathf.Rad2Deg;
     

                //trunk
                float truncatedRadius = radiusSum * neighbour.careDistance;
                //float truncatedRadiusSqr = SomeMath.Sqr(truncatedRadius);
                float truncatedBoundryDistance = truncatedRadius / (radiusSum / localPosMagnitude);
                float truncatedBoundryStart = truncatedBoundryDistance * Mathf.Cos(Mathf.Asin(truncatedRadius / truncatedBoundryDistance));
                Vector2 truncatedBoundryCenter = localPosNormalized * truncatedBoundryDistance;

                #region debug
#if UNITY_EDITOR
                if (Debuger_K.debugRVO && Debuger_K.debugRVOvelocityObstacles) {
                    //Debug radius sum before it is changed
                    if (agent.preferOneSideEvasion)
                        Debuger_K.AddLine(DrawCircle(50, neighbour.positionVector3, agentRadius + neighbour.radius), Color.red, true);

                    //Debug radius sum
                    Debuger_K.AddLine(DrawCircle(50, agent.positionVector3 + new Vector3(localPos.x, 0, localPos.y), radiusSum), Color.black, true);

                    //debug cut off circle
                    Debuger_K.AddLine(DrawCircle(50, agent.positionVector3 + ToV3(truncatedBoundryCenter), truncatedRadius), Color.black, true);
                    Vector2 A1 = GetTargetVector2((localPosRad + angleRad), truncatedBoundryStart);
                    Vector2 B1 = GetTargetVector2((localPosRad - angleRad), truncatedBoundryStart);
                    Vector2 A2 = GetTargetVector2((localPosRad + angleRad), truncatedBoundryStart + 10);
                    Vector2 B2 = GetTargetVector2((localPosRad - angleRad), truncatedBoundryStart + 10);
                    Debuger_K.AddLine(agent.positionVector3 + ToV3(A1), agent.positionVector3 + ToV3(A2), Color.blue);
                    Debuger_K.AddLine(agent.positionVector3 + ToV3(B1), agent.positionVector3 + ToV3(B2), Color.blue);

                    //debug local velocity
                    Debuger_K.AddLine(agent.positionVector3, agent.positionVector3 + ToV3(localVel), Color.blue);
                    Debuger_K.AddDot(agent.positionVector3 + ToV3(localVel), Color.blue);
                }
#endif
                #endregion

                #region collision course
                if (localVelMagnitude >= truncatedBoundryDistance - truncatedRadius && //it's closest than closest boundry point
                    Vector2.Angle(localVel, localPos) < angleDeg + 5f) {//if local velocity in angle

                    if (localVelMagnitude <= truncatedBoundryStart) {//if inside truncated area
                        if (SomeMath.SqrDistance(truncatedBoundryCenter, localVel) < SomeMath.Sqr(truncatedRadius)) {
                            Vector2 closestToBoundry = (localVel - truncatedBoundryCenter).normalized * truncatedRadius + truncatedBoundryCenter;
                            Vector2 uVector = closestToBoundry - localVel;
                            Vector2 velocityBorder = agentVelocity + (uVector * responcibility);
                            ORCAlines.Add(new ORCAline(velocityBorder, uVector.normalized, responcibility));
                        }
                    }
                    else {
                        //more fancy way but i dont fully understand it yet
                        //Vector2 w = localVel - 0.1f * localPos;
                        //float wLengthSq = (w).sqrMagnitude;
                        //float dotProduct1 = Vector2.Dot(w, localPos);
                        //float leg = (float)Math.Sqrt(localPosSqrMagnitude - radiusSumSqr);
                        //Vector2 lineDir;
                        

                        if (SomeMath.V2Cross(localPos, localVel) < 0) {//project on left leg
                            //lineDir = new Vector2(localPos.x * leg - localPos.y * radiusSum, localPos.x * radiusSum + localPos.y * leg) / localPosSqrMagnitude;
                            Vector2 legDir = GetTargetVector2(localPosRad + angleRad, 1f);

                            if (agent.preferOneSideEvasion) {
                                responcibility = 1f;               
                            }

                            //that formula below are god know what. here is short  description of it:
                            //Vector2 closestToBoundry = legDir * Vector2.Dot(legDir, localVel);
                            //Vector2 uVector = closestToBoundry - localVel;
                            //Vector2 velocityBorder = agentVelocity + (uVector * responcibility);
                            //ORCAlines.Add(new ORCAline(velocityBorder, new Vector2(-legDir.y, legDir.x), responcibility));

                            ORCAlines.Add(
                                new ORCAline(
                                    agentVelocity + (((legDir * Vector2.Dot(legDir, localVel)) - localVel) * responcibility),
                                    new Vector2(-legDir.y, legDir.x),
                                    responcibility));
                        }
                        else { //project on right leg
                            //lineDir = -new Vector2(localPos.x * leg + localPos.y * radiusSum, -localPos.x * radiusSum + localPos.y * leg) / localPosSqrMagnitude;

                            Vector2 legDir = GetTargetVector2(localPosRad - angleRad, 1f);
                            ORCAlines.Add(
                                new ORCAline(
                                    agentVelocity + (((legDir * Vector2.Dot(legDir, localVel)) - localVel) * responcibility),
                                    new Vector2(legDir.y, -legDir.x),
                                    responcibility));
                        }  
                        //Vector2 linePos = agentVelocity + 0.5f * (Vector2.Dot(localVel, lineDir) * lineDir - localVel);
                        //lineDir = new Vector2(-lineDir.x, lineDir.y);                        
                        //Debuger_K.AddDot(agent.positionVector3 + ToV3(linePos) + (Vector3.up * 0.1f), Color.red);
                        //Debuger_K.AddLine(agent.positionVector3 + (Vector3.up * 0.1f), agent.positionVector3 + ToV3(linePos) + (Vector3.up * 0.1f), Color.red);
                        //Debuger_K.AddLine(agent.positionVector3 + ToV3(linePos) + (Vector3.up * 0.1f), agent.positionVector3 + ToV3(lineDir) + (Vector3.up * 0.1f), Color.blue);
                    }                
                }
                #endregion                
            }

            #region deadlock failsafe
            if (agent.useDeadLockFailsafe && DateTime.Now.Subtract(agent.deadLockTriggeredTime).TotalSeconds < agent.deadLockFailsafeTime) {      
                foreach (var neighbour in agent.neighbourAgents) {
                    float distance = Vector2.Distance(agentPosition, neighbour.positionVector2);
                    float radiusSum = agentRadius + neighbour.radius;
                    float freeSpace = distance - radiusSum;

                    if (freeSpace < agent.deadLockFailsafeVelocity) {
                        if (freeSpace < 0f)
                            freeSpace = 0f;

                        Vector2 direction = (neighbour.positionVector2 - agentPosition).normalized;
                        ORCAlines.Add(new ORCAline((agent.deadLockFailsafeVelocity + freeSpace) * -1 * direction, -direction, 0.5f));
                    }
                }                
            }

            //if (agent.useDeadLockFailsafe && (Time.time - agent.deadLockTriggeredTime) < agent.deadLockFailsafeTime) {
            //    foreach (var neighbour in agent.neighbourAgents) {
            //        float distance = Vector2.Distance(agentPosition, neighbour.positionVector2);
            //        float radiusSum = agentRadius + neighbour.radius;
            //        float freeSpace = distance - radiusSum;

            //        if (freeSpace < agent.deadLockFailsafeVelocity) {
            //            if (freeSpace < 0f)
            //                freeSpace = 0f;

            //            Vector2 direction = (neighbour.positionVector2 - agentPosition).normalized;
            //            ORCAlines.Add(new ORCAline((agent.deadLockFailsafeVelocity + freeSpace) * -1 * direction, -direction, 0.5f));
            //        }
            //    }
            //}
            #endregion


            //**************MANUAL ADDING**************//
            //var debugOrca = agent.transform.GetComponentsInChildren<DebugOrca>();
            //foreach (var item in debugOrca) {
            //    if (item.enabled == false | item.gameObject.activeInHierarchy == false)
            //        continue;
            //    ORCAline dOrca = new ORCAline(ToV2(item.transform.position) - agentPosition, ToV2(item.transform.forward).normalized, item.responcibility);
            //    ORCAlines.Add(dOrca);
            //    Vector3 n = GetNormal3d(dOrca);
            //    //item.transform.position -= (new Vector3(n.x, 0, n.z) * Time.deltaTime * 0.1f);
            //}
            //**************MANUAL ADDING**************//


            #region debugVelocityObstacle
#if UNITY_EDITOR
            if (Debuger_K.debugRVO && Debuger_K.debugRVOvelocityObstacles) {
                foreach (var ORCA in ORCAlines) {
                    Debuger_K.AddDot(ToV3(ORCA.position) + agent.positionVector3, Color.blue, 0.05f);
                    Debuger_K.AddLine(agent.positionVector3 + ToV3(ORCA.position), agent.positionVector3 + ToV3(ORCA.position) + ToV3(ORCA.normal), Color.blue, 0.002f);
                }
            }
#endif
            #endregion

            #region apply ORCA lines
            allocatedData.ResetShape(agent.maxAgentVelocity);
            List<float> cross = allocatedData.cross;
            List<Vector2> shape = allocatedData.shape;

            //fancy way to chip off chunks from some convex shape
            foreach (var ORCA in ORCAlines) {
                Vector2 ORCApos = ORCA.position;
                Vector2 ORCArotated = new Vector2(-ORCA.normal.y, ORCA.normal.x);
                cross.Clear();

                bool anyPlus = false, anyMinus = false;
                for (int i = 0; i < shape.Count; i++) {
                    float crossCur = Mathf.Sign(SomeMath.V2Cross(ORCArotated, shape[i] - ORCApos));
                    if (crossCur == 1) anyPlus = true; else anyMinus = true;
                    cross.Add(crossCur);
                }
                shape.Add(shape[0]);
                cross.Add(cross[0]);

                if (anyMinus) {
                    if (anyPlus) {
                        for (int i = 0; i < shape.Count - 1; i++) {
                            if (cross[i] != cross[i + 1]) {
                                Vector2 intersection;
                                SomeMath.LineIntersection(shape[i], shape[i + 1], ORCApos, ORCApos + ORCArotated, out intersection);

                                if (cross[i] == 1) {
                                    shape.Insert(i + 1, intersection);
                                    cross.Insert(i + 1, 0);
                                    i++;
                                }
                                else if (cross[i + 1] == 1) {
                                    shape.Insert(i, intersection);
                                    cross.Insert(i, 0);
                                    i++;
                                }
                            }
                            if (cross[i] == -1) {
                                shape.RemoveAt(i);
                                cross.RemoveAt(i);
                                i--;
                            }
                        }
                        if (shape.Count > 0) {
                            shape[shape.Count - 1] = shape[0];
                        }
                    }
                    else {
                        shape.Clear();
                        break;
                    }
                }

            }
#if UNITY_EDITOR
            if (Debuger_K.debugRVO && Debuger_K.debugRVOconvexShape) {
                allocatedData.DrawCurrentShape(agent.positionVector3, new Color(0, 1, 0, 0.2f), Color.black);
            }
#endif
            #endregion

            if (shape.Count < 3) {
                Vector2 planeResult;
                if(SolvePlanesIntersections(ORCAlines, agent, out planeResult) == false) {
                    //string s = "\n";
                    //foreach (var item in ORCAlines) {s += string.Format("position: {0}, normal: {1} \n", item.position, item.normal);}
                    //Debug.LogWarningFormat("somehow velocity obstacles plane intersection solver dont solve those planes. pease tell developer how it's happen. ORCA list: \n{0}", s);
                }
                agent.safeVelocity = planeResult;
                return;
            }

            Vector2 prefVelocity = agent.preferableVelocity;
            float prefVelocityMagnitude = prefVelocity.magnitude;
            Vector2 prefVelocityNormalized = prefVelocity.normalized;
            Vector3 agentPosV3 = agent.positionVector3;

            //finding nearest point on shape
            Vector2 nearestPointOnShape = GetNearestPointOnShape(shape, prefVelocity, agentVelocity);

#if UNITY_EDITOR
            if (Debuger_K.debugRVO && Debuger_K.debugRVObasic) {
                Debuger_K.AddLine(ToV3(nearestPointOnShape) + agentPosV3, ToV3(prefVelocity) + agentPosV3, Color.cyan);
                Debuger_K.AddDot(ToV3(nearestPointOnShape) + agentPosV3, Color.black);
            }
#endif

            //route 1: nearest point on shape dont hit navmesh and it safe to move here
            RaycastHitNavMesh2 hit;
            if (PathFinder.Raycast(agent, nearestPointOnShape.x, nearestPointOnShape.y, agent.maxAgentVelocity, out hit) == false || //if raycast dont hit
                SomeMath.SqrMagnitude(nearestPointOnShape.x, nearestPointOnShape.y) < SomeMath.SqrDistance(hit.x, hit.z, agentPosition.x, agentPosition.y)) { //or hitpoint are further than nearest point
                agent.safeVelocity = nearestPointOnShape;   
            }
            else {
                //route 2: nearest point on shape are obstructed by navmesh   
                RaycastHitNavMesh2[] raycastResult = allocatedData.raycastResult;

                bool anyResult = false;
                Vector2 curClosest = new Vector2();
                float curClosestSqrDistance = float.MaxValue;                

                #region nearest point are in direction of prefered velocity
                //if nearest point on shape are in direction of prefered velocity then we check shape and navmesh in circle pattern to find nearest to optimal velocity
                if (SomeMath.Dot(nearestPointOnShape, prefVelocity) > 0) {
                    var curDataSet = GetNearestRayDataSet(prefVelocity.normalized.x, prefVelocity.normalized.y);
                    float[] curRayLengthData = curDataSet.lengthArray;
                    Vector2[] curRayVectorData = curDataSet.vectorArray;

                    float[] rayLengthTemp = allocatedData.rayLengthTemp;

                    float rayBorder = Mathf.Min(prefVelocityMagnitude, agent.maxAgentVelocity);
                    for (int i = 0; i < curRayLengthData.Length; i++) {
                        rayLengthTemp[i] = curRayLengthData[i] * rayBorder;
                    }
                    
                    //RAYCAST
                    PathFinder.Raycast(agent, curRayVectorData, rayLengthTemp, ref raycastResult);//doing bunch of samples with raycasting

                    for (int i = 0; i < RAY_COUNT; i++) {
                        if (raycastResult[i].resultType == NavmeshRaycastResultType2.OutsideGraph)
                            continue;

                        Vector2 curRayResult;
                        if (raycastResult[i].resultType == NavmeshRaycastResultType2.NavmeshBorderHit)
                            curRayResult = ToV2(raycastResult[i].point - agentPosV3);
                        else
                            curRayResult = curRayVectorData[i] * rayLengthTemp[i];

                        float curRayDir_x = curRayResult.x;
                        float curRayDir_y = curRayResult.y;
                        if (ClipPointToFarShapeBorder(shape, ref curRayDir_x, ref curRayDir_y)) {
                            anyResult = true;
#if UNITY_EDITOR
                            if (Debuger_K.debugRVO && Debuger_K.debugRVObasic)
                                Debuger_K.AddLine(agentPosV3, new Vector3(curRayDir_x, 0, curRayDir_y) + agentPosV3, Color.magenta);
#endif

                            float curNearestSqrDistance = SomeMath.SqrDistance(prefVelocity.x, prefVelocity.y, curRayDir_x, curRayDir_y);
                            if (curNearestSqrDistance < curClosestSqrDistance) {
                                curClosestSqrDistance = curNearestSqrDistance;
                                curClosest = new Vector2(curRayDir_x, curRayDir_y);
                            }
                        }
                    }
                } 
                #endregion
                #region nearest point are NOT in direction of prefered velocity

                else {
                    var curDataSet = GetNearestRayDataSet(nearestPointOnShape.normalized.x, nearestPointOnShape.normalized.y);
                    Vector2[] curRayVectorData = curDataSet.vectorArray;

                    for (int r = 0; r < curRayVectorData.Length; r++) {
#if UNITY_EDITOR
                        if (Debuger_K.debugRVO && Debuger_K.debugRVObasic)
                            Debuger_K.AddLine(agentPosV3, ToV3(curRayVectorData[r]) + agentPosV3, Color.green);
#endif

                        Vector2 curRay = curRayVectorData[r];
                        bool shapeHit = false;
                        float shapeHit_x;
                        float shapeHit_y;
                        float shapeHitSqrDistance = float.MaxValue;
                        shapeHit_x = shapeHit_y = 0;

                        for (int s = 0; s < shape.Count - 1; s++) {
                            Vector2 shape1 = shape[s];
                            Vector2 shape2 = shape[s + 1];
                            float hit_x, hit_y;                      

                            if(SomeMath.RayIntersectSegment(curRay.x, curRay.y, shape1.x, shape1.y, shape2.x, shape2.y, out hit_x, out hit_y)){
                                shapeHit = true;
                                float curShapeHitSqrDistance = SomeMath.SqrMagnitude(hit_x, hit_y);
                                if (curShapeHitSqrDistance < shapeHitSqrDistance) {
                                    shapeHit_x = hit_x;
                                    shapeHit_y = hit_y;
                                }
                            }
                        }

                        if (shapeHit) {
                            RaycastHitNavMesh2 raycastHit;
                            PathFinder.Raycast(agent, curRay.x, curRay.y, agent.maxAgentVelocity, out raycastHit);//doing bunch of samples with raycasting

                            if (PathFinder.Raycast(agent, curRay.x, curRay.y, agent.maxAgentVelocity, out hit) == false || //if raycast dont hit
                                SomeMath.SqrMagnitude(shapeHit_x, shapeHit_y) < SomeMath.SqrDistance(hit.x, hit.z, agentPosition.x, agentPosition.y)) { //or hitpoint are further than nearest point
                                anyResult = true;
                                float curNearestSqrDistance = SomeMath.SqrDistance(prefVelocity.x, prefVelocity.y, shapeHit_x, shapeHit_y);
                                if (curNearestSqrDistance < curClosestSqrDistance) {
                                    curClosestSqrDistance = curNearestSqrDistance;
                                    curClosest = new Vector2(shapeHit_x, shapeHit_y);
                                }
#if UNITY_EDITOR
                                if (Debuger_K.debugRVO && Debuger_K.debugRVObasic)
                                    Debuger_K.AddLine(ToV3(curRayVectorData[r]) + agentPosV3, new Vector3(shapeHit_x, 0, shapeHit_y) + agentPosV3, Color.blue);
#endif
                            }
                        }
                    }
                }
                #endregion

                if (anyResult) {   //if any raycast result are exist
                    agent.safeVelocity = curClosest;
#if UNITY_EDITOR
                    if (Debuger_K.debugRVO && Debuger_K.debugRVObasic)
                        Debuger_K.AddLine(ToV3(prefVelocity) + agentPosV3, ToV3(curClosest) + agentPosV3, Color.green);
#endif
                }
                else {
                    agent.safeVelocity = Vector2.zero;
                }
            }

            //check if velocity are too small
            if(agent.useDeadLockFailsafe && nearestPointOnShape.sqrMagnitude < SomeMath.Sqr(agent.deadLockFailsafeVelocity)) {
                agent.deadLockTriggeredTime = DateTime.Now;       
            }
        }
        
        private static bool ClipPointToFarShapeBorder(List<Vector2> shape, ref float x, ref float y) {
            float sqrMagnitude = SomeMath.SqrMagnitude(x, y);
            bool flagLow = false;
            bool flagHigh = false;

            for (int s = 0; s < shape.Count - 1; s++) {
                Vector2 shape1 = shape[s];
                Vector2 shape2 = shape[s + 1];

                float hit_x, hit_y;
                if (SomeMath.LineIntersectSegment(x, y, shape1.x, shape1.y, shape2.x, shape2.y, out hit_x, out hit_y)) {
                    bool isForward = SomeMath.Dot(x, y, hit_x, hit_y) > 0;

                    if (SomeMath.RotateLineRightAndReturnDot(shape1.x, shape1.y, shape2.x, shape2.y, x, y) > 0) {
                        if (isForward) {
                            if (SomeMath.SqrMagnitude(hit_x, hit_y) < sqrMagnitude) {
                                x = hit_x;
                                y = hit_y;
                            }
                            flagHigh = true;
                        }
                    }
                    else {
                        if (isForward) {
                            flagLow = SomeMath.SqrMagnitude(hit_x, hit_y) < sqrMagnitude;
                            if (flagLow)
                                break;
                        }
                        else {
                            flagLow = true;
                        }
                    }
                }
            }

            return flagLow && flagHigh;
        }
        private static bool ClipPointToNearShapeBorder(List<Vector2> shape, float x, float y) {
            float sqrMagnitude = SomeMath.SqrMagnitude(x, y);
            bool flagLow = false;
            bool flagHigh = false;

            for (int s = 0; s < shape.Count - 1; s++) {
                Vector2 shape1 = shape[s];
                Vector2 shape2 = shape[s + 1];

                float hit_x, hit_y;
                if (SomeMath.LineIntersectSegment(x, y, shape1.x, shape1.y, shape2.x, shape2.y, out hit_x, out hit_y)) {
                    bool isForward = SomeMath.Dot(x, y, hit_x, hit_y) > 0;

                    if (SomeMath.RotateLineRightAndReturnDot(shape1.x, shape1.y, shape2.x, shape2.y, x, y) > 0) {
                        if (isForward) {
                            if (SomeMath.SqrMagnitude(hit_x, hit_y) < sqrMagnitude) {
                                x = hit_x;
                                y = hit_y;
                            }
                            flagHigh = true;
                        }
                    }
                    else {
                        if (isForward) {
                            flagLow = SomeMath.SqrMagnitude(hit_x, hit_y) < sqrMagnitude;
                            if (flagLow)
                                break;
                        }
                        else {
                            flagLow = true;
                        }
                    }
                }
            }

            return flagLow && flagHigh;
        }

        private static Vector2 GetNearestPointOnShape(List<Vector2> shape, Vector2 targetVelocity, Vector2 curVelocity) {        
            for (int i = 0; i < shape.Count - 2; i++) {
                if (SomeMath.PointInTriangle(shape[0], shape[i], shape[i + 1], targetVelocity))
                    return targetVelocity;
            }

            Vector2 result = new Vector2();
            float curSqrDist = float.MaxValue;

            for (int i = 0; i < shape.Count - 1; i++) {
                Vector2 curClosestPos = SomeMath.NearestPointOnSegment(shape[i], shape[i + 1], targetVelocity);
                //shortest path betwin current speed and target speed to decuse oscillations
                float sqrDist = SomeMath.SqrDistance(curClosestPos, targetVelocity) + SomeMath.SqrDistance(curClosestPos, curVelocity);
                if (sqrDist < curSqrDist) {
                    curSqrDist = sqrDist;
                    result = curClosestPos;
                }
            }
            return result;
        }
        
        #region plane intersection
        /// <summary>
        /// solve plane intersection to return first avaible velocity in case when avaible velocity area is zero
        /// kinda long and hard to understand. short description:
        /// when orca lines enclose all space then this function are used. Is solve 3 cases:
        /// 1) When it is only one line enclose all space. In this case sellected point are closest to nearest point of this line. 
        /// It will be first avaiable point if this line are moved in opposite direction
        /// 2) when 2 lines enclose all space. there is 2 cases:
        /// a) Lines are paralel. In this case sellected farthest one which enclose visible space at all
        /// b) Lines are NOT paralel. In this case sellected first visible point if lines are moved to opposite directions
        /// 3) when there is more than 2 lines. In this case siquentialy will be sellected 3 planes, check where intersection
        /// of them represented as lines. And THEN checked intersection of those lines. If this point are not enclosed by other planes in is siutable one.
        /// lowest point will be result. it will be first avaible poine if orca lines moved in opposite direction.
        /// </summary>
        private static bool SolvePlanesIntersections(List<ORCAline> orcaList, PathFinderAgent agent, out Vector2 result) {
            Vector3 offset = agent.positionVector3;

            int count = orcaList.Count;

#if UNITY_EDITOR
            if (Debuger_K.debugRVO && Debuger_K.debugRVOplaneIntersections) {
                foreach (var orca in orcaList) {
                    DrawPlaneORCA(orca, offset);
                }
            }
#endif

            bool haveResult = false;
            float maxVel = agent.maxAgentVelocity;
            float maxVelSqr = SomeMath.Sqr(maxVel);

            switch (count) {
                case 1:
              
                    ORCAline case1Orca = orcaList[0];
                    Vector2 case1pos = case1Orca.position;
                    Vector2 case1dir = case1Orca.normal;
                    case1dir = new Vector2(-case1dir.y, case1dir.x);

                    float dirXcase1 = case1dir.x;
                    float dirYcase1 = case1dir.y;

                    float case1ABmagnitude = SomeMath.Magnitude(dirXcase1, dirYcase1);

                    dirXcase1 /= case1ABmagnitude;
                    dirYcase1 /= case1ABmagnitude;

                    float case1mul = SomeMath.Dot(-case1pos.x, -case1pos.y, dirXcase1, dirYcase1);
                    float case1nearestX = case1pos.x + dirXcase1 * case1mul;
                    float case1nearestY = case1pos.y + dirYcase1 * case1mul;
                    result = new Vector2(case1nearestX, case1nearestY);
                    if (SomeMath.SqrMagnitude(result) > maxVelSqr)
                        result = result.normalized * maxVel;

                    //Debuger_K.AddLine(agent.positionVector3, agent.positionVector3 + ToV3(result), Color.green);
                    return true;          
                case 2:
                    ORCAline orcaA = orcaList[0];
                    ORCAline orcaB = orcaList[1];
                    Vector3 normalA = GetNormal3d(orcaA);
                    Vector3 normalB = GetNormal3d(orcaB);
                    Vector3 intPos, intDir;
                    if (Math3d.PlanePlaneIntersection(out intPos, out intDir, normalA, ToV3(orcaA.position), normalB, ToV3(orcaB.position))) {
#if UNITY_EDITOR
                        if (Debuger_K.debugRVO && Debuger_K.debugRVOplaneIntersections) {
                            Debuger_K.AddRay(offset + intPos, intDir, Color.red, 50f);
                            Debuger_K.AddRay(offset + intPos, intDir, Color.red, -50f);
                            Debuger_K.AddLine(DrawCircle(50, agent.positionVector3, agent.maxAgentVelocity), Color.black, true);
                            Debuger_K.AddRay(offset + new Vector3(intPos.x, 0, intPos.z), new Vector3(intDir.x, 0, intDir.z), Color.red, 50f);
                            Debuger_K.AddRay(offset + new Vector3(intPos.x, 0, intPos.z), new Vector3(intDir.x, 0, intDir.z), Color.red, -50f);
                        }
#endif

                        if (intDir.y > 0)//this vector should point down
                            intDir *= -1;

                        float dirX = intDir.x;
                        float dirY = intDir.z;

                        float ABmagnitude = SomeMath.Magnitude(dirX, dirY);
                        if (ABmagnitude == 0f) {//length of line are 0
                            result = new Vector2(intPos.x, intPos.y).normalized * agent.maxAgentVelocity;
                            return true;
                        }

                        dirX = dirX / ABmagnitude;
                        dirY = dirY / ABmagnitude;

                        float mul = SomeMath.Dot(-intPos.x, -intPos.z, dirX, dirY);
                        //float nearestX = intPos.x + dirX * mul;
                        //float nearestY = intPos.z + dirY * mul;
                        Vector2 nearest = new Vector2(intPos.x + dirX * mul, intPos.z + dirY * mul);

                        float nearestMagnitude = SomeMath.Magnitude(nearest);
                        float maxVelocity = agent.maxAgentVelocity;

                        if (nearestMagnitude >= maxVelocity) {
                            result = nearest.normalized * agent.maxAgentVelocity;
                        }
                        else {
                            float vectorLength = Mathf.Sqrt(SomeMath.Sqr(maxVelocity) - SomeMath.Sqr(nearestMagnitude)); //hail to pythagoras
                            result = new Vector2(nearest.x, nearest.y) + (new Vector2(intDir.x, intDir.z).normalized * vectorLength);
                        }

                        //Debuger_K.AddDot(offset + new Vector3(nearest.x, 0, nearest.y), Color.magenta);
                        //Debuger_K.AddDot(offset + new Vector3(result.x, 0, result.y), Color.magenta);
                        //Debuger_K.AddLine(agent.positionVector3, agent.positionVector3 + ToV3(result), Color.green);            
                    }
                    else {
                        //orca normals are paralel. this is some kind of unicorn case but it still possible
                        //at least one node are outside. now we must check bouth nodes to see if bouth nodes look outside.
                        //if bouth then sellect farthest one
                        //else sellect one pointing outside

                        float dotOfA = SomeMath.Dot(orcaA.position, orcaA.normal);
                        float dotOfB = SomeMath.Dot(orcaB.position, orcaB.normal);
                        ORCAline? orcaLine = null;

                        if (dotOfA > 0) {
                            orcaLine = orcaA;
                        }
                        if (dotOfB > 0) {
                            if (orcaLine.HasValue) {
                                if (SomeMath.SqrMagnitude(orcaA.position) < SomeMath.SqrMagnitude(orcaB.position)) {
                                    orcaLine = orcaB;
                                }
                            }
                            else
                                orcaLine = orcaB;
                        }

                        if (orcaLine.HasValue == false) {//what?
                            orcaLine = orcaA;
                            Debug.LogError("how did this happen?");
                        }

                        ORCAline targetOrca = orcaLine.Value;

                        Vector2 case2pos = targetOrca.position;
                        Vector2 case2dir = targetOrca.normal;
                        case1dir = new Vector2(-case2dir.y, case2dir.x);

                        float dirXcase2 = case1dir.x;
                        float dirYcase2 = case1dir.y;

                        float case2ABmagnitude = SomeMath.Magnitude(dirXcase2, dirYcase2);

                        dirXcase2 /= case2ABmagnitude;
                        dirYcase2 /= case2ABmagnitude;

                        float case2mul = SomeMath.Dot(-case2pos.x, -case2pos.y, dirXcase2, dirYcase2);
                        float case2nearestX = case2pos.x + dirXcase2 * case2mul;
                        float case2nearestY = case2pos.y + dirYcase2 * case2mul;
                        result = new Vector2(case2nearestX, case2nearestY);
                        if (SomeMath.SqrMagnitude(result) > maxVelSqr)
                            result = result.normalized * maxVel;

                        //Debuger_K.AddLine(agent.positionVector3, agent.positionVector3 + ToV3(result), Color.green);
                    }
                    return true;
                default://OCRA LINES COUNT > 3
                    float resultX, resultY, resultZ;
                    resultX = resultY = resultZ = 0;
                    for (int i1 = 0; i1 < count; i1++) {//grab 1 vector
                        ORCAline line1 = orcaList[i1];
                        Vector3 line1normal3d = GetNormal3d(line1);

                        for (int i2 = i1; i2 < count; i2++) {
                            if (i2 == i1)
                                continue;

                            ORCAline line2 = orcaList[i2];
                            Vector3 line2normal3d = GetNormal3d(line2);

                            for (int i3 = i1; i3 < count; i3++) {
                                if (i3 == i1 | i3 == i2)
                                    continue;

                                ORCAline line3 = orcaList[i3];
                                Vector3 line3normal3d = GetNormal3d(line3);

                                Vector3 int12pos, int12dir, int13pos, int13dir;
                                if (Math3d.PlanePlaneIntersection(out int12pos, out int12dir, line1normal3d, ToV3(line1.position), line2normal3d, ToV3(line2.position)) &
                                    Math3d.PlanePlaneIntersection(out int13pos, out int13dir, line1normal3d, ToV3(line1.position), line3normal3d, ToV3(line3.position))) {

                                    //Debuger_K.AddRay(offset + int12pos, int12dir, Color.red, 50f);
                                    //Debuger_K.AddRay(offset + int12pos, int12dir, Color.red, -50f);
                                    //Debuger_K.AddRay(offset + int13pos, int13dir, Color.red, 50f);
                                    //Debuger_K.AddRay(offset + int13pos, int13dir, Color.red, -50f);

                                    Vector3 intersection;
                                    if (LineLineIntersection(out intersection, int12pos, int12dir, int13pos, int13dir)) {
                                        bool flag = true;

                                        for (int i4 = 0; i4 < count; i4++) {
                                            if (i4 == i1 | i4 == i2 | i4 == i3)
                                                continue;

                                            ORCAline line4 = orcaList[i4];
                                            Vector3 line4normal3d = GetNormal3d(line4);

                                            Vector3 intersectionLocal = intersection - ToV3(line4.position);

                                            float dot = SomeMath.Dot(intersectionLocal, line4normal3d);

                                            if (dot < 0) {
                                                flag = false;
                                                break;
                                            }
                                        }

                                        if (flag) {
                                            //Debuger_K.AddDot(offset + intersection, Color.magenta);

                                            if (haveResult) {
                                                if (resultY > intersection.y) {
                                                    resultX = intersection.x;
                                                    resultY = intersection.y;
                                                    resultZ = intersection.z;
                                                }
                                            }
                                            else {
                                                haveResult = true;
                                                resultX = intersection.x;
                                                resultY = intersection.y;
                                                resultZ = intersection.z;
                                            }
                                        }
                                    }
                                }
                            }                   
                        }
                    }
                    result = new Vector2(resultX, resultZ);
                    if (SomeMath.SqrMagnitude(result) > maxVelSqr)
                        result = result.normalized * maxVel;
                    break;
            }

            //if (haveResult) {
            //    if(DEBUG_VECTOR.HasValue == false)
            //        DEBUG_VECTOR = result;
            //}
            //if (haveResult)
            //    Debuger_K.AddDot(offset + new Vector3(resultX, 0, resultZ), Color.cyan, 0.05f);

      
            return haveResult;
        }

        private static Vector3 GetNormal3d(ORCAline line) {      
            float rad = (Mathf.Clamp01(line.responcibility) * 90f) * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad); 
            return new Vector3(line.normalX * x, Mathf.Sin(rad), line.normalY * x);
        }

#if UNITY_EDITOR
        private static void DrawPlaneORCA(ORCAline line, Vector3 offset) {
            //float angle = Mathf.Clamp01(line.responcibility) * 90f;
            //float rad = angle * Mathf.Deg2Rad;

            Vector2 normal = line.normal;
            Vector2 position = line.position;

            //float x = Mathf.Cos(rad);
            //float y = Mathf.Sin(rad);

            //Vector2 normalTrim = normal * x;
  
            Vector3 normalV3 = GetNormal3d(line);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            Debuger_K.AddDot(offset + positionV3, Color.green, 0.1f);
            Debuger_K.AddRay(offset + positionV3, normalV3, Color.green);

            //Vector2 leftV2 = new Vector2(-normal.y, normal.x);
            Vector3 leftV3 = new Vector3(-normal.y, 0, normal.x);

            //Vector2 rightV2 = new Vector2(normal.y, -normal.x);
            Vector3 rightV3 = new Vector3(normal.y, 0, -normal.x);

            Vector3 crossL = Vector3.Cross(normalV3, leftV3);
            Vector3 crossR = Vector3.Cross(normalV3, rightV3);

            //Debuger_K.AddRay(offset + positionV3, leftV3, Color.blue);
            //Debuger_K.AddRay(offset + positionV3, rightV3, Color.blue);
            //Debuger_K.AddRay(offset + positionV3, crossL, Color.blue);
            //Debuger_K.AddRay(offset + positionV3, crossR, Color.blue);

            Vector3 a = offset + positionV3 + DrawPlaneORCA_shortcut(leftV3, crossL);
            Vector3 b = offset + positionV3 + DrawPlaneORCA_shortcut(rightV3, crossL);
            Vector3 c = offset + positionV3 + DrawPlaneORCA_shortcut(rightV3, crossR);         
            Vector3 d = offset + positionV3 + DrawPlaneORCA_shortcut(leftV3, crossR);

            Debuger_K.AddLine(offset + positionV3, a, Color.red);
            Debuger_K.AddLine(offset + positionV3, b, Color.red);
            Debuger_K.AddLine(offset + positionV3, c, Color.red);
            Debuger_K.AddLine(offset + positionV3, d, Color.red);

            Debuger_K.AddTriangle(a, b, c, new Color(0f, 0f, 1f, 0.1f));
            Debuger_K.AddTriangle(a, c, d, new Color(0f, 0f, 1f, 0.1f));
            Debuger_K.AddLine(a, b);
            Debuger_K.AddLine(b, c);
            Debuger_K.AddLine(c, d);
            Debuger_K.AddLine(d, a);
        }
#endif

        private static Vector3 DrawPlaneORCA_shortcut(Vector3 A, Vector3 B) {
            return ((A + B) * 0.5f) * 2;
        }

        public static bool LineLineIntersection(
            out Vector3 intersection,
            Vector3 linePoint1, Vector3 lineVec1,
            Vector3 linePoint2, Vector3 lineVec2) {

            intersection = Vector3.zero;

            Vector3 lineVec3 = linePoint2 - linePoint1;
            Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
            Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

            float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

            //Lines are not coplanar. Take into account rounding errors.
            if ((planarFactor >= 0.00001f) || (planarFactor <= -0.00001f)) {
                return false;
            }

            //Note: sqrMagnitude does x*x+y*y+z*z on the input vector.
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }
        #endregion

        private static Vector2 GetTargetVector2(float radianPos, float length) {
            return new Vector2(Mathf.Cos(radianPos) * length, Mathf.Sin(radianPos) * length);
        }

        static Vector3[] DrawCircle(int value, Vector3 position, float radius) {
            Vector3[] result = new Vector3[value];
            for (int i = 0; i < value; ++i) {
                result[i] = new Vector3(
                    (float)Math.Cos(i * 2.0f * Math.PI / value) * radius + position.x,
                    position.y,
                    (float)Math.Sin(i * 2.0f * Math.PI / value) * radius + position.z);
            }
            return result;
        }
        static Vector3 ToV3(Vector2 pos) {
            return new Vector3(pos.x, 0, pos.y);
        }
        static Vector2 ToV2(Vector3 pos) {
            return new Vector2(pos.x, pos.z);
        }
        private static Vector2 GetTargetVector(float angle, float length) {
            return new Vector2(
                (float)Math.Cos(angle * Math.PI / 180) * length,
                (float)Math.Sin(angle * Math.PI / 180) * length);
        }

        static Vector2 RadToVector(float radian) {
            return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
        }

        private class RVOAllocatedData {
            //raycast related
            public RaycastHitNavMesh2[] raycastResult = new RaycastHitNavMesh2[0];
            public float[] rayLengthTemp = new float[RAY_COUNT];

            //orca lines related
            public List<ORCAline> ORCAlines = new List<ORCAline>();

            //shape related
            static float[] defaultX, defaultY;
            public List<Vector2> shape = new List<Vector2>();
            public List<float> cross = new List<float>();     

            static RVOAllocatedData() {
                //create static default shape
                defaultX = new float[9];
                defaultY = new float[9];

                for (int p = 0; p < PathFinderMainRVO.SHAPE_EDGES_COUNT; ++p) {
                    defaultX[p] = Mathf.Cos(p * 2f * Mathf.PI / PathFinderMainRVO.SHAPE_EDGES_COUNT);
                    defaultY[p] = Mathf.Sin(p * 2f * Mathf.PI / PathFinderMainRVO.SHAPE_EDGES_COUNT);  
                }

                defaultX[PathFinderMainRVO.SHAPE_EDGES_COUNT] = defaultX[0];
                defaultY[PathFinderMainRVO.SHAPE_EDGES_COUNT] = defaultY[0];
            }

            #region shape
            public void ResetShape(float distance) {
                shape.Clear(); 
                for (int i = 0; i < PathFinderMainRVO.SHAPE_EDGES_COUNT; i++) {
                    shape.Add(new Vector2(defaultX[i] * distance, defaultY[i] * distance));
                }
                shape.Add(shape[0]);
            }

            public void ResetCross() {
                cross.Clear();
            }

            #region debug
#if UNITY_EDITOR
            public void DrawCurrentShape(Vector3 offset, Color shapeColor, Color outlineColor) {
                if (shape.Count < 2)
                    return;
           
                Vector3[] debugShape = new Vector3[shape.Count];

                for (int v = 0; v < shape.Count; v++) {
                    debugShape[v] = new Vector3(shape[v].x, 0, shape[v].y) + offset;
                }
                Debuger_K.AddLine(debugShape, outlineColor);

                Vector3 mid = SomeMath.MidPoint(debugShape);

                for (int v = 0; v < debugShape.Length - 1; v++) {
                    Debuger_K.AddTriangle(mid, debugShape[v], debugShape[v + 1], shapeColor, false);
                }
                Debuger_K.AddTriangle(mid, debugShape[0], debugShape[debugShape.Length - 1], shapeColor, false);
            }
#endif
            #endregion
            #endregion
        }

        /// <summary>
        /// class for storing 2D ray prefabs
        /// </summary>
        private class RVORayDataSet {
            public Vector2[] vectorArray;
            public float[] lengthArray;
            public RVORayDataSet(Vector2[] vectorArray, float[] lengthArray) {
                this.vectorArray = vectorArray;
                this.lengthArray = lengthArray;
            }
        }

        /// <summary>
        /// struct for storing 2D ray that represent velocity border
        /// </summary>
        struct ORCAline {
            public readonly float positionX, positionY;
            public readonly float normalX, normalY;
            public readonly float responcibility; //represent responcibility of current agent. formula is: responcibility of current agent / summ of this and other agent responcbility

            public ORCAline(float posX, float posY, float normX, float normY, float resp) {
                positionX = posX;
                positionY = posY;
                normalX = normX;
                normalY = normY;
                responcibility = resp;

                if(float.IsNaN(normX) | float.IsInfinity(normX))
                    throw new System.ArgumentException("Parameter invalid", "normX");
                if (float.IsNaN(normY) | float.IsInfinity(normY))
                    throw new System.ArgumentException("Parameter invalid", "normY");
            }
            public ORCAline(Vector2 pos, Vector2 norm, float resp) : this(pos.x, pos.y, norm.x, norm.y, resp) { }

            public Vector2 position { get { return new Vector2(positionX, positionY); } }
            public Vector2 normal { get { return new Vector2(normalX, normalY); } }
        }

        /// <summary>
        /// struct to store 3d ray
        /// </summary>
        struct RayStruct {
            public readonly float positionX, positionY, positionZ;
            public readonly float directionX, directionY, directionZ;

            public RayStruct(float posX, float posY, float posZ, float dirX, float dirY, float dirZ) {
                positionX = posX;
                positionY = posY;
                positionZ = posZ;

                directionX = dirX;
                directionY = dirY;
                directionZ = dirZ;
            }

            public RayStruct(Vector3 pos, Vector3 dir) : this(pos.x, pos.y, pos.z, dir.x, dir.y, dir.z) { }

            public Vector3 position {
                get { return new Vector3(positionX, positionY, positionZ); }
            }

            public Vector3 direction {
                get { return new Vector3(directionX, directionY, directionZ); }
            }
        }
    }
}
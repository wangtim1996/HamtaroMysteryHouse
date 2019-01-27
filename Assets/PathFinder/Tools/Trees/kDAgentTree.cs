using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace K_PathFinder.Trees {
    public static class kDAgentTree {
        private const int AGENTS_PER_BRANCH = 10;
        private static ManualResetEvent[] searchResetEvent;

        private static List<kDTreeAgent> agents = new List<kDTreeAgent>(128);
        private static List<kDTreeBranch> branches = new List<kDTreeBranch>();

        private static int root;
        private static ComparerHolderX holderX = new ComparerHolderX();
        private static ComparerHolderY holderY = new ComparerHolderY();
        private static float minX, minY, maxX, maxY;

        public static void BuildTree() {
            agents.Clear();
            branches.Clear();

            var normalAgents = PathFinder.agents;
            int count = normalAgents.Count;

            if (count < 2)
                return;

            Vector3 firstPos = normalAgents[0].positionVector3;
            minX = firstPos.x;
            minY = firstPos.z;
            maxX = firstPos.x;
            maxY = firstPos.z;

            for (int i = 0; i < count; i++) {
                Vector3 pos = normalAgents[i].positionVector3;
                agents.Add(new kDTreeAgent(i, pos.x, pos.z));
                minX = Mathf.Min(minX, pos.x);
                minY = Mathf.Min(minY, pos.z);
                maxX = Mathf.Max(maxX, pos.x);
                maxY = Mathf.Max(maxY, pos.z);
            }

            try {
                //System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                //stopWatch.Start();
                root = BuildRecursive(0, count, 0, minX, minY, maxX, maxY);
                //stopWatch.Stop();
                //Debug.Log(stopWatch.Elapsed);
                //Debug.Log(branches.Count + " branches");
            }
            catch (Exception e) {
                Debug.Log(branches.Count);
                Debug.LogError(e);
                throw;
            }
        }
        //return index of branch
        private static int BuildRecursive(int start, int end, int depth, float minX, float minY, float maxX, float maxY) {
            float value = 0;
            int count = end - start;

            if (count <= AGENTS_PER_BRANCH) {
                branches.Add(new kDTreeBranch(start, end, depth, -1, -1, minX, minY, maxX, maxY));
            }
            else {
                if (depth % 2 == 0) {//true = X, false = Y
                    agents.Sort(start, count, holderX);

                    for (int i = start; i < end; i++) { value += agents[i].x; }
                    value /= count;

                    int borderIndex = 0;

                    for (int i = start; i < end; i++) {
                        if (agents[i].x > value) {
                            borderIndex = i;
                            break;
                        }
                    }

                    int b1 = BuildRecursive(start, borderIndex, depth + 1, minX, minY, value, maxY);
                    int b2 = BuildRecursive(borderIndex, end, depth + 1, value, minY, maxX, maxY);
                    branches.Add(new kDTreeBranch(start, end, depth, b1, b2, minX, minY, maxX, maxY));
                }
                else {//y
                    agents.Sort(start, count, holderY);

                    for (int i = start; i < end; i++) { value += agents[i].y; }
                    value /= count;

                    int borderIndex = 0;

                    for (int i = start; i < end; i++) {
                        if (agents[i].y > value) {
                            borderIndex = i;
                            break;
                        }
                    }

                    int b1 = BuildRecursive(start, borderIndex, depth + 1, minX, minY, maxX, value);
                    int b2 = BuildRecursive(borderIndex, end, depth + 1, minX, value, maxX, maxY);
                    branches.Add(new kDTreeBranch(start, end, depth, b1, b2, minX, minY, maxX, maxY));
                }
            }

            return branches.Count - 1;
        }

        public static void FindNearestAgent(PathFinderAgent agent) {
            if (agent.updateNeighbourAgents == false)
                return;

            agent.neighbourAgents.Clear();
            agent.neighbourSqrDistances.Clear();
            float radius = agent.maxNeighbourDistance;
            float radisuSqr = SomeMath.Sqr(radius);
            Vector3 pos = agent.positionVector3;
            Bounds2D targetBounds = new Bounds2D(pos.x, pos.z, radius);
            SearchRecursive(agent, targetBounds, pos, radisuSqr, root);
        }

        private static void SearchRecursive(PathFinderAgent agent, Bounds2D targetBounds, Vector3 targetPos, float targetSqrDist, int targetBranch) {
            kDTreeBranch branch = branches[targetBranch];

            if (branch.bounds.Overlap(targetBounds)) {
                if (branch.branchA != -1) {
                    SearchRecursive(agent, targetBounds, targetPos, targetSqrDist, branch.branchA);
                    SearchRecursive(agent, targetBounds, targetPos, targetSqrDist, branch.branchB);
                }
                else {
                    for (int i = branch.start; i < branch.end; i++) {
                        var realAgent = PathFinder.agents[agents[i].index];
                        if (agent != realAgent) {
                            float curSqrDist = SomeMath.SqrDistance(targetPos, realAgent.positionVector3);
                            if (curSqrDist < targetSqrDist) {
                                if (agent.neighbourAgents.Count < agent.maxNeighbors) {
                                    agent.neighbourAgents.Add(realAgent);
                                    agent.neighbourSqrDistances.Add(curSqrDist);
                                }
                                else {
                                    for (int n = 0; n < agent.maxNeighbors; n++) {
                                        if (agent.neighbourSqrDistances[n] > curSqrDist) {
                                            agent.neighbourAgents[n] = realAgent;
                                            agent.neighbourSqrDistances[n] = curSqrDist;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void FindNearestAgents() {
            for (int i = 0; i < PathFinder.agents.Count; i++) {
                FindNearestAgent(PathFinder.agents[i]);
            }
        }

        public static void FindNearestAgentsAsync(int maxThreads) {
            if (searchResetEvent == null || searchResetEvent.Length != maxThreads) {
                searchResetEvent = new ManualResetEvent[maxThreads];
                for (int i = 0; i < maxThreads; i++) { searchResetEvent[i] = new ManualResetEvent(true); }
            }

            int curIndex = 0;
            int agentsPerThread = (agents.Count / maxThreads) + 1;

            for (int i = 0; i < maxThreads; i++) {
                int end = curIndex + agentsPerThread;

                if (end >= agents.Count) {
                    end = agents.Count;
                    searchResetEvent[i].Reset();
                    ThreadPool.QueueUserWorkItem(AsyncSearchThreadPoolCallback, new UpdateNearestAgentsThreadContext(curIndex, end, searchResetEvent[i]));
                    break;
                }
                else {
                    searchResetEvent[i].Reset();
                    ThreadPool.QueueUserWorkItem(AsyncSearchThreadPoolCallback, new UpdateNearestAgentsThreadContext(curIndex, end, searchResetEvent[i]));
                }

                curIndex = end;
            }
            WaitHandle.WaitAll(searchResetEvent);
        }

        private static void AsyncSearchThreadPoolCallback(object threadContext) {
            try {
                UpdateNearestAgentsThreadContext contex = (UpdateNearestAgentsThreadContext)threadContext;
                for (int i = contex.threadStart; i < contex.threadEnd; i++) { FindNearestAgent(PathFinder.agents[i]); }
                contex.manualResetEvent.Set();
            }
            catch (Exception e) {
                Debug.LogErrorFormat("Error occure while searching nearest agent: {0}", e);
                throw;
            }
        }

        //********************inner types********************//

        private struct kDTreeBranch {
            public readonly int start, end, depth, branchA, branchB;
            public readonly Bounds2D bounds;

            public kDTreeBranch(int start, int end, int depth, int branchA, int branchB, float minX, float minY, float maxX, float maxY) {
                this.start = start;
                this.end = end;
                this.depth = depth;
                this.branchA = branchA;
                this.branchB = branchB;
                bounds = new Bounds2D(minX, minY, maxX, maxY);
            }
        }
        private struct kDTreeAgent {
            public readonly int index;
            public readonly float x, y;

            public kDTreeAgent(int ID, float X, float Y) {
                index = ID;
                x = X;
                y = Y;
            }

            public Vector2 pos {
                get { return new Vector2(x, y); }
            }
        }

        private struct UpdateNearestAgentsThreadContext {
            public readonly int threadStart, threadEnd;
            public readonly ManualResetEvent manualResetEvent;
            public UpdateNearestAgentsThreadContext(int start, int end, ManualResetEvent resetEvent) {
                threadStart = start;
                threadEnd = end;
                manualResetEvent = resetEvent;
            }
        }

        private class ComparerHolderX : IComparer<kDTreeAgent> {
            public int Compare(kDTreeAgent agent1, kDTreeAgent agent2) {
                if (agent1.x == agent2.x)
                    return 0;
                if (agent1.x - agent2.x > 0)
                    return 1;
                else
                    return -1;
            }
        }
        private class ComparerHolderY : IComparer<kDTreeAgent> {
            public int Compare(kDTreeAgent agent1, kDTreeAgent agent2) {
                if (agent1.y == agent2.y)
                    return 0;
                if (agent1.y - agent2.y > 0)
                    return 1;
                else
                    return -1;
            }
        }
    }
}
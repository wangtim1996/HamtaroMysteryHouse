using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

//serve more as prototype but can be userful later on
namespace K_PathFinder.Trees {
    public interface IKDTreeMember<T> {
        Vector2 position { get; }
        List<T> neighbourAgents { get; }
        List<float> neighbourSqrDistances { get; }
        float maxNeighbours { get; }
        float maxRadius { get; }
    }

    public class Generic2dKDTree<T> where T : IKDTreeMember<T>, IEquatable<T> {
        private static ComparerHolderX holderX = new ComparerHolderX();
        private static ComparerHolderY holderY = new ComparerHolderY();
        private ManualResetEvent[] searchResetEvent;

        private int membersPerBranch;
        private List<T> data;

        private int root;
        private float minX, minY, maxX, maxY;
        private static List<kDTreeMember> members = new List<kDTreeMember>(64);
        private static List<kDTreeBranch> branches = new List<kDTreeBranch>();

        public Generic2dKDTree() { }
        public Generic2dKDTree(List<T> membersList) {
            SetData(membersList);
        }

        public void SetData(List<T> collection) {
            data = collection;
        }

        public void BuildTree(int membersPerBranch = 10) {
            members.Clear();
            branches.Clear();

            int count = data.Count;

            if (count < 2)
                return;

            this.membersPerBranch = Mathf.Max(1, membersPerBranch);

            Vector2 firstPos = data[0].position;
            minX = firstPos.x;
            minY = firstPos.y;
            maxX = firstPos.x;
            maxY = firstPos.y;

            for (int i = 0; i < count; i++) {
                Vector2 pos = data[i].position;
                members.Add(new kDTreeMember(i, pos.x, pos.y));
                minX = Mathf.Min(minX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxX = Mathf.Max(maxX, pos.x);
                maxY = Mathf.Max(maxY, pos.y);
            }

            root = BuildRecursive(0, count, 0, minX, minY, maxX, maxY);
        }

        //return index of branch
        private int BuildRecursive(int start, int end, int depth, float minX, float minY, float maxX, float maxY) {
            float value = 0;
            int count = end - start;

            if (count < membersPerBranch) {
                branches.Add(new kDTreeBranch(start, end, depth, -1, -1, minX, minY, maxX, maxY));
            }
            else {
                if (depth % 2 == 0) {//true = X, false = Y
                    members.Sort(start, count, holderX);

                    for (int i = start; i < end; i++) { value += members[i].x; }
                    value /= count;

                    int borderIndex = 0;

                    for (int i = start; i < end; i++) {
                        if (members[i].x > value) {
                            borderIndex = i;
                            break;
                        }
                    }

                    int b1 = BuildRecursive(start, borderIndex, depth + 1, minX, minY, value, maxY);
                    int b2 = BuildRecursive(borderIndex, end, depth + 1, value, minY, maxX, maxY);
                    branches.Add(new kDTreeBranch(start, end, depth, b1, b2, minX, minY, maxX, maxY));
                }
                else {//y
                    members.Sort(start, count, holderY);

                    for (int i = start; i < end; i++) { value += members[i].y; }
                    value /= count;

                    int borderIndex = 0;

                    for (int i = start; i < end; i++) {
                        if (members[i].y > value) {
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

        public void FindNearest(T target) {
            target.neighbourAgents.Clear();
            target.neighbourSqrDistances.Clear();
            float radius = target.maxRadius;
            float radisuSqr = SomeMath.Sqr(radius);
            Vector2 pos = target.position;
            Bounds2D targetBounds = new Bounds2D(pos.x, pos.y, radius);

            SearchRecursive(target, targetBounds, pos, radisuSqr, root);
        }
        public void FindNearestAll() {
            for (int i = 0; i < data.Count; i++) {
                FindNearest(data[i]);
            }
        }
        public void FindNearestAllAsync(int maxThreads) {
            if (searchResetEvent == null || searchResetEvent.Length != maxThreads) {
                searchResetEvent = new ManualResetEvent[maxThreads];
                for (int i = 0; i < maxThreads; i++) {
                    searchResetEvent[i] = new ManualResetEvent(true);
                }
            }

            int curIndex = 0;
            int agentsPerThread = (members.Count / maxThreads) + 1;

            for (int i = 0; i < maxThreads; i++) {
                int end = curIndex + agentsPerThread;

                if (end >= members.Count) {
                    end = members.Count;
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

        private void AsyncSearchThreadPoolCallback(System.Object threadContext) {
            try {
                UpdateNearestAgentsThreadContext contex = (UpdateNearestAgentsThreadContext)threadContext;

                for (int i = contex.threadStart; i < contex.threadEnd; i++) {
                    FindNearest(data[i]);
                }

                contex.manualResetEvent.Set();
            }
            catch (Exception e) {
                Debug.LogErrorFormat("Error occure while searching nearest agent: {0}", e);
                throw;
            }
        } 

        private void SearchRecursive(T target, Bounds2D targetBounds, Vector2 targetPos, float targetSqrDist, int targetBranch) {
            kDTreeBranch branch = branches[targetBranch];

            if (branch.bounds.Overlap(targetBounds)) {
                if (branch.branchA != -1) {
                    SearchRecursive(target, targetBounds, targetPos, targetSqrDist, branch.branchA);
                    SearchRecursive(target, targetBounds, targetPos, targetSqrDist, branch.branchB);
                }
                else {
                    for (int i = branch.start; i < branch.end; i++) {
                        T realAgent = data[members[i].index];
                        if (target.Equals(realAgent) == false) {
                            float curSqrDist = SomeMath.SqrDistance(targetPos, realAgent.position);
                            if (curSqrDist < targetSqrDist) {
                                if (target.neighbourAgents.Count < target.maxNeighbours) {
                                    target.neighbourAgents.Add(realAgent);
                                    target.neighbourSqrDistances.Add(curSqrDist);
                                }
                                else {
                                    for (int n = 0; n < target.maxNeighbours; n++) {
                                        if (target.neighbourSqrDistances[n] > curSqrDist) {
                                            target.neighbourAgents[n] = realAgent;
                                            target.neighbourSqrDistances[n] = curSqrDist;
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
        
        //main data structs
        private struct kDTreeMember {
            public readonly int index;
            public readonly float x, y;

            public kDTreeMember(int ID, float X, float Y) {
                index = ID;
                x = X;
                y = Y;
            }

            public Vector2 pos {
                get { return new Vector2(x, y); }
            }
        }
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

        private class ComparerHolderX : IComparer<kDTreeMember> {
            public int Compare(kDTreeMember agent1, kDTreeMember agent2) {
                if (agent1.x == agent2.x)
                    return 0;
                if (agent1.x - agent2.x > 0)
                    return 1;
                else
                    return -1;
            }
        }
        private class ComparerHolderY : IComparer<kDTreeMember> {
            public int Compare(kDTreeMember agent1, kDTreeMember agent2) {
                if (agent1.y == agent2.y)
                    return 0;
                if (agent1.y - agent2.y > 0)
                    return 1;
                else
                    return -1;
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
    }
}

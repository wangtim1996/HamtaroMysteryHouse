using K_PathFinder.PFDebuger;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//WIP

namespace K_PathFinder.Trees {
    public interface IQuadTreeMember {
        Bounds2D bounds { get; }
    }

    public class GenericQuadTree<T> where T : IQuadTreeMember {
        Stack<QuadTreeBranch> pool;
        List<QuadTreeBranch> inUse = new List<QuadTreeBranch>();
        List<T> data = new List<T>();

        QuadTreeBranch root;
        private int membersPerBranch;

        public GenericQuadTree(int startPoolCount = 100) {
            pool = new Stack<QuadTreeBranch>(startPoolCount);
            for (int i = 0; i < startPoolCount; i++) {
                pool.Push(new QuadTreeBranch());
            }
            inUse = new List<QuadTreeBranch>();
        }

        public void SetData(List<T> data) {
            this.data.Clear();
            foreach (var item in data) {
                this.data.Add(item);
            }
        }

        public void BuildTree(int membersPerBranch = 2) {
            this.membersPerBranch = Mathf.Max(1, membersPerBranch);

            foreach (var item in inUse) { ReturnBranch(item); }
            inUse.Clear();

            //find current bonds
            if (data.Count == 0)
                return;

            Bounds2D curBounds = data[0].bounds;

            float minX = curBounds.minX;
            float minY = curBounds.minY;
            float maxX = curBounds.maxX;
            float maxY = curBounds.maxY;

            for (int i = 1; i < data.Count; i++) {
                curBounds = data[i].bounds;
                minX = Mathf.Min(curBounds.minX, minX);
                minY = Mathf.Min(curBounds.minY, minY);
                maxX = Mathf.Max(curBounds.maxX, maxX);
                maxY = Mathf.Max(curBounds.maxY, maxY);
            }

            root = GetFreeBranch();
            root.list.AddRange(data);
            root.bounds = new Bounds2D(minX, minY, maxX, maxY);
            root.depth = 0;
            BuildRecursive(root);
        }

        internal void Clear() {
            data.Clear();
            foreach (var item in inUse) {
                ReturnBranch(item);
            }
            inUse.Clear();
        }

        public void Search(Bounds2D bounds, ref List<T> result) {
            result.Clear();
            SearchRecursive(root, bounds, result);
        }

        private void SearchRecursive(QuadTreeBranch curBranch, Bounds2D bounds, List<T> result) {
            if (curBranch == null)
                return;

            Bounds2D branchBounds = curBranch.bounds;

            if (bounds.Overlap(branchBounds) == false)
                return;

            for (int i = 0; i < curBranch.list.Count; i++) {
                if (bounds.Overlap(curBranch.list[i].bounds))
                    result.Add(curBranch.list[i]);
            }

            if (curBranch.branches[0] != null) SearchRecursive(curBranch.branches[0], bounds, result);
            if (curBranch.branches[1] != null) SearchRecursive(curBranch.branches[1], bounds, result);
            if (curBranch.branches[2] != null) SearchRecursive(curBranch.branches[2], bounds, result);
            if (curBranch.branches[3] != null) SearchRecursive(curBranch.branches[3], bounds, result);
        }

        private void BuildRecursive(QuadTreeBranch target) {
            if (target.count <= membersPerBranch)
                return;

            if (target.depth > 5)
                return;

            Bounds2D curBounds = target.bounds;
            float centerX = curBounds.centerX;
            float centerY = curBounds.centerY;

            int e = 0;
            for (int i = 0; i < target.list.Count; i++) {
                if (target.list[i].bounds.Overlap(centerX, centerY) == false)
                    e++;
            }

            if (e < membersPerBranch)
                return;

            QuadTreeBranch[] branches = target.branches;
            branches[0] = GetFreeBranch();
            branches[1] = GetFreeBranch();
            branches[2] = GetFreeBranch();
            branches[3] = GetFreeBranch();

            branches[0].depth = target.depth + 1;
            branches[1].depth = target.depth + 1;
            branches[2].depth = target.depth + 1;
            branches[3].depth = target.depth + 1;

            branches[0].bounds = new Bounds2D(curBounds.minX, curBounds.minY, centerX, centerY);
            branches[1].bounds = new Bounds2D(curBounds.minX, centerY, centerX, curBounds.maxY);
            branches[2].bounds = new Bounds2D(centerX, curBounds.minY, curBounds.maxX, centerY);
            branches[3].bounds = new Bounds2D(centerX, centerY, curBounds.maxX, curBounds.maxY);

            T val;      

            for (int i = target.list.Count - 1; i >= 0; i--) {
                val = target.list[i];

                if (branches[0].bounds.Enclose(val.bounds)) {
                    branches[0].list.Add(val);
                    target.list.RemoveAt(i);
                }
                if (branches[1].bounds.Enclose(val.bounds)) {
                    branches[1].list.Add(val);
                    target.list.RemoveAt(i);
                }
                if (branches[2].bounds.Enclose(val.bounds)) {
                    branches[2].list.Add(val);
                    target.list.RemoveAt(i);
                }
                if (branches[3].bounds.Enclose(val.bounds)) {
                    branches[3].list.Add(val);
                    target.list.RemoveAt(i);
                }
            }

            BuildRecursive(branches[0]);
            BuildRecursive(branches[1]);
            BuildRecursive(branches[2]);
            BuildRecursive(branches[3]);
        }

        private QuadTreeBranch GetFreeBranch() {
            if (pool.Count > 0)
                return pool.Pop();
            return new QuadTreeBranch();
        }

        private void ReturnBranch(QuadTreeBranch branch) {
            branch.Clear();
            pool.Push(branch);
        }

#if UNITY_EDITOR
        public void DrawTree() {
            DrawBranchRecursive(root);
        }

        void DrawBranchRecursive(QuadTreeBranch branch, float offset = 0f, float offsetDelta = 0f) {
            if (branch == null)
                return;

            Bounds2D branchBounds = branch.bounds;
            Debuger_K.AddBounds(branchBounds, Color.green);

            Vector3 curPos = branchBounds.centerVector3;

            for (int i = 0; i < branch.list.Count; i++) {
                Bounds2D countentBounds = branch.list[i].bounds;
                Debuger_K.AddBounds(countentBounds, Color.blue);
                Debuger_K.AddLine(curPos, countentBounds.centerVector3, Color.magenta);
            }

            for (int i = 0; i < 4; i++) {
                if(branch.branches[i] != null) {
                    DrawBranchRecursive(branch.branches[i]);
                }
            }
        }
#endif


        private struct QuadTreeInternalData {
            float x, y;
            int index;
        }

        private class QuadTreeBranch {
            public QuadTreeBranch root;
            public int depth;
            public Bounds2D bounds;
            public QuadTreeBranch[] branches = new QuadTreeBranch[4];
            public List<T> list = new List<T>();

            public void Add(T value) {
                list.Add(value);
            }

            public void Clear() {
                root = null;
                for (int i = 0; i < 4; i++) {
                    branches[i] = null;
                }
                list.Clear();
            }

            public int count {
                get { return list.Count; }
            }
        }
    }
}

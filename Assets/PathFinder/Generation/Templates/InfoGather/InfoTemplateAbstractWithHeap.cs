using K_PathFinder.Graphs;
using K_PathFinder.PFTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.PathGeneration {
    public abstract class InfoTemplateAbstractWithHeap : IThreadPoolWorkBatcherMember, IObjectPoolMember {
        //heap
        List<Node> heap = new List<Node>();
        protected int heapCount = 0;


        public abstract void PerformWork(object context);
        public virtual void Clear() {
            heapCount = 0;
            heap.Clear();
        }

        //***************************************HEAP***************************************//
        #region HEAP
        private int HeapNodeCompare(Node left, Node right) {
            if (left.gh < right.gh)
                return 1;
            if (left.gh > right.gh)
                return -1;
            return 0;
        }

        protected void HeapAdd(Node value) {
            if (heap.Count == heapCount)
                heap.Add(value);
            else
                heap[heapCount] = value;

            HeapSortUp(heapCount);
            heapCount++;
        }

        protected Node HeapRemoveFirst() {
            Node first = heap[0];
            heapCount--;
            heap[0] = heap[heapCount];
            HeapSortDown(0);
            return first;
        }

        private void HeapSortUp(int index) {
            if (index == 0) return;
            Node item = heap[index];
            int parentIndex;

            int debigBreak = 0;
            while (true) {
                debigBreak++;
                if (debigBreak > 1000) {
                    Debug.LogError("debigBreak > 1000");
                    break;
                }

                parentIndex = (index - 1) / 2;
                Node parentItem = heap[parentIndex];

                if (HeapNodeCompare(item, parentItem) > 0) {
                    HeapSwap(index, parentIndex);
                    index = parentIndex;
                }
                else
                    break;
            }
        }

        private void HeapSortDown(int index) {
            Node item = heap[index];
            int childIndexLeft, childIndexRight, swapIndex;

            int debigBreak = 0;
            while (true) {
                debigBreak++;
                if (debigBreak > 1000) {
                    Debug.LogError("debigBreak > 1000");
                    break;
                }

                childIndexLeft = index * 2 + 1;
                childIndexRight = index * 2 + 2;
                swapIndex = 0;

                if (childIndexLeft < heapCount) {
                    swapIndex = childIndexLeft;

                    if (childIndexRight < heapCount && HeapNodeCompare(heap[childIndexLeft], heap[childIndexRight]) < 0)
                        swapIndex = childIndexRight;

                    if (HeapNodeCompare(item, heap[swapIndex]) < 0) {
                        HeapSwap(index, swapIndex);
                        index = swapIndex;
                    }
                    else
                        return;
                }
                else
                    return;
            }
        }

        private void HeapSwap(int indexA, int indexB) {
            Node valA = heap[indexA];
            heap[indexA] = heap[indexB];
            heap[indexB] = valA;
        }
        #endregion
        //***************************************HEAP***************************************//


        public struct Node {
            public int root, index;
            public float g, h;
            public CellContent content;

            public Node(int index, int root, float g, float h, CellContent content) {
                this.index = index;
                this.root = root;
                this.g = g;
                this.h = h;
                this.content = content;
            }

            public float gh {
                get { return g + h; }
            }

            public override string ToString() {
                return gh.ToString();
            }
        }
    }
}
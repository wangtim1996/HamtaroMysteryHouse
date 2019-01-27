using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

namespace K_PathFinder.Trees {    
    public class HeapStructed<T> where T : struct, IComparable<T> {
        public List<T> heap = new List<T>();
        public int heapCount = 0;

        public StringBuilder log = new StringBuilder();
        
        private int ParentIndex(int index) {
            return (index - 1) / 2;
        }

        private void ChildIndex(int index, out int left, out int right) {
            left = index * 2 + 1;
            right = index * 2 + 2;
        }

        public void HeapAdd(T value) {
            log.AppendLine(string.Format("added {0}", value));

            if (heap.Count == heapCount)
                heap.Add(value);
            else
                heap[heapCount] = value;
            
            HeapSortUp(heapCount);
            heapCount++;
        }

        public T HeapRemoveFirst() {
            T first = heap[0];

            log.AppendLine(string.Format("removed first {0}", first));

            heapCount--;
            heap[0] = heap[heapCount];        
            HeapSortDown(0);
            return first;
        }

        private void HeapSortUp(int index) {
            if (index == 0) return;

            log.AppendLine(string.Format("sort up {0}", index));

            T item = heap[index];
            int parentIndex;

            while (true) {
                parentIndex = (index - 1) / 2;
                T parentItem = heap[parentIndex];

                if (item.CompareTo(parentItem) > 0) {
                    HeapSwap(index, parentIndex);
                    index = parentIndex;
                }
                else
                    break;
            }
        }

        private void HeapSortDown(int index) {
            log.AppendLine(string.Format("sort down {0}", index));

            T item = heap[index];
            int childIndexLeft, childIndexRight, swapIndex;

            while (true) {
                childIndexLeft = index * 2 + 1;
                childIndexRight = index * 2 + 2;
                swapIndex = 0;

                if (childIndexLeft < heapCount) {
                    swapIndex = childIndexLeft;

                    if (childIndexRight < heapCount && heap[childIndexLeft].CompareTo(heap[childIndexRight]) < 0)
                        swapIndex = childIndexRight;

                    if (item.CompareTo(heap[swapIndex]) < 0) {
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
            log.AppendLine(string.Format("swap {0} : {1}", indexA, indexB));

            T valA = heap[indexA];
            heap[indexA] = heap[indexB];
            heap[indexB] = valA;
        }


        public int count {
            get { return heapCount; }
        }

        public void Clear() {
            heapCount = 0;
            heap.Clear();
        }


        public HeapGUI GetGUI {
            get { return new HeapGUI(this); }
        }
        public class HeapGUI {
            HeapStructed<T> heap;
            List<T> heapValues;

            List<Vector3> debug = new List<Vector3>();
            const float SIZE = 30;
            Vector2 size = new Vector2(SIZE, SIZE);


            public HeapGUI(HeapStructed<T> Heap) {
                heap = Heap;
                heapValues = Heap.heap;
            }

            public void OnGUI() {
                if (heap.count == 0)
                    return;

                debug.Clear();
                for (int h = 0; h < heap.count; h++) {
                    debug.Add(new Vector3());
                }

                float screen = Screen.width;
                debug[0] = new Vector3(SIZE, 0, screen);



                for (int h = 0; h < heap.count; h++) {
                    Vector4 curV4 = debug[h];


                    float height = curV4.x;
                    float start = curV4.y;
                    float end = curV4.z;
                    float x = (start + end) / 2;

                    Vector2 curPos = new Vector2(x - SIZE, height - SIZE);
                    GUI.Box(new Rect(curPos, size * 2f), heapValues[h].ToString());


                    float half = (end - start) * 0.5f;


                    int L, R;
                    ChildIndex(h, out L, out R);

                    if (L < heap.count) {
                        float Lheight = height + SIZE + SIZE;
                        float Ls = start;
                        float Le = Ls + half;

                        debug[L] = new Vector3(Lheight, Ls, Le);
                        if (R < heap.count) {
                            float Rheight = Lheight;
                            float Rs = Le;
                            float Re = end;
                            debug[R] = new Vector4(Rheight, Rs, Re);
                        }
                    }
                }
            }

            private int ParentIndex(int index) {
                return (index - 1) / 2;
            }

            private void ChildIndex(int index, out int left, out int right) {
                left = index * 2 + 1;
                right = index * 2 + 2;
            }
        }

    }


    //public class HeapGeneric<T> where T : IHeapItem<T> {
    //    List<T> items;
    //    int itemCount = 0;

    //    public HeapGeneric(int startCapacity) {
    //        items = new List<T>(startCapacity);
    //    }

    //    public void Add(T item) {
    //        item.HeapIndex = items.Count;
    //        items.Add(item);
    //        SortUp(item);
    //        itemCount++;
    //    }

    //    public T RemoveFirst() {
    //        T first = items[0];
    //        itemCount--;
    //        items[0] = items[itemCount];
    //        items[0].HeapIndex = 0;
    //        SortDown(items[0]);
    //        return first;
    //    }

    //    private void SortDown(T item) {
    //        while (true) {
    //            int childIndexLeft = item.HeapIndex * 2 + 1;
    //            int childIndexRight = item.HeapIndex * 2 + 2;
    //            int swapIndex = 0;
    //            if (childIndexLeft < itemCount) {
    //                swapIndex = childIndexLeft;

    //                if (childIndexRight < itemCount && items[childIndexLeft].CompareTo(items[childIndexRight]) < 0)
    //                    swapIndex = childIndexRight;

    //                if (item.CompareTo(items[swapIndex]) < 0)
    //                    Swap(item, items[swapIndex]);
    //                else
    //                    return;
    //            }
    //            else
    //                return;
    //        }
    //    }

    //    private void SortUp(T item) {
    //        int parentIndex = (item.HeapIndex - 1) / 2;
    //        while (true) {
    //            T parentItem = items[parentIndex];
    //            if (item.CompareTo(parentItem) > 0) {
    //                Swap(item, parentItem);
    //                parentIndex = (item.HeapIndex - 1) / 2;
    //            }
    //            else
    //                break;
    //        }
    //    }

    //    private void Swap(T A, T B) {
    //        items[A.HeapIndex] = B;
    //        items[B.HeapIndex] = A;
    //        int aIndex = A.HeapIndex;
    //        A.HeapIndex = B.HeapIndex;
    //        B.HeapIndex = aIndex;
    //    }

    //    public bool Contains(T item) {
    //        return Equals(items[item.HeapIndex], item);
    //    }

    //    public int count {
    //        get { return itemCount; }
    //    }

    //    public void UpdateItem(T item) {
    //        SortUp(item);
    //    }
    //}

    //public interface IHeapItem<T> : IComparable<T>{
    //    int HeapIndex {get; set; }
    //}
}
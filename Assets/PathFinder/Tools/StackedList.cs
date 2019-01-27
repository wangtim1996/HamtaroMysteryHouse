using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace K_PathFinder.CoolTools {
    public class StackedList<T> {
        const int INVALID_ROOT = -2;
        const int INVALID_INDEX = -1;

        int size, unfoldedSize;
        List<T> values = new List<T>();
        LinkedNode[] nodes;
        int filledIndexes;

        struct LinkedNode {
            public int itemIndex, nextIndex;

            public void SetItem(int index) {
                itemIndex = index;
                nextIndex = INVALID_INDEX;
            }

            public override string ToString() {
                return string.Format("i: {0}, next {1}", itemIndex, nextIndex);
            }
        }

        public StackedList(int Size, int initialExtraBuffer) {
            if (initialExtraBuffer < 0)
                Debug.LogError("unfoldedSizeMultiplier cant be less than 0");

            size = Size;
            unfoldedSize = Size + initialExtraBuffer;
            filledIndexes = Size;

            nodes = new LinkedNode[unfoldedSize];
            for (int i = 0; i < Size; i++) { nodes[i].nextIndex = INVALID_ROOT; }          
        }
        
        public void Add(int index, T value) {   
            if(index >= size) 
                Debug.LogError("Index cannot be greater than size");            

            int valueIndex = values.Count;
            values.Add(value);
            
            LinkedNode curNode = nodes[index];
            if (curNode.nextIndex == INVALID_ROOT) {
                nodes[index].SetItem(valueIndex);
            }
            else {
                int freeIndex = filledIndexes++;

                if (freeIndex == nodes.Length) {
                    LinkedNode[] newNodes = new LinkedNode[nodes.Length * 2];
                    for (int i = 0; i < nodes.Length; i++) { newNodes[i] = nodes[i]; }
                    nodes = newNodes;
                }

              
                int lastIndex = index;
                for (; index != INVALID_INDEX; index = nodes[index].nextIndex) { lastIndex = index; }
                nodes[lastIndex].nextIndex = freeIndex;
                nodes[freeIndex].SetItem(valueIndex);
            }
        }

        public void Read(int index, ICollection<T> collection) {
            if (index >= size) 
                Debug.LogError("Index cannot be greater than size");
            

            LinkedNode first = nodes[index];
            if (first.nextIndex == INVALID_ROOT)
                return;

            for (; index != -1; index = nodes[index].nextIndex) {
                collection.Add(values[nodes[index].itemIndex]);
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("nodes");
            for (int i = 0; i < nodes.Length; i++) { sb.AppendLine(nodes[i].ToString()); }
            sb.AppendLine(string.Empty);
            sb.AppendLine("values");
            for (int i = 0; i < values.Count; i++) { sb.AppendLine(values[i].ToString()); }
            return sb.ToString();
        }
    }
}
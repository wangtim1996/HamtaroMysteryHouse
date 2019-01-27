using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using K_PathFinder.Graphs;

namespace K_PathFinder {
    //i waited literaly year to finaly write this class. hooray!
    [Serializable]
    public class AreaAdvanced : Area {
        [NonSerialized] public List<Cell> cells = new List<Cell>();
        [NonSerialized] public AreaWorldMod container;

        //this called when cell finish generation
        public void AddCell(Cell cell, out IEnumerable<CellPathContentAbstract> content, out bool cellCanBeUsedInitialState) {
            lock (cells) {
                cells.Add(cell);
                content = container.cellPathContents;
                cellCanBeUsedInitialState = container.cellUsabilityState;
            }
        }

        //this called when cell start it destruction process
        public void RemoveCell(Cell cell) {
            lock (cells)
                cells.Remove(cell);
        }
    }
}

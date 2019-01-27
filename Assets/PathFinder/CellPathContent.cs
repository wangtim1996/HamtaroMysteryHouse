using K_PathFinder.Graphs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace K_PathFinder {
    /// <summary>
    /// this is abstract class that will be returned when you request path
    /// since unity do not serialize abstract classes right now no storage of this type were made
    /// you need to derive from this class and add it to the AreaWorldMod related method
    /// then this will be added to current AreaWorldMod cells and those which will be generated later on
    /// </summary>
    public abstract class CellPathContentAbstract { 
        /// <summary>
        /// called when this value added to cell
        /// </summary>
        public virtual void OnAddingToCell(Cell cell) {}

        /// <summary>
        /// called when this value removed from cell
        /// </summary>
        public virtual void OnRemovingFromCell(Cell cell) { }
    }


    //*******************************************//
    //Bunch of premade types
    //They are more like used as example. 
    //I suggest you to expand this type yourself
    //******************************************//

    public class CellPathContentObject : CellPathContentAbstract {
        public object obj;

        public CellPathContentObject(object obj) {
            this.obj = obj;
        }
    }

    public class CellPathContentGameObject : CellPathContentAbstract {
        public GameObject gameObject;

        public CellPathContentGameObject(GameObject gameObject) {
            this.gameObject = gameObject;
        }
    }

    public class CellPathContentVector2 : CellPathContentAbstract {
        public Vector2 vector;

        public CellPathContentVector2(Vector2 vector) {
            this.vector = vector;
        }
    }

    public class CellPathContentVector3 : CellPathContentAbstract {
        public Vector3 vector;

        public CellPathContentVector3(Vector3 vector) {
            this.vector = vector;
        }
    }
}
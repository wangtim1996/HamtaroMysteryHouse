using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace K_PathFinder {
    //area for navmesh
    [Serializable]
    public class Area{
        [SerializeField]public string name = "area name";
        [SerializeField]public int id = -1;
        [SerializeField]public Color color;
        [SerializeField]public int overridePriority = 1; //navmesh uses rasterization to get data and this value tell friority of area if z-fighting was occured
        [SerializeField]public float cost = 1f; //cost of movement. act as distance multiplier

        public Area(string name, int id, Color color) {
            this.name = name;
            this.id = id;
            this.color = color;
        }

        public Area(string name, int id) {
            this.name = name;
            this.id = id;

            System.Random rnd = new System.Random(GetHashCode());
            color = new Color(rnd.Next(0,255) / 255f, rnd.Next(0, 255) / 255f, rnd.Next(0, 255) / 255f, 1f);      
        }

        public Area(string name, Color color) : this(name, -1, color) {}
        public Area(string name) : this(name, -1) {}
        public Area() : this("area name", -1) { }
    }
}

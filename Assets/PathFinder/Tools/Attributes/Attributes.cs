using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder {    
    //For drawing Area sellector from global dictionary
    //use it for int
    public class AreaAttribute : PropertyAttribute {
        public bool drawLabel;
        public AreaAttribute(bool DrawLabel = true) {
            drawLabel = DrawLabel;
        }
    }    
    
    //for drawing Tags sellector
    //use it for string
    public class TagAttribute : PropertyAttribute {
        public bool drawLabel;
        public TagAttribute(bool DrawLabel = true) {
            drawLabel = DrawLabel;
        }
    }
}


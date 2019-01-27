using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder {
    public interface IChunkContent {
        /// <summary>
        /// use it to define space that this content take
        /// </summary>
        Bounds chunkContentBounds { get; }
    }
}
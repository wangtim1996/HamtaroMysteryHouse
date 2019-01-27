using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder {
    public static class ChunkContentMap {
        //(x * sizeX) + z;
        private static Bounds2DInt mapSpace;
        private static ChunkContent[,] map;
        private static bool init = false;

        private static HashSet<IChunkContent> beforeInit = new HashSet<IChunkContent>();
        private static Dictionary<IChunkContent, ChunkContentMetaData> content = new Dictionary<IChunkContent, ChunkContentMetaData>();
  
        //internal types
        private struct ChunkContentMetaData {
            public readonly Bounds2DInt lastUpdate;

            public ChunkContentMetaData(Bounds2DInt lastUpdate) {
                this.lastUpdate = lastUpdate;
            }
        }


        public class ChunkContent : HashSet<IChunkContent> {
            //todo
        }

        public static void Init() {
            if (init)
                return;

            init = true;          
            foreach (var item in beforeInit) {
                Process(item);
            }
            beforeInit.Clear();
        }

        public static void Clear() {
            mapSpace = Bounds2DInt.zero;
            map = null;
            beforeInit.Clear();
            content.Clear();
            init = false;
        }

        private static void ResizeMap(Bounds2DInt newMapSpace) {
            if (newMapSpace.minX > mapSpace.minX || 
                newMapSpace.minY > mapSpace.minY ||
                newMapSpace.maxX < mapSpace.maxX || 
                newMapSpace.maxY < mapSpace.maxY)
                throw new ArgumentException(
                    String.Format("Sizes of content map can only be expanded. Old: x {0}, New: x {1}", mapSpace, newMapSpace));

            if (mapSpace == newMapSpace)
                return;

            int offsetX = mapSpace.minX - newMapSpace.minX;
            int offsetZ = mapSpace.minY - newMapSpace.minY;
            int newSizeX = newMapSpace.sizeX;
            int newSizeZ = newMapSpace.sizeZ;

            ChunkContent[,] newMap = new ChunkContent[newSizeX, newSizeZ];

            for (int x = 0; x < mapSpace.sizeX; x++) {
                for (int z = 0; z < mapSpace.sizeZ; z++) {
                    newMap[x + offsetX, z + offsetZ] = map[x, z];
                }
            }

            //Debug.Log("map resized to " + newMapSpace);

            mapSpace = newMapSpace;
            map = newMap;
        }

        private static void IncludeBounds(int startX, int startZ, int endX, int endZ) {
            Bounds2DInt boundsPlusOne = new Bounds2DInt(startX, startZ, endX + 1, endZ + 1);
            if (content.Count == 0) {
                mapSpace = boundsPlusOne;
                map = new ChunkContent[boundsPlusOne.sizeX, boundsPlusOne.sizeZ];      
            }
            else {
                ResizeMap(Bounds2DInt.GetIncluded(mapSpace, boundsPlusOne));
            }
        }
        private static void IncludeBounds(Bounds2DInt bounds) {
            IncludeBounds(bounds.minX, bounds.minY, bounds.maxX, bounds.maxY);
        }

        public static void RemoveContent(IChunkContent removedContent) {            
            if(init) {
                ChunkContentMetaData metaData;
                if (content.TryGetValue(removedContent, out metaData)) {
                    //Bounds2DInt bounds = metaData.lastUpdate;
                    GetChunkRemoveContent(metaData.lastUpdate, removedContent);
                    content.Remove(removedContent);
                }
                //else it's already probably removed or there is assebly reload
            }
            else {
                beforeInit.Remove(removedContent);
            }
        }
        public static void RemoveContent<T>(params T[] processedContentArray) where T : IChunkContent {
            if (init) {
                for (int i = 0; i < processedContentArray.Length; i++) {
                    IChunkContent removedContent = processedContentArray[i];
                    ChunkContentMetaData metaData;
                    if (content.TryGetValue(removedContent, out metaData)) {
                        //Bounds2DInt bounds = metaData.lastUpdate;
                        GetChunkRemoveContent(metaData.lastUpdate, removedContent);        
                        content.Remove(removedContent);
                    }
                }           
            }
            else {
                for (int i = 0; i < processedContentArray.Length; i++) {
                    beforeInit.Remove(processedContentArray[i]);
                }      
            }
        }
        public static void RemoveContent<T>(List<T> processedContentArray) where T : IChunkContent {            
            if (init) {
                for (int i = 0; i < processedContentArray.Count; i++) {
                    IChunkContent removedContent = processedContentArray[i];
                    ChunkContentMetaData metaData;
                    if (content.TryGetValue(removedContent, out metaData)) {
                        //Bounds2DInt bounds = metaData.lastUpdate;
                        GetChunkRemoveContent(metaData.lastUpdate, removedContent);
                        content.Remove(removedContent);
                    }
                }
            }
            else {
                for (int i = 0; i < processedContentArray.Count; i++) {
                    beforeInit.Remove(processedContentArray[i]);
                }
            }
        }
        
        /// <summary>
        /// target content will be added or update it's possition if already added
        /// </summary>
        public static void Process(IChunkContent processedContent) {
            Bounds2DInt processedContentBounds = PathFinder.ToChunkPosition(processedContent.chunkContentBounds);

            ChunkContentMetaData metaData;
            if (content.TryGetValue(processedContent, out metaData)) {
                Bounds2DInt currentContentBounds = metaData.lastUpdate;         

                if (currentContentBounds == processedContentBounds)
                    return; //nothing to change since it occupy same space

                GetChunkRemoveContent(currentContentBounds, processedContent);
                IncludeBounds(processedContentBounds);
                GetChunkAddContent(processedContentBounds, processedContent);
                content[processedContent] = new ChunkContentMetaData(processedContentBounds);                
            }
            else {
                if (init == false) {
                    beforeInit.Add(processedContent);
                    return;
                }

                IncludeBounds(processedContentBounds);

                GetChunkAddContent(processedContentBounds, processedContent);
                content[processedContent] = new ChunkContentMetaData(processedContentBounds);
            }
        }
        /// <summary>
        /// target content will be added or update it's possition if already added
        /// </summary>
        public static void Process<T>(params T[] processedContentArray) where T : IChunkContent {            
            if (processedContentArray == null || processedContentArray.Length == 0)
                return;

            Bounds2DInt first = PathFinder.ToChunkPosition(processedContentArray[0].chunkContentBounds);

            int minX = first.minX, 
                minZ = first.minY,
                maxX = first.maxX,
                maxZ = first.maxY;

            Bounds2DInt[] array = new Bounds2DInt[processedContentArray.Length];
            array[0] = first;

            for (int i = 1; i < processedContentArray.Length; i++) {
                Bounds2DInt curRange = PathFinder.ToChunkPosition(processedContentArray[i].chunkContentBounds);
                array[i] = curRange;
                if (minX < curRange.minX) minX = curRange.minX;
                if (minZ < curRange.minY) minZ = curRange.minY;
                if (maxX < curRange.maxX) maxX = curRange.maxX;
                if (maxZ < curRange.maxY) maxZ = curRange.maxY;
            }

            IncludeBounds(new Bounds2DInt(minX, minZ, maxX, maxZ));

            for (int i = 0; i < processedContentArray.Length; i++) {
                Bounds2DInt contentBoundsNew = array[i];
                IChunkContent processedContent = processedContentArray[i];
                ChunkContentMetaData metaData;
                if (content.TryGetValue(processedContent, out metaData)) {
                    Bounds2DInt currentContentBounds = metaData.lastUpdate;

                    if (currentContentBounds == contentBoundsNew)
                        return; //nothing to change since it occupy same space

                    GetChunkRemoveContent(currentContentBounds, processedContent);              
                    GetChunkAddContent(contentBoundsNew, processedContent);
                    content[processedContent] = new ChunkContentMetaData(contentBoundsNew);
                }
                else {
                    if (init == false) {
                        beforeInit.Add(processedContent);
                        return;
                    }
                    
                    GetChunkAddContent(contentBoundsNew, processedContent);
                    content[processedContent] = new ChunkContentMetaData(contentBoundsNew);
                }
            }

        }
        /// <summary>
        /// target content will be added or update it's possition if already added
        /// </summary>
        public static void Process<T>(List<T> processedContentList) where T : IChunkContent {
            //Debug.Log(processedContentList.Count);

            if (processedContentList == null || processedContentList.Count == 0)
                return;

            Bounds2DInt first = PathFinder.ToChunkPosition(processedContentList[0].chunkContentBounds);

            int minX = first.minX,
                minZ = first.minY,
                maxX = first.maxX,
                maxZ = first.maxY;

            Bounds2DInt[] array = new Bounds2DInt[processedContentList.Count];
            array[0] = first;

            for (int i = 1; i < processedContentList.Count; i++) {
                Bounds2DInt curRange = PathFinder.ToChunkPosition(processedContentList[i].chunkContentBounds);
                array[i] = curRange;
                if (minX > curRange.minX) minX = curRange.minX;
                if (minZ > curRange.minY) minZ = curRange.minY;
                if (maxX < curRange.maxX) maxX = curRange.maxX;
                if (maxZ < curRange.maxY) maxZ = curRange.maxY;
            }

            IncludeBounds(new Bounds2DInt(minX, minZ, maxX, maxZ));

            for (int i = 0; i < processedContentList.Count; i++) {
                Bounds2DInt processedContentBounds = array[i];
                IChunkContent processedContent = processedContentList[i];
                ChunkContentMetaData metaData;
                if (content.TryGetValue(processedContent, out metaData)) {
                    Bounds2DInt currentContentBounds = metaData.lastUpdate;

                    if (currentContentBounds == processedContentBounds)
                        return; //nothing to change since it occupy same space

                    GetChunkRemoveContent(currentContentBounds, processedContent);
                    GetChunkAddContent(processedContentBounds, processedContent);
                    content[processedContent] = new ChunkContentMetaData(processedContentBounds);
                }
                else {
                    if (init == false) {
                        beforeInit.Add(processedContent);
                        return;
                    }

                    GetChunkAddContent(processedContentBounds, processedContent);
                    content[processedContent] = new ChunkContentMetaData(processedContentBounds);
                }
            }

            //SceneDebug();
        }
        
        private static void GetChunkAddContent(Bounds2DInt bounds, IChunkContent content) {
            for (int x = bounds.minX; x < bounds.maxX + 1; x++) {
                for (int z = bounds.minY; z < bounds.maxY + 1; z++) {         
                    GetChunkContent(x, z).Add(content);
                }
            }
        }

        private static void GetChunkRemoveContent(Bounds2DInt bounds, IChunkContent content) {
            for (int x = bounds.minX; x < bounds.maxX + 1; x++) {
                for (int z = bounds.minY; z < bounds.maxY + 1; z++) {               
                    GetChunkContent(x, z).Remove(content);
                }
            }
        }
        
        private static ChunkContent GetChunkContent(int x, int z) {
            x = x - mapSpace.minX;
            z = z - mapSpace.minY;

            ChunkContent result = map[x, z];
            if (result == null) {
                result = new ChunkContent();
                map[x, z] = result;
            }
            return result;
        }

        private static bool TryGetChunkContent(int x, int z, out ChunkContent content) {
            if (x >= mapSpace.minX &&
                z >= mapSpace.minY &&
                x < mapSpace.maxX &&
                z < mapSpace.maxY) {
                content = GetChunkContent(x, z);            
                return content != null;
            }
            else {
                content = null;
                return false;
            }
        }
        
        public static void GetContent<T>(int x, int z, ICollection<T> collectionToFill) where T : class, IChunkContent {
            ChunkContent chunk;
            if (TryGetChunkContent(x, z, out chunk)) {
                foreach (IChunkContent content in chunk) {
                    if (content is T)
                        collectionToFill.Add(content as T);
                }
            }
        }

        public static void GetContent<T>(int x, int z, ICollection<T> collectionToFill, Predicate<T> match) where T : class, IChunkContent {
            ChunkContent chunk;
            if (TryGetChunkContent(x, z, out chunk)) {
                foreach (IChunkContent content in chunk) {
                    if (content is T && match(content as T))
                        collectionToFill.Add(content as T);
                }
            }
        }



        private static int startX {
            get { return mapSpace.minX; }
        }
        private static int startZ {
            get { return mapSpace.minY; }
        }
        private static int endX {
            get { return mapSpace.maxX; }
        }
        private static int endZ {
            get { return mapSpace.maxY; }
        }

#if UNITY_EDITOR
        public static void SceneDebug() {
            Debuger_K.ClearGeneric();
            
            float gs = PathFinder.gridSize;
            Vector3 add = new Vector3(gs * 0.5f, 0, gs * 0.5f);

            StringBuilder sb = new StringBuilder();
            foreach (var item in content) {
                sb.AppendLine(item.Value.lastUpdate.ToString());
            }

            Debuger_K.AddLabelFormat(new Vector3(startX * gs, 0, startZ * gs), "Map space {0}\nCount {1}\n{2}", mapSpace, content.Count, sb);

            for (int x = startX; x < endX + 1; x++) {
                Debuger_K.AddLine(new Vector3(x, 0, startZ) * gs, new Vector3(x, 0, endZ) * gs, Color.blue);
            }

            for (int z = startZ; z < endZ + 1; z++) {
                Debuger_K.AddLine(new Vector3(startX, 0, z) * gs, new Vector3(endX, 0, z) * gs, Color.blue);
            }

            System.Random random = new System.Random();

            for (int x = startX; x < endX; x++) {
                for (int z = startZ; z < endZ; z++) {
                    ChunkContent content;
                    TryGetChunkContent(x, z, out content);
                    
                    Vector3 p = (new Vector3(x, 0, z) * gs) + add;

                    Debuger_K.AddLabelFormat(p, "x: {0}\nz: {1}", x, z);

                    Color color = new Color(1, 0, 0, 0.1f);
                    if (content != null && content.Count != 0)
                        color = new Color(0, 1, 0, 0.1f);

                    Debuger_K.AddTriangle(new Vector3(x, 0, z) * gs, (new Vector3(x + 1, 0, z) * gs), (new Vector3(x, 0, z + 1) * gs), color, false);
                    Debuger_K.AddTriangle(new Vector3(x + 1, 0, z + 1) * gs, (new Vector3(x + 1, 0, z) * gs), (new Vector3(x, 0, z + 1) * gs), color, false);

                    if (content != null && content.Count != 0) {
                        Color cColor = new Color(random.Next(100) / 100f, random.Next(100) / 100f, random.Next(100) / 100f, 1f);
                        foreach (var item in content) {
                            Debuger_K.AddBounds(item.chunkContentBounds, cColor);
                            Debuger_K.AddLine(p, item.chunkContentBounds.center, cColor);
                        }
                    }
                }
            }
        }
#endif
    }
}

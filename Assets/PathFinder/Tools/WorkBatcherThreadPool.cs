using K_PathFinder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace K_PathFinder.PFTools {
    public interface IThreadPoolWorkBatcherMember {
        void PerformWork(object context);
    }

    /// <summary>
    /// generic class for grouping bunch of work in threads. with pool
    /// </summary>
    public class WorkBatcherThreadPool<T> : ThreadPoolWorkBatcher<T> where T : IThreadPoolWorkBatcherMember, IObjectPoolMember, new() { //oh wow! so generic!
        ObjectPoolGeneric<T> pool;

        public WorkBatcherThreadPool(int startPoolSize = 100){
            pool = new ObjectPoolGeneric<T>(startPoolSize);
        }

        public void AddWorkPooled(object context) {
            AddWork(pool.Rent(), context);
        }

        protected override void ThreadPoolCallbackLimited(object context) {
            try {
                ThreadPoolContextLimited threadContext = (ThreadPoolContextLimited)context;
                List<WorkValue> workBatch = threadContext.workBatch;

                for (int i = threadContext.threadStart; i < threadContext.threadEnd; i++) {
                    workBatch[i].obj.PerformWork(workBatch[i].context);
                    pool.ReturnRented(workBatch[i].obj);
                }

                threadContext.mre.Set();
            }
            catch (Exception e) {
                UnityEngine.Debug.LogErrorFormat("TP work batcher {0}: {1}", typeof(T).GetType(), e);
                throw;
            }
        }
        protected override void ThreadPoolCallbackSimple(object context) {
            try {
                ThreadPoolContextSimple threadContext = (ThreadPoolContextSimple)context;
                threadContext.obj.PerformWork(threadContext.context);
                pool.ReturnRented(threadContext.obj);
                threadContext.mre.Set();
            }
            catch (Exception e) {
                UnityEngine.Debug.LogError(e);
                throw;
            }
        }
    }
    
    /// <summary>
    /// generic class for grouping bunch of work in threads
    /// </summary>
    public class ThreadPoolWorkBatcher<T> where T : IThreadPoolWorkBatcherMember {
        object locker = new object();
        List<WorkValue> batch0 = new List<WorkValue>();
        List<WorkValue> batch1 = new List<WorkValue>();
        bool curBatch = false;
        ManualResetEvent[] eventPool;

        public void AddWork(T work, object context) {
            lock (locker) {
                if (curBatch)
                    batch1.Add(new WorkValue(work, context));
                else
                    batch0.Add(new WorkValue(work, context));
            }
        }

        public bool haveWork {
            get { return workCount > 0; }            
        }

        public int workCount {
            get {
                lock (locker) {
                    if (curBatch)
                        return batch1.Count;
                    else
                        return batch0.Count;
                }
            }
        }

        public void Clear() {
            lock (locker) {
                batch0.Clear();
                batch1.Clear();
            }
        }
        
        public void PerformCurrentBatch(int maxThreads) {
            List<WorkValue> batch;
            int workCount;

            lock (locker) {
                batch = curBatch ? batch1 : batch0;
                workCount = batch.Count;
                if (workCount == 0)
                    return;

                curBatch = !curBatch;
            }      


            if (eventPool == null || eventPool.Length != maxThreads) {
                eventPool = new ManualResetEvent[maxThreads];
                for (int i = 0; i < maxThreads; i++) {
                    eventPool[i] = new ManualResetEvent(true);
                }
            }

            int curIndex = 0;
            int agentsPerThread = (workCount / maxThreads) + 1;

            for (int i = 0; i < maxThreads; i++) {
                int end = curIndex + agentsPerThread;

                if (end >= workCount) {
                    end = workCount;
                    eventPool[i].Reset();
                    ThreadPool.QueueUserWorkItem(ThreadPoolCallbackLimited, new ThreadPoolContextLimited(eventPool[i], batch, curIndex, end));
                    break;
                }
                else {
                    eventPool[i].Reset();
                    ThreadPool.QueueUserWorkItem(ThreadPoolCallbackLimited, new ThreadPoolContextLimited(eventPool[i], batch, curIndex, end));
                }

                curIndex = end;
            }

            WaitHandle.WaitAll(eventPool);
            batch.Clear();
        }

        //callbacks
        protected virtual void ThreadPoolCallbackLimited(object context) {  
            try {
                ThreadPoolContextLimited threadContext = (ThreadPoolContextLimited)context;
                List<WorkValue> workBatch = threadContext.workBatch;

                for (int i = threadContext.threadStart; i < threadContext.threadEnd; i++) {
                    workBatch[i].obj.PerformWork(workBatch[i].context);
                }

                threadContext.mre.Set();
            }
            catch (Exception e) {
                UnityEngine.Debug.LogError(e);
                throw;
            }
        }
        protected virtual void ThreadPoolCallbackSimple(object context) {
            try {
                ThreadPoolContextSimple threadContext = (ThreadPoolContextSimple)context;
                threadContext.obj.PerformWork(threadContext.context);
                threadContext.mre.Set();
            }
            catch (Exception e) {
                UnityEngine.Debug.LogError(e);
                throw;
            }
        }

        //structs
        protected struct ThreadPoolContextLimited {
            public readonly ManualResetEvent mre;
            public readonly int threadStart, threadEnd;
            public readonly List<WorkValue> workBatch;

            public ThreadPoolContextLimited(ManualResetEvent MRE, List<WorkValue> batch, int start, int end) {
                mre = MRE;
                threadStart = start;
                threadEnd = end;
                workBatch = batch;
            }
        }
        protected struct ThreadPoolContextSimple {
            public readonly ManualResetEvent mre;
            public readonly object context;
            public readonly T obj;

            public ThreadPoolContextSimple(WorkValue workValue, ManualResetEvent MRE) {
                obj = workValue.obj;
                context = workValue.context;
                mre = MRE;           
            }
        }
        protected struct WorkValue {
            public readonly object context;
            public readonly T obj;

            public WorkValue(T Obj, object Context) {
                obj = Obj;
                context = Context;
            }
        }
    }

    /// <summary>
    /// simple toy to have to stagger thread work
    /// </summary>
    public class WorkBatcher<T> {
        object locker = new object();
        Queue<T> batch0 = new Queue<T>();
        Queue<T> batch1 = new Queue<T>();
        bool curBatch = false;

        public void Add(T val) {
            lock (locker) {
                if (curBatch)
                    batch1.Enqueue(val);
                else
                    batch0.Enqueue(val);
            }
        }

        public Queue<T> currentBatch {
            get {
                lock (locker) {
                    if (curBatch)
                        return batch1;
                    else
                        return batch0;
                }
            }
        }


        public void Flip() {
            lock (locker) {
                curBatch = !curBatch;
            }
        }

        public bool haveWork {
            get { return workCount > 0; }
        }

        public int workCount {
            get {
                lock (locker) {
                    if (curBatch)
                        return batch1.Count;
                    else
                        return batch0.Count;
                }
            }
        }

        public void Clear() {
            lock (locker) {
                batch0.Clear();
                batch1.Clear();
            }
        }
    }
}

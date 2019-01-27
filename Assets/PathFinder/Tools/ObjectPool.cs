using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.PFTools {
    public interface IObjectPoolMember {
        void Clear();
    }

    public interface IObjectPoolPoolable {
        void ReturnToPool();
    }

    public class ObjectPoolGeneric<T> where T : IObjectPoolMember, new() {
        Stack<T> data;

        public ObjectPoolGeneric(int startSize = 100) {
            data = new Stack<T>(startSize);
        }

        public T Rent() {
            lock (data) {
                if (data.Count > 0)
                    return data.Pop();
                else
                    return new T();
            }
        }

        public void ReturnRented(T obj) {
            lock (data) {
                obj.Clear();
                data.Push(obj);
            }
        }

    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace K_PathFinder{
    public enum ProfilderLogMode {
        log, warning, error
    }
    public class NavmeshProfiler {
        Stopwatch mainWatch = new Stopwatch();
        Stopwatch threadWatch = new Stopwatch();
        StringBuilder log = new StringBuilder();

        int chunkPosX;
        int chunkPosZ;

        public NavmeshProfiler(int ChunkPosX, int ChunkPosZ) {
            chunkPosX = ChunkPosX;
            chunkPosZ = ChunkPosZ;
        }

        public void StartProfile() {
            mainWatch.Start();
            log.AppendLine("start profiling. PathFinder version " + PathFinder.VERSION);
        }

        public void EndProfile() {
            log.AppendLine("end profiling");
            mainWatch.Stop();        
        }

        public void StartThreadStuff() {
            threadWatch.Start();
        }

        public void EndThreadStuff() {
            threadWatch.Stop();
        }

        public void Abort() {
            AddLog("stop == true. so we stop", Color.red);
        }

        //add log
        public void AddLog(string s) {
            log.AppendFormat("{0} : {1}\n", mainWatch.Elapsed, s);
        }

        public void AddLog(string s, Color color) {
            log.AppendFormat("<color={0}>{1} : {2}</color>\n", ColorToHex(color), mainWatch.Elapsed, s);
        }

        public void AddLogFormat(string format, params string[] args) {
            AddLog(string.Format(format, args));
        }
        public void AddLogFormat(string format, params object[] args) {
            AddLog(string.Format(format, args));
        }

        public void AddLogFormat(string format, Color color, params string[] args) {
            AddLog(string.Format(format, args), color);
        }
        public void AddLogFormat(string format, Color color, params object[] args) {
            AddLog(string.Format(format, args), color);
        }


        public void DebugLog(ProfilderLogMode mode) {
            switch (mode) {
                case ProfilderLogMode.log:
                    UnityEngine.Debug.Log(string.Format("Chunk x: {0}, z: {1}. Thread time: {2} ms, Total time: {3} ms\n\nLog:\n{4}", chunkPosX, chunkPosZ, threadWatch.ElapsedMilliseconds, mainWatch.ElapsedMilliseconds, log.ToString()));
                    break;
                case ProfilderLogMode.warning:
                    UnityEngine.Debug.LogWarning(string.Format("Chunk x: {0}, z: {1}. Thread time: {2} ms, Total time: {3} ms\n\nLog:\n{4}", chunkPosX, chunkPosZ, threadWatch.ElapsedMilliseconds, mainWatch.ElapsedMilliseconds, log.ToString()));
                    break;
                case ProfilderLogMode.error:
                    UnityEngine.Debug.LogError(string.Format("Chunk x: {0}, z: {1}. Thread time: {2} ms, Total time: {3} ms\n\nLog:\n{4}", chunkPosX, chunkPosZ, threadWatch.ElapsedMilliseconds, mainWatch.ElapsedMilliseconds, log.ToString()));
                    break;
            }           
        }

        string ColorToHex(Color32 color) {
            return string.Format("#{0}{1}{2}{3}", color.r.ToString("X2"), color.g.ToString("X2"), color.b.ToString("X2"), color.a.ToString("X2"));
        }
    }
}

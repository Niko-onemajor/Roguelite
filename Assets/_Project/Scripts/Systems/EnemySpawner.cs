using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>按波次计划分批刷怪；WaveComplete = 全部生成且场上无存活。</summary>
    public class EnemySpawner : MonoBehaviour
    {
        List<WaveBatch> batches;
        int batchIndex;
        WaveBatch current;
        int remainingInBatch;
        int pending;
        float spawnCd = 0.2f;

        public bool WaveComplete => pending == 0 && EnemyRegistry.AliveCount == 0;

        public void StartWave(List<WaveBatch> schedule)
        {
            batches = schedule;
            batchIndex = 0;
            current = null;
            remainingInBatch = 0;
            pending = 0;
            spawnCd = 0.2f;
            if (schedule == null) return;
            foreach (WaveBatch b in schedule) pending += b.count;
        }

        /// <summary>中止当前波次，避免继续刷残留敌人（回合倒计时结束时调用）。</summary>
        public void StopWave()
        {
            batchIndex = batches != null ? batches.Count : 0;
            current = null;
            remainingInBatch = 0;
            pending = 0;
        }

        void Update()
        {
            if (pending <= 0) return;
            spawnCd -= Time.deltaTime;
            if (spawnCd > 0f) return;
            if (current == null || remainingInBatch <= 0)
            {
                if (batchIndex >= batches.Count) return;
                current = batches[batchIndex++];
                remainingInBatch = current.count;
            }
            EnemyFactory.Spawn(current.enemy, RandomOffScreenPosition());
            remainingInBatch--;
            pending--;
            spawnCd = current.spawnInterval;
        }

        static Vector2 RandomOffScreenPosition() => ArenaBounds.RandomRingPosition();
    }
}
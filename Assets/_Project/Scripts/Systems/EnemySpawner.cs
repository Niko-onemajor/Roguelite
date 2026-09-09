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

        static Vector2 RandomOffScreenPosition()
        {
            Camera cam = Camera.main;
            if (cam == null) return Random.insideUnitCircle * 5f;
            float halfH = cam.orthographicSize + 1.5f;
            float halfW = halfH * cam.aspect + 1.5f;
            int side = Random.Range(0, 4);
            float x = side == 0 ? -halfW : side == 1 ? halfW : Random.Range(-halfW, halfW);
            float y = side == 2 ? -halfH : side == 3 ? halfH : Random.Range(-halfH, halfH);
            return new Vector2(x, y);
        }
    }
}
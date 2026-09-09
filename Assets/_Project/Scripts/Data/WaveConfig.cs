using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    [Serializable]
    public class WaveBatch
    {
        public EnemyData enemy;
        public int count = 1;
        public float spawnInterval = 1f; // 本批每两只间的刷新间隔
    }

    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Roguelite/WaveConfig")]
    public class WaveConfig : ScriptableObject
    {
        public float prepareTime = 2f;
        public List<WaveBatch> wave1 = new List<WaveBatch>();
        public List<WaveBatch> wave2 = new List<WaveBatch>();
        public List<WaveBatch> wave3 = new List<WaveBatch>();
        public List<WaveBatch> wave4 = new List<WaveBatch>();
        public List<WaveBatch> wave5 = new List<WaveBatch>();

        public List<List<WaveBatch>> Waves()
        {
            var list = new List<List<WaveBatch>> { wave1, wave2, wave3, wave4, wave5 };
            list.RemoveAll(w => w.Count == 0);
            return list;
        }
    }
}
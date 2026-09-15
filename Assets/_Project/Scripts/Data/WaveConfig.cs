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
        public float combatTime = 25f; // 每回合战斗倒计时(秒)，时间到残敌消失进入锻体
        public List<WaveBatch> wave1 = new List<WaveBatch>();
        public List<WaveBatch> wave2 = new List<WaveBatch>();
        public List<WaveBatch> wave3 = new List<WaveBatch>();
        public List<WaveBatch> wave4 = new List<WaveBatch>();
        public List<WaveBatch> wave5 = new List<WaveBatch>();
        public List<WaveBatch> wave6 = new List<WaveBatch>();
        public List<WaveBatch> wave7 = new List<WaveBatch>();
        public List<WaveBatch> wave8 = new List<WaveBatch>();
        public List<WaveBatch> wave9 = new List<WaveBatch>();
        public List<WaveBatch> wave10 = new List<WaveBatch>();
        public List<WaveBatch> wave11 = new List<WaveBatch>();
        public List<WaveBatch> wave12 = new List<WaveBatch>();
        public List<WaveBatch> wave13 = new List<WaveBatch>();
        public List<WaveBatch> wave14 = new List<WaveBatch>();
        public List<WaveBatch> wave15 = new List<WaveBatch>();
        public List<WaveBatch> wave16 = new List<WaveBatch>();
        public List<WaveBatch> wave17 = new List<WaveBatch>();
        public List<WaveBatch> wave18 = new List<WaveBatch>();
        public List<WaveBatch> wave19 = new List<WaveBatch>();
        public List<WaveBatch> wave20 = new List<WaveBatch>();

        public List<List<WaveBatch>> Waves()
        {
            var list = new List<List<WaveBatch>>
            {
                wave1, wave2, wave3, wave4, wave5, wave6, wave7, wave8, wave9, wave10,
                wave11, wave12, wave13, wave14, wave15, wave16, wave17, wave18, wave19, wave20
            };
            list.RemoveAll(w => w.Count == 0);
            return list;
        }
    }
}
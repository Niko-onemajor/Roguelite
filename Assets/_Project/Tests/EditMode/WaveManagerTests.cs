using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Roguelite.Tests
{
    /// <summary>波次配置(20 波)与无尽波生成逻辑。</summary>
    public class WaveManagerTests
    {
        [Test]
        public void Default_Config_Provides_20_Waves()
        {
            Assert.That(GameConfig.Default().waves.Waves().Count, Is.EqualTo(20));
        }

        [Test]
        public void Default_Config_Waves_Are_NonEmpty()
        {
            foreach (var wave in GameConfig.Default().waves.Waves())
                Assert.That(wave, Is.Not.Empty);
        }

        [Test]
        public void BuildEndlessWave_Cycle0_No_Scaling()
        {
            var templates = Templates();
            var wave = WaveManager.BuildEndlessWave(templates, 1); // 第 1 波，周期 0

            Assert.That(wave, Has.Count.EqualTo(1));
            Assert.That(wave[0].count, Is.EqualTo(4));
            Assert.That(wave[0].enemy.maxHP, Is.EqualTo(20f).Within(0.001f));
            Assert.That(wave[0].enemy.contactDamage, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void BuildEndlessWave_Scales_Count_HP_And_Damage()
        {
            var templates = Templates();
            var wave = WaveManager.BuildEndlessWave(templates, 3); // 周期 (3-1)/1 = 2

            Assert.That(wave[0].count,
                Is.EqualTo(4 + 2 * WaveManager.EndlessCountPerCycle));
            Assert.That(wave[0].enemy.maxHP,
                Is.EqualTo(20f * (1f + 2f * WaveManager.EndlessHPMultPerCycle)).Within(0.001f));
            Assert.That(wave[0].enemy.contactDamage,
                Is.EqualTo(5f * (1f + 2f * WaveManager.EndlessDamageMultPerCycle)).Within(0.001f));
        }

        [Test]
        public void BuildEndlessWave_Cycles_Through_Templates()
        {
            var templates = Templates(2);
            var wave3 = WaveManager.BuildEndlessWave(templates, 3); // 周期 1，模板 0
            var wave4 = WaveManager.BuildEndlessWave(templates, 4); // 周期 1，模板 1

            Assert.That(wave3[0].enemy.type, Is.EqualTo(EnemyType.Chaser));
            Assert.That(wave4[0].enemy.type, Is.EqualTo(EnemyType.Tank));
        }

        [Test]
        public void BuildEndlessWave_Returns_Clone_Not_Template()
        {
            var templates = Templates();
            var wave = WaveManager.BuildEndlessWave(templates, 1);

            Assert.That(wave[0].enemy, Is.Not.SameAs(templates[0][0].enemy));
        }

        static List<List<WaveBatch>> Templates(int count = 1)
        {
            var list = new List<List<WaveBatch>>();
            for (int i = 0; i < count; i++)
            {
                var e = ScriptableObject.CreateInstance<EnemyData>();
                e.type = i == 1 ? EnemyType.Tank : EnemyType.Chaser;
                e.maxHP = 20f;
                e.contactDamage = 5f;
                list.Add(new List<WaveBatch>
                {
                    new WaveBatch { enemy = e, count = 4, spawnInterval = 1f }
                });
            }
            return list;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    public enum WaveState { Prepare, Combat, Interlude, Forge, Shop, Choice, GameOver }

    /// <summary>波次状态机：Prepare→Combat→Interlude→Forge→Shop→(循环)。
    /// 脚本波(默认 20)通关后弹出 结算/无尽 选择，无尽波以模板循环、难度随周期递增。</summary>
    public class WaveManager : MonoBehaviour
    {
        WaveConfig config;
        EnemySpawner spawner;
        ShopSystem shop;
        ForgeSystem forge;
        PlayerStats stats;

        public WaveState State { get; private set; }
        public bool Victory { get; private set; }

        /// <summary>无尽模式每周期追加的数量/血量/伤害增幅。</summary>
        public const int EndlessCountPerCycle = 3;
        public const float EndlessHPMultPerCycle = 0.25f;
        public const float EndlessDamageMultPerCycle = 0.18f;

        public void BeginRun(WaveConfig cfg, EnemySpawner spawn, ShopSystem shopSystem, ForgeSystem forgeSystem, PlayerStats playerStats)
        {
            config = cfg;
            spawner = spawn;
            shop = shopSystem;
            forge = forgeSystem;
            stats = playerStats;
            Victory = false;
            StartCoroutine(RunLoop());
        }

        IEnumerator RunLoop()
        {
            List<List<WaveBatch>> waves = config.Waves();
            if (waves.Count == 0)
            {
                EndGame(true, 0);
                yield break;
            }

            int waveIndex = 0;   // 0-based；>= waves.Count 表示进入无尽循环
            bool endless = false;
            while (true)
            {
                List<WaveBatch> schedule = waveIndex < waves.Count ? waves[waveIndex] : BuildEndlessWave(waves, waveIndex + 1);
                int total = waveIndex < waves.Count ? waves.Count : -1; // -1 = 无尽

                State = WaveState.Prepare;
                SetInput(true);
                GameEvents.RaiseWave(waveIndex + 1, total);
                yield return new WaitForSeconds(config.prepareTime);
                if (stats.CurrentHP <= 0f) { EndGame(false, waveIndex); yield break; }

                State = WaveState.Combat;
                yield return StartCoroutine(CombatTick(schedule));
                if (stats.CurrentHP <= 0f) { EndGame(false, waveIndex); yield break; }

                State = WaveState.Interlude;
                yield return new WaitForSeconds(1f);

                // 锻体：花费 10 金币开启海克斯风格 3 选 1(无升阶/刷新)；金币不足则直接跳过
                State = WaveState.Forge;
                SetInput(false);
                if (forge.TryOpenForge())
                    yield return new WaitUntil(() => !forge.IsAwaitingChoice);
                SetInput(true);

                // 脚本波全通关(默认第 20 波)→ 结算 or 无尽
                if (!endless && waveIndex == waves.Count - 1)
                {
                    State = WaveState.Choice;
                    SetInput(false);
                    bool chosen = false;
                    Action<bool> onChosen = e => { endless = e; chosen = true; };
                    GameEvents.EndlessChosen += onChosen;
                    GameEvents.RaiseEndlessChoiceOffered();
                    yield return new WaitUntil(() => chosen);
                    GameEvents.EndlessChosen -= onChosen;
                    if (!endless) { EndGame(true, waves.Count); yield break; }
                }
                waveIndex++;

                // 商店：未通关时每波后开放；进入无尽后每波都开放
                if (endless || waveIndex < waves.Count)
                {
                    State = WaveState.Shop;
                    SetInput(false);
                    shop.OpenOffer();
                    yield return new WaitUntil(() => !shop.IsAwaitingChoice);
                    SetInput(true);
                }
            }
        }

        /// <summary>战斗阶段：启动刷怪并倒计时。清怪可提前结束；时间到→残敌消失、场上金币自动入库。</summary>
        IEnumerator CombatTick(List<WaveBatch> schedule)
        {
            spawner.StartWave(schedule);
            float remaining = config.combatTime;
            while (remaining > 0f)
            {
                if (stats.CurrentHP <= 0f) yield break;
                if (spawner.WaveComplete) break; // 提前清完直接进锻体
                remaining -= Time.deltaTime;
                GameEvents.RaiseCombatTime(Mathf.Max(0f, remaining));
                yield return null;
            }
            GameEvents.RaiseCombatTime(0f);

            bool timeout = !spawner.WaveComplete; // 时间到时场上仍有残留
            if (timeout)
            {
                spawner.StopWave();
                foreach (Enemy e in new List<Enemy>(EnemyRegistry.All))
                    if (e != null) PoolManager.Return(e.gameObject); // 残敌消失：不记账、不掉金币
            }
            PickupFactory.BankAll(); // 未拾取金币自动入库，合并到下回合使用
        }

        /// <summary>生成无尽波：按波形(第 waveIndex 波)对模板取模循环，数量/血量/伤害随周期递增。</summary>
        public static List<WaveBatch> BuildEndlessWave(IReadOnlyList<List<WaveBatch>> templates, int waveIndex)
        {
            var result = new List<WaveBatch>();
            if (templates == null || templates.Count == 0) return result;
            int cycle = (waveIndex - 1) / templates.Count;
            List<WaveBatch> src = templates[(waveIndex - 1) % templates.Count];
            foreach (WaveBatch b in src)
                result.Add(new WaveBatch
                {
                    enemy = CloneScaled(b.enemy, cycle),
                    count = b.count + cycle * EndlessCountPerCycle,
                    spawnInterval = b.spawnInterval
                });
            return result;
        }

        static EnemyData CloneScaled(EnemyData src, int cycle)
        {
            var e = ScriptableObject.CreateInstance<EnemyData>();
            e.type = src.type;
            e.maxHP = src.maxHP * (1f + EndlessHPMultPerCycle * cycle);
            e.moveSpeed = src.moveSpeed;
            e.contactDamage = src.contactDamage * (1f + EndlessDamageMultPerCycle * cycle);
            e.attackInterval = src.attackInterval;
            e.keepDistance = src.keepDistance;
            e.range = src.range;
            e.projectileDamage = src.projectileDamage * (1f + EndlessDamageMultPerCycle * cycle);
            e.projectileSpeed = src.projectileSpeed;
            e.goldMin = src.goldMin;
            e.goldMax = src.goldMax;
            e.scale = src.scale;
            e.color = src.color;
            return e;
        }

        void EndGame(bool victory, int wavesCleared)
        {
            State = WaveState.GameOver;
            Victory = victory;
            SetInput(false);
            GameEvents.RaiseGameEnded(victory, wavesCleared, stats.Kills);
        }

        static void SetInput(bool enabled)
        {
            if (PlayerController.Instance != null)
                PlayerController.Instance.inputEnabled = enabled;
        }
    }
}
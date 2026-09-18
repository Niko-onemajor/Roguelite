using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    public enum WaveState { Prepare, Combat, Interlude, Rune, Shop, Choice, GameOver }

    /// <summary>波次状态机：Prepare→Combat→Interlude→Shop→(循环)，第1/7/11/15波前插入 Rune 符文选择。
    /// 脚本波(默认 20)通关后弹出 结算/无尽 选择，无尽波以模板循环、难度随周期递增；
    /// 怪物血量/护甲/魔抗随波数(脚本)与周期(无尽)成长。锻体由商店内按钮触发，不再自动弹出。</summary>
    public class WaveManager : MonoBehaviour
    {
        WaveConfig config;
        EnemySpawner spawner;
        ShopSystem shop;
        RuneSystem rune;
        PlayerStats stats;

        public WaveState State { get; private set; }
        public bool Victory { get; private set; }
        int _currentWave; // 当前正在进行的 0-based 波号，供暂停页“结算游戏”使用

        /// <summary>无尽模式每周期追加的数量/血量/伤害/护甲魔抗增幅。</summary>
        public const int EndlessCountPerCycle = 3;
        public const float EndlessHPMultPerCycle = 0.25f;
        public const float EndlessDamageMultPerCycle = 0.18f;
        public const float EndlessDefenseGainPerCycle = 3f; // 护甲/魔抗每周期 +3

        /// <summary>脚本波(1~20)难度成长：每波血量 +4%、护甲/魔抗 +0.5。</summary>
        public const float ScriptedHPMultPerWave = 0.04f;
        public const float ScriptedDefenseGainPerWave = 0.5f;

        /// <summary>开局(第1波前)选择的 1-based 波号；第4/9/14/19...波则在进入商店前选择(每5回合一次)。</summary>
        public static readonly int[] RuneWaves = { 1 };

        public void BeginRun(WaveConfig cfg, EnemySpawner spawn, ShopSystem shopSystem, RuneSystem runeSystem, PlayerStats playerStats)
        {
            config = cfg;
            spawner = spawn;
            shop = shopSystem;
            rune = runeSystem;
            stats = playerStats;
            Victory = false;
            _currentWave = 0;
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
                _currentWave = waveIndex; // 记录当前波号，供暂停页“结算游戏”统计
                int waveNo = waveIndex + 1; // 1-based
                List<WaveBatch> schedule = waveIndex < waves.Count
                    ? ScaleScriptedWave(waves[waveIndex], waveNo)
                    : BuildEndlessWave(waves, waveNo);
                int total = waveIndex < waves.Count ? waves.Count : -1; // -1 = 无尽

                // 开局/第7/11/15波开始时弹出符文选择(海克斯 3 选 1，每张 1 次刷新)
                if (rune != null && System.Array.IndexOf(RuneWaves, waveNo) >= 0)
                {
                    State = WaveState.Rune;
                    SetInput(false);
                    rune.OpenOffer();
                    yield return new WaitUntil(() => !rune.IsAwaitingChoice);
                    SetInput(true);
                    if (stats.CurrentHP <= 0f) { EndGame(false, waveIndex); yield break; }
                }

                State = WaveState.Prepare;
                SetInput(true);
                GameEvents.RaiseWave(waveNo, total);
                yield return new WaitForSeconds(config.prepareTime);
                if (stats.CurrentHP <= 0f) { EndGame(false, waveIndex); yield break; }

                State = WaveState.Combat;
                yield return StartCoroutine(CombatTick(schedule));
                if (stats.CurrentHP <= 0f) { EndGame(false, waveIndex); yield break; }

                State = WaveState.Interlude;
                yield return new WaitForSeconds(1f);

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
                    // 符文：第4/9/14/19...波(waveNo%5==4)在进入商店之前选择(每5回合一次)
                    if (waveNo >= 4 && waveNo % 5 == 4)
                    {
                        State = WaveState.Rune;
                        SetInput(false);
                        rune.OpenOffer();
                        yield return new WaitUntil(() => !rune.IsAwaitingChoice);
                        SetInput(true);
                        if (stats.CurrentHP <= 0f) { EndGame(false, waveIndex); yield break; }
                    }

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
                if (spawner.WaveComplete) break; // 提前清完直接进商店
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

        /// <summary>脚本波难度缩放：按波数线性提升 血量/护甲/魔抗(伤害已由配置逐波上调)。</summary>
        public static List<WaveBatch> ScaleScriptedWave(IReadOnlyList<WaveBatch> schedule, int waveNumber)
        {
            var result = new List<WaveBatch>(schedule.Count);
            float hpMult = 1f + ScriptedHPMultPerWave * (waveNumber - 1);
            float defenseGain = ScriptedDefenseGainPerWave * (waveNumber - 1);
            foreach (WaveBatch b in schedule)
                result.Add(new WaveBatch
                {
                    enemy = CloneScaled(b.enemy, hpMult, 1f, defenseGain),
                    count = b.count,
                    spawnInterval = b.spawnInterval
                });
            return result;
        }

        static EnemyData CloneScaled(EnemyData src, int cycle) =>
            CloneScaled(src,
                1f + EndlessHPMultPerCycle * cycle,
                1f + EndlessDamageMultPerCycle * cycle,
                EndlessDefenseGainPerCycle * cycle);

        static EnemyData CloneScaled(EnemyData src, float hpMult, float dmgMult, float defenseGain)
        {
            var e = ScriptableObject.CreateInstance<EnemyData>();
            e.type = src.type;
            e.maxHP = src.maxHP * hpMult;
            e.moveSpeed = src.moveSpeed;
            e.contactDamage = src.contactDamage * dmgMult;
            e.attackInterval = src.attackInterval;
            e.keepDistance = src.keepDistance;
            e.range = src.range;
            e.projectileDamage = src.projectileDamage * dmgMult;
            e.projectileSpeed = src.projectileSpeed;
            e.goldMin = src.goldMin;
            e.goldMax = src.goldMax;
            e.scale = src.scale;
            e.color = src.color;
            e.armor = src.armor + defenseGain;          // 护甲随难度成长
            e.magicResist = src.magicResist + defenseGain; // 魔抗随难度成长
            return e;
        }

        /// <summary>暂停页“结算游戏”：立即终止波次状态机并以当前波数结算（手动结束按失败处理）。
        /// StopAllCoroutines 清掉 RunLoop/CombatTick，避免结束面板出现后状态机继续推进。</summary>
        public void EndRunNow(bool victory)
        {
            StopAllCoroutines();
            if (spawner != null) spawner.StopWave();
            EndGame(victory, _currentWave);
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
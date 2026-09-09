using System.Collections;
using UnityEngine;

namespace Roguelite
{
    public enum WaveState { Prepare, Combat, Interlude, Shop, GameOver }

    /// <summary>波次状态机：Prepare→Combat→Interlude→Shop→(循环)→GameOver。</summary>
    public class WaveManager : MonoBehaviour
    {
        WaveConfig config;
        EnemySpawner spawner;
        ShopSystem shop;
        PlayerStats stats;

        public WaveState State { get; private set; }
        public bool Victory { get; private set; }

        public void BeginRun(WaveConfig cfg, EnemySpawner spawn, ShopSystem shopSystem, PlayerStats playerStats)
        {
            config = cfg;
            spawner = spawn;
            shop = shopSystem;
            stats = playerStats;
            Victory = false;
            StartCoroutine(RunLoop());
        }

        IEnumerator RunLoop()
        {
            System.Collections.Generic.List<System.Collections.Generic.List<WaveBatch>> waves = config.Waves();
            int waveIndex = 0;
            while (waveIndex < waves.Count)
            {
                State = WaveState.Prepare;
                SetInput(true);
                GameEvents.RaiseWave(waveIndex + 1, waves.Count);
                yield return new WaitForSeconds(config.prepareTime);
                if (stats.CurrentHP <= 0f) { EndGame(false, waveIndex); yield break; }

                State = WaveState.Combat;
                spawner.StartWave(waves[waveIndex]);
                while (!spawner.WaveComplete)
                {
                    if (stats.CurrentHP <= 0f) { EndGame(false, waveIndex); yield break; }
                    yield return null;
                }

                State = WaveState.Interlude;
                yield return new WaitForSeconds(1f);

                if (waveIndex == waves.Count - 1)
                {
                    EndGame(true, waveIndex + 1);
                    yield break;
                }

                State = WaveState.Shop;
                SetInput(false);
                shop.OpenOffer();
                yield return new WaitUntil(() => !shop.IsAwaitingChoice);
                SetInput(true);
                waveIndex++;
            }
            EndGame(true, waves.Count);
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
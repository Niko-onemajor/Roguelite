using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>运行时特效/音效播放器：全部由代码构建，不依赖任何素材。
    /// 粒子爆发(升级金光)+ 程序生成的正弦音效(上升音高)。
    /// EditMode 测试(非 play mode)自动跳过，避免批处理环境创建游戏对象。</summary>
    public static class FxPlayer
    {
        static readonly Dictionary<float, AudioClip> clips = new Dictionary<float, AudioClip>();

        /// <summary>升级(锻体生效)反馈：玩家位置金光粒子爆发 + 稀有度音高音效。</summary>
        public static void LevelUp(Vector3 worldPos, Color color, float pitch)
        {
            if (!Application.isPlaying) return;
            Burst(worldPos, color, 28);
            Beep(pitch, 0.22f, 0.35f);
        }

        /// <summary>在指定位置爆发一圈彩色粒子，短暂存活后自动销毁。</summary>
        public static void Burst(Vector3 worldPos, Color color, int count = 28)
        {
            var go = new GameObject("Fx_Burst");
            go.transform.position = worldPos;
            var ps = go.AddComponent<ParticleSystem>();
            // AddComponent 默认 playOnAwake=true，挂上即自动播放；先停止并清空使其进入停止态，
            // 否则随后修改 duration 等参数会报 “Setting the duration while system is still playing”
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.55f;
            main.startSpeed = 5f;
            main.startSize = 0.45f;
            main.startColor = color;
            main.gravityModifier = 0f;
            main.maxParticles = count * 2;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;

            ps.Play();
            Object.Destroy(go, 1.2f);
        }

        /// <summary>播放短促正弦音(上升感)，clip 按频率缓存复用。</summary>
        public static void Beep(float frequency, float duration = 0.18f, float volume = 0.4f)
        {
            AudioClip clip = GetClip(frequency, duration);
            AudioSource.PlayClipAtPoint(clip, Vector3.zero, volume);
        }

        static AudioClip GetClip(float freq, float duration)
        {
            if (clips.TryGetValue(freq, out AudioClip cached) && cached != null)
                return cached;

            const int sampleRate = 22050;
            int samples = Mathf.Max(1, (int)(duration * sampleRate));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = 1f - t / duration; // 线性衰减，避免爆音
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Clamp01(env);
            }
            var clip = AudioClip.Create("fx_beep", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            clips[freq] = clip;
            return clip;
        }
    }
}
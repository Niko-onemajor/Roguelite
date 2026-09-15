using UnityEngine;

namespace Roguelite
{
    /// <summary>有限圆形竞技场：所有参战单位(玩家/敌人)位移都被钳位在圆内，
    /// 刷怪点取圆周附近随机角，保证敌人入场即合法不越界。</summary>
    public static class ArenaBounds
    {
        public const float Radius = 10f;       // 竞技场半径(世界单位)
        const float SpawnRingRadius = 9f;      // 刷怪环半径(略小于边界，避免刚生成就出界)
        const float SpawnMargin = 0.3f;        // 钳位时预留的边界余量(占位 sprite 视觉贴合用)

        /// <summary>把二维位置钳位到竞技场圆内(向圆心投影)；在圈内则原样返回。</summary>
        public static Vector2 Clamp(Vector2 pos)
        {
            float limit = Radius - SpawnMargin;
            float mag = pos.magnitude;
            if (mag > limit) pos = pos.normalized * limit;
            return pos;
        }

        /// <summary>随机刷怪点：角度均匀、半径固定在刷怪环上。</summary>
        public static Vector2 RandomRingPosition()
        {
            float angle = Random.value * (Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle) * SpawnRingRadius, Mathf.Sin(angle) * SpawnRingRadius);
        }

        /// <summary>判断一点是否在竞技场有效范围内(用于测试断言)。</summary>
        public static bool Contains(Vector2 pos) => pos.magnitude <= Radius + 0.001f;
    }
}
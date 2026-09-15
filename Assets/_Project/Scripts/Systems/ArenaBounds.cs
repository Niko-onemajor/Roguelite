using UnityEngine;

namespace Roguelite
{
    /// <summary>有限正方形竞技场：所有参战单位(玩家/敌人)位移都被钳位在方形内，
    /// 刷怪点取方形边界附近随机点，保证敌人入场即合法不越界。</summary>
    public static class ArenaBounds
    {
        public const float Radius = 10f;       // 竞技场半边长(世界单位，整场 20x20)
        const float SpawnRingRadius = 9f;      // 刷怪环半边长(略小于边界，避免刚生成就出界)
        const float SpawnMargin = 0.3f;        // 钳位时预留的边界余量(占位 sprite 视觉贴合用)

        /// <summary>把二维位置钳位到竞技场方形内(逐轴夹取)；在方形内则原样返回。</summary>
        public static Vector2 Clamp(Vector2 pos)
        {
            float limit = Radius - SpawnMargin;
            pos.x = Mathf.Clamp(pos.x, -limit, limit);
            pos.y = Mathf.Clamp(pos.y, -limit, limit);
            return pos;
        }

        /// <summary>随机刷怪点：落在方形刷怪环(四条边的中间段)上。</summary>
        public static Vector2 RandomRingPosition()
        {
            int edge = Random.Range(0, 4); // 0=下,1=上,2=左,3=右
            float c = Random.Range(-SpawnRingRadius, SpawnRingRadius);
            switch (edge)
            {
                case 0: return new Vector2(c, -SpawnRingRadius);
                case 1: return new Vector2(c, SpawnRingRadius);
                case 2: return new Vector2(-SpawnRingRadius, c);
                default: return new Vector2(SpawnRingRadius, c);
            }
        }

        /// <summary>判断一点是否在竞技场有效范围内(用于测试断言)。</summary>
        public static bool Contains(Vector2 pos) =>
            Mathf.Abs(pos.x) <= Radius + 0.001f && Mathf.Abs(pos.y) <= Radius + 0.001f;
    }
}
using UnityEngine;

namespace Roguelite
{
    /// <summary>正交相机跟随组件：以玩家为中心移动，x/y 与玩家对齐，z 保持 -10。
    /// 玩家不存在时归位原点，保证竞技场任何阶段相机都不会漂走。</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0f, 0f, -10f);

        void LateUpdate()
        {
            Vector3 target = Vector3.zero;
            if (PlayerController.Instance != null)
                target = PlayerController.Instance.transform.position;
            transform.position = new Vector3(target.x, target.y, offset.z);
        }
    }
}
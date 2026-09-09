using UnityEngine;

namespace Roguelite
{
    public static partial class PoolManager
    {
        /// <summary>桩：Task 7 完整实现。</summary>
        public static GameObject Spawn(GameObject prototype, Vector3 pos, Quaternion rot) => null;

        /// <summary>桩：Task 7 完整实现。</summary>
        public static void Return(GameObject go) => UnityEngine.Object.Destroy(go);
    }
}
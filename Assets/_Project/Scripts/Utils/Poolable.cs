using UnityEngine;

namespace Roguelite
{
    /// <summary>挂在池化原型上，携带原型 key；Return 时按 key 归池。</summary>
    public class Poolable : MonoBehaviour
    {
        public int Key { get; set; }
    }
}
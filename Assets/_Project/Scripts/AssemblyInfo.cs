using System.Runtime.CompilerServices;

// 允许 EditMode 测试程序集访问 internal 成员，便于直接驱动内部行为(如 Enemy.TryContactDamage)。
[assembly: InternalsVisibleTo("Roguelite.Tests")]
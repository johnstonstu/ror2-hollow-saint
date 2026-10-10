// Explicit Unity lifetime adapters. Tests drive callbacks; they do not certify
// Unity execution order or destruction timing in a running game.
namespace UnityEngine
{
    public class Object
    {
        public bool destroyed;
        public static implicit operator bool(Object value) => value != null && !value.destroyed;
    }
    public class MonoBehaviour : Object
    {
        public RoR2.HealthComponent attachedOwner;
        public T GetComponent<T>() where T : class => attachedOwner as T;
    }
    public static class Time { public static float time; }
    public static class Mathf { public static float Max(float a, float b) => Math.Max(a, b); }
}
namespace RoR2
{
    public class HealthComponent : UnityEngine.Object { public bool alive = true, isActiveAndEnabled = true; }
    public class Stage : UnityEngine.Object { public static Stage instance = new(); }
}

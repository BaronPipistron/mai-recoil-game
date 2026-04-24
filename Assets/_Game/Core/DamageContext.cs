using UnityEngine;

namespace RecoilArena.Core
{
    public readonly struct DamageContext
    {
        public readonly Transform Source;
        public readonly Vector3 HitPoint;
        public readonly Vector3 Direction;
        public readonly bool IsWeakSpot;

        public DamageContext(Transform source, Vector3 hitPoint, Vector3 direction, bool isWeakSpot)
        {
            Source = source;
            HitPoint = hitPoint;
            Direction = direction;
            IsWeakSpot = isWeakSpot;
        }
    }
}

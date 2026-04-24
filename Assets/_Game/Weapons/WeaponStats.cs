using UnityEngine;

namespace RecoilArena.Weapons
{
    [CreateAssetMenu(menuName = "RecoilArena/Weapon Stats", fileName = "ShotgunStats")]
    public class WeaponStats : ScriptableObject
    {
        [Header("Damage")]
        public float pelletDamage = 10.5f;
        public int pelletCount = 10;
        public float spreadAngle = 8.5f;
        public float range = 32f;
        public float weakSpotBonusMultiplier = 2.1f;

        [Header("Handling")]
        public float fireCooldown = 0.9f;
        public int clipSize = 4;
        public int reserveAmmo = 36;
        public float reloadDuration = 2f;

        [Header("Recoil")]
        public float recoilImpulse = 18f;
        public float recoilVerticalLift = 0f;
    }
}

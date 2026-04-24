using UnityEngine;

namespace RecoilArena.Enemies
{
    [CreateAssetMenu(menuName = "RecoilArena/Enemy Stats", fileName = "EnemyStats")]
    public class EnemyStats : ScriptableObject
    {
        [Header("Core")]
        public string enemyName = "Enemy";
        public float maxHealth = 40f;
        public float moveSpeed = 4f;
        public float acceleration = 14f;
        public float turnSpeed = 720f;

        [Header("Combat")]
        public float attackRange = 1.9f;
        public float attackDamage = 12f;
        public float attackCooldown = 1.2f;
        public int scoreReward = 100;

        [Header("Weak Spot")]
        public float weakSpotMultiplier = 2f;
        public float weakSpotMinShiftInterval = 1.1f;
        public float weakSpotMaxShiftInterval = 2.4f;
        public float weakSpotMoveSpeed = 5f;
        public Vector3[] weakSpotOffsets =
        {
            new Vector3(0f, 0.35f, 0.42f),
            new Vector3(0.35f, 0.2f, 0f),
            new Vector3(-0.35f, 0.2f, 0f),
            new Vector3(0f, -0.1f, -0.38f)
        };

        [Header("Presentation")]
        public Vector3 bodyScale = new Vector3(1f, 1f, 1f);
        public Color bodyColor = new Color(0.45f, 0.8f, 1f);
        public Color weakSpotColor = new Color(1f, 0.26f, 0.18f);
    }
}

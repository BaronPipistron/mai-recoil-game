using RecoilArena.Player;
using UnityEngine;

namespace RecoilArena.Arena
{
    public class HazardVolume : MonoBehaviour
    {
        [SerializeField] private float damagePerSecond = 35f;
        [SerializeField] private float safeHeightY = -0.35f;

        private void OnTriggerStay(Collider other)
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                if (health.transform.position.y > safeHeightY)
                {
                    return;
                }

                health.ApplyDamage(damagePerSecond * Time.deltaTime);
            }
        }
    }
}

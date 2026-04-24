using RecoilArena.Player;
using UnityEngine;

namespace RecoilArena.Arena
{
    public class KillVolume : MonoBehaviour
    {
        [SerializeField] private float killDamage = 500f;

        private void OnTriggerEnter(Collider other)
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.ApplyDamage(killDamage);
            }
        }
    }
}

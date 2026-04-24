using System;
using UnityEngine;

namespace RecoilArena.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [Header("Audio")]
        [SerializeField] private AudioSource damageAudioSource;
        [SerializeField] private AudioClip damageClip;
        [SerializeField] private float damageVolume = 0.72f;
        [SerializeField] private float damageSoundCooldown = 0.08f;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;
        private float _nextDamageSoundTime;

        private void Awake()
        {
            CurrentHealth = maxHealth;

            if (damageAudioSource == null)
            {
                damageAudioSource = GetComponent<AudioSource>();
            }

            if (damageClip == null)
            {
                damageClip = Resources.Load<AudioClip>("Audio/misc_09");
            }
        }

        public void ResetHealth(float? newMaxHealth = null)
        {
            if (newMaxHealth.HasValue)
            {
                maxHealth = Mathf.Max(10f, newMaxHealth.Value);
            }

            CurrentHealth = maxHealth;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void AddMaxHealth(float amount)
        {
            maxHealth = Mathf.Clamp(maxHealth + amount, 25f, 300f);
            CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void Heal(float amount)
        {
            if (!IsAlive)
            {
                return;
            }

            CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            PlayDamageSound();
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f)
            {
                Died?.Invoke();
            }
        }

        private void PlayDamageSound()
        {
            if (damageAudioSource == null || damageClip == null)
            {
                return;
            }

            if (Time.time < _nextDamageSoundTime)
            {
                return;
            }

            damageAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.06f);
            damageAudioSource.PlayOneShot(damageClip, damageVolume);
            _nextDamageSoundTime = Time.time + damageSoundCooldown;
        }
    }
}

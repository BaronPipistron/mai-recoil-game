using RecoilArena.Core;
using RecoilArena.Player;
using RecoilArena.Waves;
using UnityEngine;
using UnityEngine.AI;

namespace RecoilArena.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyStats stats;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private WeakSpot weakSpot;

        private NavMeshAgent _agent;
        private PlayerHealth _targetHealth;
        private Transform _targetTransform;
        private WaveDirector _waveDirector;
        private float _currentHealth;
        private float _nextAttackTime;
        private bool _isActive;
        private bool _isAlive;
        private Material _bodyMaterial;
        private GameObject _runtimeVisual;
        private const string RusherPrimaryModelKey = "Models/Mike";
        private const string RusherFallbackModelKey = "Models/SmallerModel";
        private const string BulwarkPrimaryModelKey = "Models/Stan";
        private const string BulwarkFallbackModelKey = "Models/BaseModel";
        private const string RusherPrimaryTextureKey = "Models/Mike_Texture";
        private const string BulwarkPrimaryTextureKey = "Models/Stan_Texture";
        private const float RusherVisualHeight = 1.7f;
        private const float BulwarkVisualHeight = 2.2f;
        private static bool _loggedMissingRusherModel;
        private static bool _loggedMissingBulwarkModel;
        private static bool _loggedMissingEnemyShader;
        private static Material _cachedRusherMaterial;
        private static Material _cachedBulwarkMaterial;

        public EnemyStats Stats => stats;
        public bool IsAlive => _isAlive;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (weakSpot == null)
            {
                weakSpot = GetComponentInChildren<WeakSpot>();
            }

            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (bodyRenderer != null)
            {
                _bodyMaterial = bodyRenderer.material;
            }

            ApplyStatsVisuals();
        }

        private void Update()
        {
            if (!_isActive || !_isAlive || _targetTransform == null || _targetHealth == null || !_targetHealth.IsAlive)
            {
                if (_agent.enabled)
                {
                    _agent.isStopped = true;
                }

                return;
            }

            _agent.SetDestination(_targetTransform.position);

            float sqrDistance = (_targetTransform.position - transform.position).sqrMagnitude;
            float attackRange = stats.attackRange;

            if (sqrDistance <= attackRange * attackRange && Time.time >= _nextAttackTime)
            {
                _targetHealth.ApplyDamage(stats.attackDamage);
                _nextAttackTime = Time.time + stats.attackCooldown;
            }
        }

        public void Setup(EnemyStats enemyStats, Transform targetTransform, PlayerHealth targetHealth, WaveDirector waveDirector)
        {
            stats = enemyStats;
            _targetTransform = targetTransform;
            _targetHealth = targetHealth;
            _waveDirector = waveDirector;

            _currentHealth = stats.maxHealth;
            _isAlive = true;
            _isActive = true;
            _nextAttackTime = Time.time + Random.Range(0.2f, 0.8f);

            EnsureRuntimeModel();
            ApplyStatsVisuals();

            if (_agent != null)
            {
                _agent.enabled = true;
                _agent.speed = stats.moveSpeed;
                _agent.acceleration = stats.acceleration;
                _agent.angularSpeed = stats.turnSpeed;
                _agent.stoppingDistance = Mathf.Max(0.5f, stats.attackRange * 0.75f);
                _agent.isStopped = false;
            }

            if (weakSpot != null)
            {
                weakSpot.Setup(this, stats);
            }
        }

        public void ApplyDamage(float amount, DamageContext context)
        {
            if (!_isAlive || amount <= 0f)
            {
                return;
            }

            _currentHealth -= amount;

            if (context.IsWeakSpot && weakSpot != null)
            {
                weakSpot.PlayHitFeedback();
            }

            if (_currentHealth <= 0f)
            {
                Die(context.IsWeakSpot);
            }
        }

        private void ApplyStatsVisuals()
        {
            transform.localScale = (_runtimeVisual == null && stats != null) ? stats.bodyScale : Vector3.one;

            if (_bodyMaterial != null && stats != null)
            {
                if (_bodyMaterial.HasProperty("_BaseColor"))
                {
                    _bodyMaterial.SetColor("_BaseColor", stats.bodyColor);
                }

                if (_bodyMaterial.HasProperty("_Color"))
                {
                    _bodyMaterial.color = stats.bodyColor;
                }
            }
        }

        private void EnsureRuntimeModel()
        {
            if (stats == null || _runtimeVisual != null)
            {
                return;
            }

            bool isBulwark = stats.enemyName == "Bulwark";
            string primaryModelKey = isBulwark ? BulwarkPrimaryModelKey : RusherPrimaryModelKey;
            string fallbackModelKey = isBulwark ? BulwarkFallbackModelKey : RusherFallbackModelKey;
            GameObject modelAsset = Resources.Load<GameObject>(primaryModelKey);
            if (modelAsset == null)
            {
                modelAsset = Resources.Load<GameObject>(fallbackModelKey);
            }

            if (modelAsset == null)
            {
                if (isBulwark)
                {
                    if (!_loggedMissingBulwarkModel)
                    {
                        Debug.LogWarning($"Enemy model not found at Resources/{primaryModelKey} or Resources/{fallbackModelKey}.");
                        _loggedMissingBulwarkModel = true;
                    }
                }
                else
                {
                    if (!_loggedMissingRusherModel)
                    {
                        Debug.LogWarning($"Enemy model not found at Resources/{primaryModelKey} or Resources/{fallbackModelKey}.");
                        _loggedMissingRusherModel = true;
                    }
                }
                return;
            }

            _runtimeVisual = Instantiate(modelAsset, transform);
            _runtimeVisual.name = "RuntimeVisual";
            _runtimeVisual.transform.localPosition = Vector3.zero;
            _runtimeVisual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            _runtimeVisual.transform.localScale = Vector3.one;

            foreach (Collider col in _runtimeVisual.GetComponentsInChildren<Collider>(true))
            {
                Destroy(col);
            }

            Renderer[] renderers = _runtimeVisual.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            if (TryGetCombinedBounds(renderers, out Bounds initialBounds))
            {
                float targetHeight = isBulwark ? BulwarkVisualHeight : RusherVisualHeight;
                float height = Mathf.Max(0.05f, initialBounds.size.y);
                float scaleMultiplier = targetHeight / height;
                _runtimeVisual.transform.localScale = Vector3.one * scaleMultiplier;

                if (TryGetCombinedBounds(renderers, out Bounds scaledBounds))
                {
                    float bottomOffset = scaledBounds.min.y - transform.position.y;
                    _runtimeVisual.transform.position -= Vector3.up * bottomOffset;
                }
            }

            Material runtimeMaterial = isBulwark
                ? (_cachedBulwarkMaterial ??= BuildRuntimeEnemyMaterial(true))
                : (_cachedRusherMaterial ??= BuildRuntimeEnemyMaterial(false));

            if (runtimeMaterial != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    Material[] materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                    {
                        renderer.sharedMaterial = runtimeMaterial;
                        continue;
                    }

                    for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        materials[materialIndex] = runtimeMaterial;
                    }

                    renderer.sharedMaterials = materials;
                }
            }

            Renderer runtimeRenderer = renderers[0];
            if (bodyRenderer != null && bodyRenderer.GetComponentInParent<WeakSpot>() == null)
            {
                bodyRenderer.enabled = false;
            }

            bodyRenderer = runtimeRenderer;
            _bodyMaterial = bodyRenderer.sharedMaterial != null ? bodyRenderer.sharedMaterial : bodyRenderer.material;
        }

        private Material BuildRuntimeEnemyMaterial(bool isTank)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                if (!_loggedMissingEnemyShader)
                {
                    Debug.LogWarning("No compatible shader found for runtime enemy material.");
                    _loggedMissingEnemyShader = true;
                }

                return null;
            }

            Material material = new Material(shader);
            Color tint = stats != null ? stats.bodyColor : Color.white;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }

            Texture2D texture = isTank
                ? Resources.Load<Texture2D>(BulwarkPrimaryTextureKey)
                : Resources.Load<Texture2D>(RusherPrimaryTextureKey);

            if (texture == null)
            {
                texture = isTank
                    ? Resources.Load<Texture2D>("Models/redTEX")
                    : Resources.Load<Texture2D>("Models/blueTEX");
            }

            if (texture != null)
            {
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                }

                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", texture);
                }
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", tint * 0.22f);
            }

            return material;
        }

        private static bool TryGetCombinedBounds(Renderer[] renderers, out Bounds bounds)
        {
            if (renderers == null || renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private void Die(bool weakSpotKill)
        {
            _isAlive = false;
            _isActive = false;

            if (_agent != null && _agent.enabled)
            {
                _agent.isStopped = true;
                _agent.enabled = false;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            if (_waveDirector != null)
            {
                _waveDirector.NotifyEnemyKilled(this, weakSpotKill);
            }

            Destroy(gameObject, 0.35f);
        }
    }
}

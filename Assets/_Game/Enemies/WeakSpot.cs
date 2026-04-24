using RecoilArena.Core;
using UnityEngine;

namespace RecoilArena.Enemies
{
    public class WeakSpot : MonoBehaviour, IDamageable
    {
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private float pulseSpeed = 6f;
        [SerializeField] private float pulseIntensity = 2.6f;

        private EnemyBase _owner;
        private EnemyStats _stats;
        private int _currentOffsetIndex;
        private Vector3 _targetOffset;
        private float _nextShiftTime;
        private Material _material;
        private Color _baseColor = Color.red;
        private float _feedbackTimer;

        public bool IsAlive => _owner != null && _owner.IsAlive;

        private void Awake()
        {
            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<Renderer>();
            }

            if (visualRenderer != null)
            {
                _material = visualRenderer.material;
            }
        }

        private void Update()
        {
            if (_owner == null || _stats == null || !_owner.IsAlive)
            {
                return;
            }

            if (Time.time >= _nextShiftTime)
            {
                PickNextOffset();
                ScheduleNextShift();
            }

            transform.localPosition = Vector3.Lerp(transform.localPosition, _targetOffset, Time.deltaTime * _stats.weakSpotMoveSpeed);
            AnimateEmission();

            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
            }
        }

        public void Setup(EnemyBase owner, EnemyStats stats)
        {
            _owner = owner;
            _stats = stats;
            _baseColor = stats.weakSpotColor;

            if (_material != null)
            {
                _material.color = _baseColor;
            }

            _currentOffsetIndex = 0;
            _targetOffset = stats.weakSpotOffsets != null && stats.weakSpotOffsets.Length > 0
                ? stats.weakSpotOffsets[0]
                : new Vector3(0f, 1.2f, 0.4f);

            transform.localPosition = _targetOffset;
            ScheduleNextShift();
        }

        public void ApplyDamage(float amount, DamageContext context)
        {
            if (_owner == null || !_owner.IsAlive)
            {
                return;
            }

            DamageContext weakContext = new DamageContext(context.Source, context.HitPoint, context.Direction, true);
            _owner.ApplyDamage(amount * _stats.weakSpotMultiplier, weakContext);
            PlayHitFeedback();
        }

        public void PlayHitFeedback()
        {
            _feedbackTimer = 0.25f;
        }

        private void PickNextOffset()
        {
            if (_stats.weakSpotOffsets == null || _stats.weakSpotOffsets.Length == 0)
            {
                return;
            }

            if (_stats.weakSpotOffsets.Length == 1)
            {
                _targetOffset = _stats.weakSpotOffsets[0];
                return;
            }

            int nextIndex = _currentOffsetIndex;
            int guard = 0;
            while (nextIndex == _currentOffsetIndex && guard < 12)
            {
                nextIndex = Random.Range(0, _stats.weakSpotOffsets.Length);
                guard++;
            }

            _currentOffsetIndex = nextIndex;
            _targetOffset = _stats.weakSpotOffsets[nextIndex];
        }

        private void ScheduleNextShift()
        {
            float min = Mathf.Max(0.35f, _stats.weakSpotMinShiftInterval);
            float max = Mathf.Max(min + 0.05f, _stats.weakSpotMaxShiftInterval);
            _nextShiftTime = Time.time + Random.Range(min, max);
        }

        private void AnimateEmission()
        {
            if (_material == null)
            {
                return;
            }

            float pulse = 0.4f + Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)) * pulseIntensity;
            if (_feedbackTimer > 0f)
            {
                pulse += 2.5f;
            }

            _material.EnableKeyword("_EMISSION");
            _material.SetColor("_EmissionColor", _baseColor * pulse);
        }
    }
}

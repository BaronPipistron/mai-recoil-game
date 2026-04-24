using System;
using System.Collections;
using System.Collections.Generic;
using RecoilArena.Core;
using RecoilArena.Enemies;
using RecoilArena.Player;
using UnityEngine;

namespace RecoilArena.Weapons
{
    public class ShotgunWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponStats stats;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCameraEffects cameraEffects;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;
        [SerializeField] private ParticleSystem muzzleParticles;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource reloadAudioSource;
        [SerializeField] private AudioClip fireClip;
        [SerializeField] private AudioClip[] fireVariantClips;
        [SerializeField] private AudioClip reloadClip;
        [SerializeField] private AudioClip reloadCompleteClip;
        [SerializeField] private Transform weaponVisualRoot;
        [SerializeField] private float shotKickDistance = 0.085f;
        [SerializeField] private float shotKickRotation = 9f;
        [SerializeField] private float shotRecoverySpeed = 20f;
        [SerializeField] private float reloadTiltDegrees = 28f;
        [SerializeField] private float reloadDropDistance = 0.11f;
        [SerializeField] private float reloadSideOffset = 0.06f;
        [SerializeField] private int muzzleFlashParticles = 12;
        [SerializeField] private float muzzleFlashSpeed = 30f;
        [SerializeField] private float muzzleFlashLifetime = 0.06f;
        [SerializeField] private float muzzleFlashSpread = 6f;
        [SerializeField] private float tracerWidth = 0.05f;
        [SerializeField] private float tracerTravelSpeed = 160f;
        [SerializeField] private float tracerFadeDuration = 0.05f;

        private float _nextFireTime;
        private bool _controlsEnabled;
        private bool _isReloading;
        private Coroutine _reloadRoutine;
        private float _damageMultiplier = 1f;
        private float _reloadMultiplier = 1f;
        private float _baseDamageMultiplier = 1f;
        private float _baseReloadMultiplier = 1f;
        private Vector3 _weaponBaseLocalPosition;
        private Quaternion _weaponBaseLocalRotation = Quaternion.identity;
        private Vector3 _shotPositionOffset;
        private Vector3 _shotRotationOffset;
        private float _reloadAnimNormalized;
        private static bool _loggedMissingWeaponModel;
        private static bool _loggedMissingFireClip;
        private static bool _loggedMissingWeaponShader;
        private static bool _loggedMissingMuzzleFlashShader;
        private static bool _loggedMissingTracerShader;
        private static Material _runtimeWeaponMaterial;
        private static Material _runtimeMuzzleFlashMaterial;
        private static Material _runtimeTracerMaterial;

        public event Action<int, int> AmmoChanged;
        public event Action<bool, bool> ShotFeedback;

        public int AmmoInClip { get; private set; }
        public int ReserveAmmo { get; private set; }
        public bool IsReloading => _isReloading;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponentInParent<PlayerController>();
            }

            if (cameraEffects == null)
            {
                cameraEffects = GetComponentInParent<PlayerCameraEffects>();
            }

            if (aimCamera == null && playerController != null)
            {
                aimCamera = playerController.PlayerCamera;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            EnsureAudioSource();
            EnsureReloadAudioSource();
            EnsureDefaultAudioClips();
            EnsureWeaponVisualRoot();
            EnsureMuzzleParticles();
            EnsureRuntimeWeaponModel();
            CacheWeaponBaseTransform();
            RefillAllAmmo();
        }

        private void Update()
        {
            if (!_controlsEnabled)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                TryReload();
            }

            if (Input.GetButtonDown("Fire1"))
            {
                TryFire();
            }
        }

        private void LateUpdate()
        {
            if (weaponVisualRoot == null)
            {
                return;
            }

            float recoverSpeed = Mathf.Max(8f, shotRecoverySpeed);
            _shotPositionOffset = Vector3.Lerp(_shotPositionOffset, Vector3.zero, Time.deltaTime * recoverSpeed);
            _shotRotationOffset = Vector3.Lerp(_shotRotationOffset, Vector3.zero, Time.deltaTime * recoverSpeed);

            float reloadCurve = _isReloading ? Mathf.Sin(_reloadAnimNormalized * Mathf.PI) : 0f;
            Vector3 reloadPosition = new Vector3(
                reloadSideOffset * reloadCurve,
                -reloadDropDistance * reloadCurve,
                -reloadDropDistance * 0.34f * reloadCurve);
            Vector3 reloadRotation = new Vector3(
                8f * reloadCurve,
                -reloadTiltDegrees * reloadCurve,
                22f * reloadCurve);

            weaponVisualRoot.localPosition = _weaponBaseLocalPosition + _shotPositionOffset + reloadPosition;
            weaponVisualRoot.localRotation = _weaponBaseLocalRotation
                * Quaternion.Euler(_shotRotationOffset)
                * Quaternion.Euler(reloadRotation);
        }

        public void SetControlEnabled(bool enabled)
        {
            _controlsEnabled = enabled;
        }

        public void RefillAllAmmo()
        {
            AmmoInClip = stats != null ? stats.clipSize : 0;
            ReserveAmmo = stats != null ? stats.reserveAmmo : 0;
            _nextFireTime = 0f;
            StopReload();
            RaiseAmmoChanged();
        }

        public void AddDamageMultiplier(float delta)
        {
            _damageMultiplier = Mathf.Clamp(_damageMultiplier + delta, 0.5f, 2.4f);
        }

        public void AddReloadMultiplier(float delta)
        {
            _reloadMultiplier = Mathf.Clamp(_reloadMultiplier + delta, 0.35f, 1.5f);
        }

        public void AddAmmo(int amount)
        {
            ReserveAmmo = Mathf.Clamp(ReserveAmmo + amount, 0, 999);
            RaiseAmmoChanged();
        }

        public void ResetRuntimeTuning()
        {
            _damageMultiplier = _baseDamageMultiplier;
            _reloadMultiplier = _baseReloadMultiplier;
            StopReload();
            _nextFireTime = 0f;
        }

        private void TryFire()
        {
            if (stats == null || aimCamera == null || playerController == null)
            {
                return;
            }

            if (Time.time < _nextFireTime || _isReloading)
            {
                return;
            }

            if (AmmoInClip <= 0)
            {
                TryReload();
                return;
            }

            _nextFireTime = Time.time + stats.fireCooldown;
            AmmoInClip--;

            bool anyHit = false;
            bool weakHit = false;

            Vector3 rayOrigin = aimCamera.transform.position;
            Vector3 fireDirection = aimCamera.transform.forward;

            for (int i = 0; i < stats.pelletCount; i++)
            {
                Vector3 pelletDirection = ApplySpread(fireDirection, stats.spreadAngle);
                if (Physics.Raycast(rayOrigin, pelletDirection, out RaycastHit hit, stats.range, hitMask, QueryTriggerInteraction.Ignore))
                {
                    WeakSpot weakSpot = hit.collider.GetComponent<WeakSpot>();
                    bool isWeak = weakSpot != null;
                    IDamageable damageable = isWeak ? weakSpot : hit.collider.GetComponentInParent<IDamageable>();

                    if (damageable != null && damageable.IsAlive)
                    {
                        float damage = stats.pelletDamage * _damageMultiplier;
                        if (isWeak)
                        {
                            damage *= stats.weakSpotBonusMultiplier;
                            weakHit = true;
                        }

                        damageable.ApplyDamage(damage, new DamageContext(transform, hit.point, pelletDirection, isWeak));
                        anyHit = true;
                    }
                }
            }

            Vector3 tracerStart = muzzlePoint != null ? muzzlePoint.position : rayOrigin + fireDirection * 0.24f;
            Vector3 tracerEnd = ResolveTracerEndPoint(rayOrigin, fireDirection, stats.range);
            PlayShotTracer(tracerStart, tracerEnd, weakHit);

            playerController.ApplyShotgunRecoil(fireDirection, stats.recoilImpulse, stats.recoilVerticalLift);

            if (cameraEffects != null)
            {
                cameraEffects.PlayShotKick(weakHit ? 1.6f : 1.35f);
            }

            TriggerFireAnimation();

            if (muzzleParticles != null)
            {
                muzzleParticles.Play(true);
            }

            PlayFireSound();

            RaiseAmmoChanged();
            ShotFeedback?.Invoke(anyHit, weakHit);

            if (AmmoInClip <= 0 && ReserveAmmo > 0)
            {
                TryReload();
            }
        }

        private void TryReload()
        {
            if (_isReloading || stats == null)
            {
                return;
            }

            if (AmmoInClip >= stats.clipSize || ReserveAmmo <= 0)
            {
                return;
            }

            _reloadRoutine = StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            _isReloading = true;

            if (reloadAudioSource != null && reloadClip != null)
            {
                reloadAudioSource.pitch = UnityEngine.Random.Range(0.9f, 1.04f);
                reloadAudioSource.PlayOneShot(reloadClip, 1.05f);
            }

            float reloadDuration = Mathf.Max(0.25f, stats.reloadDuration * _reloadMultiplier);
            float elapsed = 0f;
            _reloadAnimNormalized = 0f;
            while (elapsed < reloadDuration)
            {
                elapsed += Time.deltaTime;
                _reloadAnimNormalized = Mathf.Clamp01(elapsed / reloadDuration);
                yield return null;
            }
            _reloadAnimNormalized = 1f;

            if (reloadAudioSource != null && reloadCompleteClip != null)
            {
                reloadAudioSource.pitch = UnityEngine.Random.Range(0.96f, 1.07f);
                reloadAudioSource.PlayOneShot(reloadCompleteClip, 0.72f);
            }

            int needed = stats.clipSize - AmmoInClip;
            int moved = Mathf.Min(needed, ReserveAmmo);
            AmmoInClip += moved;
            ReserveAmmo -= moved;

            _isReloading = false;
            _reloadRoutine = null;
            _reloadAnimNormalized = 0f;
            RaiseAmmoChanged();
        }

        private void StopReload()
        {
            if (_reloadRoutine != null)
            {
                StopCoroutine(_reloadRoutine);
                _reloadRoutine = null;
            }

            _isReloading = false;
            _reloadAnimNormalized = 0f;
        }

        private void RaiseAmmoChanged()
        {
            AmmoChanged?.Invoke(AmmoInClip, ReserveAmmo);
        }

        private static Vector3 ApplySpread(Vector3 forward, float spreadAngle)
        {
            Vector2 spread = UnityEngine.Random.insideUnitCircle * spreadAngle;
            Quaternion rotation = Quaternion.Euler(-spread.y, spread.x, 0f);
            return rotation * forward;
        }

        private Vector3 ResolveTracerEndPoint(Vector3 origin, Vector3 direction, float maxDistance)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return origin + direction * maxDistance;
        }

        private void PlayShotTracer(Vector3 start, Vector3 end, bool weakHit)
        {
            Vector3 delta = end - start;
            if (delta.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            StartCoroutine(ShotTracerRoutine(start, end, weakHit));
        }

        private IEnumerator ShotTracerRoutine(Vector3 start, Vector3 end, bool weakHit)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                yield break;
            }

            Vector3 dir = direction / distance;
            GameObject tracerObject = new GameObject("RuntimeShotTracer");
            Destroy(tracerObject, 0.25f);
            LineRenderer lineRenderer = tracerObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 2;

            Material tracerMaterial = GetOrCreateRuntimeTracerMaterial();
            if (tracerMaterial != null)
            {
                lineRenderer.sharedMaterial = tracerMaterial;
            }

            float width = Mathf.Max(0.015f, tracerWidth * (weakHit ? 1.15f : 1f));
            lineRenderer.widthMultiplier = width;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width * 0.2f;

            Color baseColor = weakHit
                ? new Color(1f, 0.45f, 0.22f, 0.98f)
                : new Color(1f, 0.82f, 0.36f, 0.95f);
            lineRenderer.startColor = baseColor;
            lineRenderer.endColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

            float tailLength = Mathf.Clamp(distance * 0.24f, 0.45f, 3.4f);
            float travelSpeed = Mathf.Max(40f, tracerTravelSpeed);
            float travelDuration = Mathf.Clamp(distance / travelSpeed, 0.015f, 0.08f);

            float elapsed = 0f;
            while (elapsed < travelDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / travelDuration);
                Vector3 head = Vector3.Lerp(start, end, t);
                Vector3 tail = head - dir * tailLength;
                lineRenderer.SetPosition(0, tail);
                lineRenderer.SetPosition(1, head);
                yield return null;
            }

            float fadeDuration = Mathf.Max(0.015f, tracerFadeDuration);
            float fadeElapsed = 0f;
            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(fadeElapsed / fadeDuration);
                Color startColor = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                lineRenderer.startColor = startColor;
                lineRenderer.endColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
                yield return null;
            }

            Destroy(tracerObject);
        }

        private void EnsureDefaultAudioClips()
        {
            bool usePreferredPrimary = fireClip == null || fireClip.name == "explosion_01" || fireClip.name == "shoot_01";
            if (usePreferredPrimary)
            {
                AudioClip fallbackFire = Resources.Load<AudioClip>("Audio/retro_laser_02");
                if (fallbackFire == null)
                {
                    fallbackFire = Resources.Load<AudioClip>("Audio/shoot_02");
                }
                if (fallbackFire == null)
                {
                    fallbackFire = Resources.Load<AudioClip>("Audio/shoot_01");
                }

                if (fallbackFire != null)
                {
                    fireClip = fallbackFire;
                }
                else if (!_loggedMissingFireClip)
                {
                    Debug.LogWarning("Shotgun fire clip not found at Resources/Audio/shoot_01.");
                    _loggedMissingFireClip = true;
                }
            }

            if (fireVariantClips == null || fireVariantClips.Length == 0)
            {
                List<AudioClip> fallbackVariants = new List<AudioClip>(4);
                AddIfValidAndUnique(fallbackVariants, fireClip);
                AddIfValidAndUnique(fallbackVariants, Resources.Load<AudioClip>("Audio/retro_explosion"));
                AddIfValidAndUnique(fallbackVariants, Resources.Load<AudioClip>("Audio/shoot_02"));
                AddIfValidAndUnique(fallbackVariants, Resources.Load<AudioClip>("Audio/explosion_02"));
                fireVariantClips = fallbackVariants.ToArray();
            }

            AudioClip dedicatedReload = Resources.Load<AudioClip>("Audio/reload_handgun");
            if (dedicatedReload != null)
            {
                reloadClip = dedicatedReload;
                reloadCompleteClip = null;
            }

            if (reloadClip == null)
            {
                AudioClip fallbackReload = Resources.Load<AudioClip>("Audio/terminal_07");
                if (fallbackReload == null)
                {
                    fallbackReload = Resources.Load<AudioClip>("Audio/terminal_03");
                }

                if (fallbackReload != null)
                {
                    reloadClip = fallbackReload;
                }
            }

            if (reloadCompleteClip != null && IsLegacyUiLikeClip(reloadCompleteClip))
            {
                reloadCompleteClip = null;
            }
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = Mathf.Max(1f, audioSource.volume);
            audioSource.dopplerLevel = 0f;
        }

        private void EnsureMuzzleParticles()
        {
            if (muzzlePoint == null)
            {
                return;
            }

            if (muzzleParticles == null)
            {
                Transform existing = muzzlePoint.Find("RuntimeMuzzleFlash");
                if (existing != null)
                {
                    muzzleParticles = existing.GetComponent<ParticleSystem>();
                }
            }

            if (muzzleParticles == null)
            {
                GameObject muzzleFlash = new GameObject("RuntimeMuzzleFlash");
                muzzleFlash.transform.SetParent(muzzlePoint, false);
                muzzleFlash.transform.localPosition = Vector3.zero;
                muzzleFlash.transform.localRotation = Quaternion.identity;
                muzzleParticles = muzzleFlash.AddComponent<ParticleSystem>();
            }

            ConfigureMuzzleParticles(muzzleParticles);
        }

        private void ConfigureMuzzleParticles(ParticleSystem system)
        {
            if (system == null)
            {
                return;
            }

            // Unity throws when duration is changed on a currently playing system.
            if (system.isPlaying || system.IsAlive(true))
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Clear(true);
            }

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.08f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                Mathf.Max(0.02f, muzzleFlashLifetime * 0.72f),
                Mathf.Max(0.04f, muzzleFlashLifetime));
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                Mathf.Max(12f, muzzleFlashSpeed * 0.72f),
                Mathf.Max(18f, muzzleFlashSpeed));
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.082f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.95f, 0.74f, 1f),
                new Color(1f, 0.45f, 0.12f, 0.95f));
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            short burstCount = (short)Mathf.Clamp(muzzleFlashParticles, 4, 24);
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = Mathf.Clamp(muzzleFlashSpread, 1f, 16f);
            shape.radius = 0.012f;
            shape.length = 0.035f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.z = new ParticleSystem.MinMaxCurve(2.2f);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient flashGradient = new Gradient();
            flashGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.96f, 0.78f), 0f),
                    new GradientColorKey(new Color(1f, 0.34f, 0.1f), 0.58f),
                    new GradientColorKey(new Color(0.2f, 0.05f, 0.02f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.58f, 0.65f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(flashGradient);

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 1.12f),
                new Keyframe(0.35f, 1f),
                new Keyframe(1f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 1.02f;
            renderer.lengthScale = 3.6f;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Material muzzleFlashMaterial = GetOrCreateRuntimeMuzzleFlashMaterial();
            if (muzzleFlashMaterial != null)
            {
                renderer.sharedMaterial = muzzleFlashMaterial;
            }
        }

        private static Material GetOrCreateRuntimeMuzzleFlashMaterial()
        {
            if (_runtimeMuzzleFlashMaterial != null)
            {
                return _runtimeMuzzleFlashMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                if (!_loggedMissingMuzzleFlashShader)
                {
                    Debug.LogWarning("No compatible shader found for runtime muzzle flash material.");
                    _loggedMissingMuzzleFlashShader = true;
                }

                return null;
            }

            _runtimeMuzzleFlashMaterial = new Material(shader);
            Color baseColor = new Color(1f, 0.56f, 0.17f, 1f);

            if (_runtimeMuzzleFlashMaterial.HasProperty("_BaseColor"))
            {
                _runtimeMuzzleFlashMaterial.SetColor("_BaseColor", baseColor);
            }

            if (_runtimeMuzzleFlashMaterial.HasProperty("_Color"))
            {
                _runtimeMuzzleFlashMaterial.SetColor("_Color", baseColor);
            }

            if (_runtimeMuzzleFlashMaterial.HasProperty("_EmissionColor"))
            {
                _runtimeMuzzleFlashMaterial.EnableKeyword("_EMISSION");
                _runtimeMuzzleFlashMaterial.SetColor("_EmissionColor", new Color(1.5f, 0.72f, 0.25f, 1f));
            }

            if (_runtimeMuzzleFlashMaterial.HasProperty("_Surface"))
            {
                _runtimeMuzzleFlashMaterial.SetFloat("_Surface", 1f);
            }

            if (_runtimeMuzzleFlashMaterial.HasProperty("_Cull"))
            {
                _runtimeMuzzleFlashMaterial.SetFloat("_Cull", 0f);
            }

            return _runtimeMuzzleFlashMaterial;
        }

        private static Material GetOrCreateRuntimeTracerMaterial()
        {
            if (_runtimeTracerMaterial != null)
            {
                return _runtimeTracerMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                if (!_loggedMissingTracerShader)
                {
                    Debug.LogWarning("No compatible shader found for runtime shot tracer material.");
                    _loggedMissingTracerShader = true;
                }

                return null;
            }

            _runtimeTracerMaterial = new Material(shader);
            Color tracerColor = new Color(1f, 0.86f, 0.38f, 1f);

            if (_runtimeTracerMaterial.HasProperty("_BaseColor"))
            {
                _runtimeTracerMaterial.SetColor("_BaseColor", tracerColor);
            }

            if (_runtimeTracerMaterial.HasProperty("_Color"))
            {
                _runtimeTracerMaterial.SetColor("_Color", tracerColor);
            }

            if (_runtimeTracerMaterial.HasProperty("_EmissionColor"))
            {
                _runtimeTracerMaterial.EnableKeyword("_EMISSION");
                _runtimeTracerMaterial.SetColor("_EmissionColor", new Color(1.6f, 1f, 0.42f, 1f));
            }

            return _runtimeTracerMaterial;
        }

        private void EnsureReloadAudioSource()
        {
            if (reloadAudioSource == null)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] != audioSource)
                    {
                        reloadAudioSource = sources[i];
                        break;
                    }
                }

                if (reloadAudioSource == null)
                {
                    reloadAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            reloadAudioSource.playOnAwake = false;
            reloadAudioSource.spatialBlend = 0f;
            reloadAudioSource.volume = Mathf.Max(0.9f, reloadAudioSource.volume);
            reloadAudioSource.dopplerLevel = 0f;
        }

        private void PlayFireSound()
        {
            AudioClip primaryClip = fireClip;
            AudioClip accentClip = SelectFireVariantClip(primaryClip);

            if (primaryClip == null && accentClip == null)
            {
                return;
            }

            if (primaryClip == null)
            {
                primaryClip = accentClip;
            }

            if (audioSource != null)
            {
                audioSource.spatialBlend = 0f;
                audioSource.pitch = UnityEngine.Random.Range(0.9f, 1f);
                audioSource.PlayOneShot(primaryClip, 1.3f);

                if (accentClip != null && accentClip != primaryClip)
                {
                    audioSource.pitch = UnityEngine.Random.Range(0.8f, 0.92f);
                    audioSource.PlayOneShot(accentClip, 0.75f);
                }

                return;
            }

            if (aimCamera != null)
            {
                AudioSource.PlayClipAtPoint(primaryClip, aimCamera.transform.position, 1f);
            }
        }

        private AudioClip SelectFireVariantClip(AudioClip excludedClip)
        {
            if (fireVariantClips == null || fireVariantClips.Length == 0)
            {
                return null;
            }

            AudioClip fallbackClip = null;
            int startIndex = UnityEngine.Random.Range(0, fireVariantClips.Length);
            for (int i = 0; i < fireVariantClips.Length; i++)
            {
                AudioClip clip = fireVariantClips[(startIndex + i) % fireVariantClips.Length];
                if (clip != null)
                {
                    if (fallbackClip == null)
                    {
                        fallbackClip = clip;
                    }

                    if (clip != excludedClip)
                    {
                        return clip;
                    }
                }
            }

            return fallbackClip;
        }

        private static void AddIfValidAndUnique(List<AudioClip> clips, AudioClip clip)
        {
            if (clip != null && !clips.Contains(clip))
            {
                clips.Add(clip);
            }
        }

        private static bool IsLegacyUiLikeClip(AudioClip clip)
        {
            if (clip == null)
            {
                return false;
            }

            string clipName = clip.name.ToLowerInvariant();
            return clipName.Contains("beep") || clipName.Contains("terminal");
        }

        private void TriggerFireAnimation()
        {
            if (weaponVisualRoot == null)
            {
                return;
            }

            float kick = Mathf.Max(0.02f, shotKickDistance);
            _shotPositionOffset += new Vector3(0f, -kick * 0.2f, -kick);
            float clampDistance = kick * 1.6f;
            if (_shotPositionOffset.sqrMagnitude > clampDistance * clampDistance)
            {
                _shotPositionOffset = _shotPositionOffset.normalized * clampDistance;
            }

            float rotationKick = Mathf.Max(2f, shotKickRotation);
            _shotRotationOffset += new Vector3(
                -rotationKick,
                UnityEngine.Random.Range(-rotationKick * 0.35f, rotationKick * 0.35f),
                UnityEngine.Random.Range(-rotationKick * 0.45f, rotationKick * 0.45f));
        }

        private void EnsureWeaponVisualRoot()
        {
            if (weaponVisualRoot != null)
            {
                return;
            }

            if (muzzlePoint != null && muzzlePoint.parent != null)
            {
                weaponVisualRoot = muzzlePoint.parent;
                return;
            }

            if (aimCamera != null)
            {
                Transform fallback = aimCamera.transform.Find("WeaponRoot");
                if (fallback != null)
                {
                    weaponVisualRoot = fallback;
                }
            }
        }

        private void CacheWeaponBaseTransform()
        {
            if (weaponVisualRoot == null)
            {
                return;
            }

            _weaponBaseLocalPosition = weaponVisualRoot.localPosition;
            _weaponBaseLocalRotation = weaponVisualRoot.localRotation;
            _shotPositionOffset = Vector3.zero;
            _shotRotationOffset = Vector3.zero;
        }

        private void EnsureRuntimeWeaponModel()
        {
            if (aimCamera == null)
            {
                return;
            }

            EnsureWeaponVisualRoot();
            Transform weaponRoot = weaponVisualRoot;
            if (weaponRoot == null)
            {
                return;
            }

            if (weaponRoot.Find("RuntimeShotgunModel") != null)
            {
                Transform existingModel = weaponRoot.Find("RuntimeShotgunModel");
                if (existingModel != null)
                {
                    ApplyBlackWeaponMaterial(existingModel.gameObject);
                }

                CacheWeaponBaseTransform();
                return;
            }

            GameObject shotgunModel = Resources.Load<GameObject>("Models/Shotgun");
            if (shotgunModel == null)
            {
                if (!_loggedMissingWeaponModel)
                {
                    Debug.LogWarning("Shotgun model not found at Resources/Models/Shotgun.");
                    _loggedMissingWeaponModel = true;
                }
                return;
            }

            GameObject visual = Instantiate(shotgunModel, weaponRoot);
            visual.name = "RuntimeShotgunModel";
            visual.transform.localPosition = new Vector3(0f, -0.06f, 0.32f);
            visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            visual.transform.localScale = Vector3.one * 0.22f;

            foreach (Collider col in visual.GetComponentsInChildren<Collider>(true))
            {
                Destroy(col);
            }

            ApplyBlackWeaponMaterial(visual);

            foreach (Renderer renderer in weaponRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.transform.IsChildOf(visual.transform))
                {
                    renderer.enabled = false;
                }
            }

            if (muzzlePoint != null)
            {
                muzzlePoint.localPosition = new Vector3(0f, -0.04f, 0.82f);
            }

            CacheWeaponBaseTransform();
        }

        private void ApplyBlackWeaponMaterial(GameObject visual)
        {
            if (visual == null)
            {
                return;
            }

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            Material weaponMaterial = GetOrCreateRuntimeWeaponMaterial();
            if (weaponMaterial == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = weaponMaterial;
                    continue;
                }

                for (int m = 0; m < materials.Length; m++)
                {
                    materials[m] = weaponMaterial;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static Material GetOrCreateRuntimeWeaponMaterial()
        {
            if (_runtimeWeaponMaterial != null)
            {
                return _runtimeWeaponMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                if (!_loggedMissingWeaponShader)
                {
                    Debug.LogWarning("No compatible shader found for runtime shotgun material.");
                    _loggedMissingWeaponShader = true;
                }

                return null;
            }

            _runtimeWeaponMaterial = new Material(shader);
            Color baseColor = new Color(0.03f, 0.03f, 0.035f, 1f);

            if (_runtimeWeaponMaterial.HasProperty("_BaseColor"))
            {
                _runtimeWeaponMaterial.SetColor("_BaseColor", baseColor);
            }

            if (_runtimeWeaponMaterial.HasProperty("_Color"))
            {
                _runtimeWeaponMaterial.SetColor("_Color", baseColor);
            }

            if (_runtimeWeaponMaterial.HasProperty("_Metallic"))
            {
                _runtimeWeaponMaterial.SetFloat("_Metallic", 0.68f);
            }

            if (_runtimeWeaponMaterial.HasProperty("_Smoothness"))
            {
                _runtimeWeaponMaterial.SetFloat("_Smoothness", 0.72f);
            }

            if (_runtimeWeaponMaterial.HasProperty("_EmissionColor"))
            {
                _runtimeWeaponMaterial.EnableKeyword("_EMISSION");
                _runtimeWeaponMaterial.SetColor("_EmissionColor", new Color(0.015f, 0.015f, 0.02f, 1f));
            }

            return _runtimeWeaponMaterial;
        }
    }
}


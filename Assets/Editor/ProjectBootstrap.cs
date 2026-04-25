using System.Collections.Generic;
using RecoilArena.Arena;
using RecoilArena.Core;
using RecoilArena.Enemies;
using RecoilArena.Player;
using RecoilArena.UI;
using RecoilArena.Upgrades;
using RecoilArena.Waves;
using RecoilArena.Weapons;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecoilArena.EditorTools
{
    public static class ProjectBootstrap
    {
        private const string ShotgunStatsPath = "Assets/_Game/Data/ShotgunStats.asset";
        private const string MeleeStatsPath = "Assets/_Game/Data/EnemyStats_Melee.asset";
        private const string TankStatsPath = "Assets/_Game/Data/EnemyStats_Tank.asset";

        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player.prefab";
        private const string MeleePrefabPath = "Assets/_Game/Prefabs/Enemy_Melee.prefab";
        private const string TankPrefabPath = "Assets/_Game/Prefabs/Enemy_Tank.prefab";

        private const string ScenePath = "Assets/_Game/Scenes/Arena.unity";

        [MenuItem("RecoilArena/Build Prototype")]
        public static void BuildPrototype()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EnsureFolders();

            WeaponStats shotgunStats = CreateOrUpdateShotgunStats();
            EnemyStats meleeStats = CreateOrUpdateMeleeStats();
            EnemyStats tankStats = CreateOrUpdateTankStats();
            MaterialSet materials = CreateOrUpdateMaterials();
            Font uiFont = LoadUIFont();

            GameObject playerPrefab = CreateOrUpdatePlayerPrefab(shotgunStats, materials.weaponMaterial);
            GameObject meleePrefab = CreateOrUpdateEnemyPrefab(
                MeleePrefabPath,
                false,
                meleeStats,
                materials.meleeMaterial,
                materials.weakSpotMaterial,
                0.28f,
                "Assets/_Game/Art/ThirdParty/OpenGameArt_AnimatedMechPack/FBX/Mike.fbx",
                1.7f,
                new Vector3(0f, 180f, 0f));
            GameObject tankPrefab = CreateOrUpdateEnemyPrefab(
                TankPrefabPath,
                true,
                tankStats,
                materials.tankMaterial,
                materials.weakSpotMaterial,
                0.34f,
                "Assets/_Game/Art/ThirdParty/OpenGameArt_AnimatedMechPack/FBX/Stan.fbx",
                2.25f,
                new Vector3(0f, 180f, 0f));

            CreateOrUpdateArenaScene(playerPrefab, meleePrefab, tankPrefab, meleeStats, tankStats, uiFont, materials);
            ConfigureBuildSettings();
            AssetDatabase.ForceReserializeAssets(new[]
            {
                ScenePath,
                PlayerPrefabPath,
                MeleePrefabPath,
                TankPrefabPath,
                ShotgunStatsPath,
                MeleeStatsPath,
                TankStatsPath,
                "Assets/_Game/Materials/M_ArenaFloor.mat",
                "Assets/_Game/Materials/M_Obstacle.mat",
                "Assets/_Game/Materials/M_Lava.mat",
                "Assets/_Game/Materials/M_EnemyMelee.mat",
                "Assets/_Game/Materials/M_EnemyTank.mat",
                "Assets/_Game/Materials/M_WeakSpot.mat",
                "Assets/_Game/Materials/M_Weapon.mat"
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Recoil Arena bootstrap completed.");
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/_Game",
                "Assets/_Game/Core",
                "Assets/_Game/Player",
                "Assets/_Game/Weapons",
                "Assets/_Game/Enemies",
                "Assets/_Game/Waves",
                "Assets/_Game/Upgrades",
                "Assets/_Game/UI",
                "Assets/_Game/Arena",
                "Assets/_Game/Prefabs",
                "Assets/_Game/Scenes",
                "Assets/_Game/Data",
                "Assets/_Game/Materials",
                "Assets/_Game/Art"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
                    string name = System.IO.Path.GetFileName(folder);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        AssetDatabase.CreateFolder(parent, name);
                    }
                }
            }
        }

        private static WeaponStats CreateOrUpdateShotgunStats()
        {
            WeaponStats stats = AssetDatabase.LoadAssetAtPath<WeaponStats>(ShotgunStatsPath);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<WeaponStats>();
                AssetDatabase.CreateAsset(stats, ShotgunStatsPath);
            }

            stats.pelletDamage = 10.5f;
            stats.pelletCount = 10;
            stats.spreadAngle = 8.5f;
            stats.range = 32f;
            stats.weakSpotBonusMultiplier = 2.1f;
            stats.fireCooldown = 0.9f;
            stats.clipSize = 4;
            stats.reserveAmmo = 36;
            stats.reloadDuration = 2f;
            stats.recoilImpulse = 50f;
            stats.recoilVerticalLift = 0f;
            EditorUtility.SetDirty(stats);
            return stats;
        }

        private static EnemyStats CreateOrUpdateMeleeStats()
        {
            EnemyStats stats = AssetDatabase.LoadAssetAtPath<EnemyStats>(MeleeStatsPath);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStats>();
                AssetDatabase.CreateAsset(stats, MeleeStatsPath);
            }

            stats.enemyName = "Rusher";
            stats.maxHealth = 50f;
            stats.moveSpeed = 5.8f;
            stats.acceleration = 20f;
            stats.turnSpeed = 880f;
            stats.attackRange = 2.2f;
            stats.attackDamage = 12f;
            stats.attackCooldown = 1.05f;
            stats.scoreReward = 100;
            stats.weakSpotMultiplier = 1.65f;
            stats.weakSpotMinShiftInterval = 1f;
            stats.weakSpotMaxShiftInterval = 2f;
            stats.weakSpotMoveSpeed = 6f;
            stats.bodyScale = new Vector3(1f, 1.1f, 1f);
            stats.bodyColor = new Color(0.22f, 0.9f, 1f);
            stats.weakSpotColor = new Color(1f, 0.34f, 0.22f);
            stats.weakSpotOffsets = new[]
            {
                new Vector3(0f, 0.38f, 0.42f),
                new Vector3(0.34f, 0.22f, 0f),
                new Vector3(-0.34f, 0.22f, 0f),
                new Vector3(0f, -0.08f, -0.36f)
            };
            EditorUtility.SetDirty(stats);
            return stats;
        }

        private static EnemyStats CreateOrUpdateTankStats()
        {
            EnemyStats stats = AssetDatabase.LoadAssetAtPath<EnemyStats>(TankStatsPath);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStats>();
                AssetDatabase.CreateAsset(stats, TankStatsPath);
            }

            stats.enemyName = "Bulwark";
            stats.maxHealth = 165f;
            stats.moveSpeed = 3.2f;
            stats.acceleration = 10f;
            stats.turnSpeed = 420f;
            stats.attackRange = 2.7f;
            stats.attackDamage = 20f;
            stats.attackCooldown = 1.45f;
            stats.scoreReward = 250;
            stats.weakSpotMultiplier = 1.7f;
            stats.weakSpotMinShiftInterval = 1.3f;
            stats.weakSpotMaxShiftInterval = 2.6f;
            stats.weakSpotMoveSpeed = 4.2f;
            stats.bodyScale = new Vector3(1.6f, 1.6f, 1.6f);
            stats.bodyColor = new Color(0.76f, 0.43f, 1f);
            stats.weakSpotColor = new Color(1f, 0.21f, 0.18f);
            stats.weakSpotOffsets = new[]
            {
                new Vector3(0f, 0.18f, 0.34f),
                new Vector3(0.3f, 0.06f, 0f),
                new Vector3(-0.3f, 0.06f, 0f),
                new Vector3(0f, -0.18f, -0.32f)
            };
            EditorUtility.SetDirty(stats);
            return stats;
        }
        private static MaterialSet CreateOrUpdateMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material arenaMat = CreateMaterial("Assets/_Game/Materials/M_ArenaFloor.mat", shader, new Color(0.11f, 0.15f, 0.2f), new Color(0.06f, 0.1f, 0.18f));
            Material obstacleMat = CreateMaterial("Assets/_Game/Materials/M_Obstacle.mat", shader, new Color(0.2f, 0.28f, 0.38f), new Color(0.14f, 0.2f, 0.3f));
            Material lavaMat = CreateMaterial("Assets/_Game/Materials/M_Lava.mat", shader, new Color(0.68f, 0.2f, 0.08f), new Color(1.4f, 0.26f, 0.03f));
            Material meleeMat = CreateMaterial("Assets/_Game/Materials/M_EnemyMelee.mat", shader, new Color(0.25f, 0.86f, 1f), new Color(0.1f, 0.35f, 0.55f));
            Material tankMat = CreateMaterial("Assets/_Game/Materials/M_EnemyTank.mat", shader, new Color(0.76f, 0.43f, 1f), new Color(0.26f, 0.14f, 0.35f));
            Material weakMat = CreateMaterial("Assets/_Game/Materials/M_WeakSpot.mat", shader, new Color(1f, 0.3f, 0.18f), new Color(2f, 0.35f, 0.24f));
            Material weaponMat = CreateMaterial("Assets/_Game/Materials/M_Weapon.mat", shader, new Color(0.03f, 0.03f, 0.035f), new Color(0.015f, 0.015f, 0.02f));
            Texture2D sciFiTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/_Game/Art/ThirdParty/OpenGameArt_SciFiMetal/Texture_v5_1.png");

            if (sciFiTexture != null)
            {
                ApplyTexture(arenaMat, sciFiTexture, new Vector2(12f, 12f));
                ApplyTexture(obstacleMat, sciFiTexture, new Vector2(6f, 6f));
            }

            return new MaterialSet
            {
                arenaMaterial = arenaMat,
                obstacleMaterial = obstacleMat,
                lavaMaterial = lavaMat,
                meleeMaterial = meleeMat,
                tankMaterial = tankMat,
                weakSpotMaterial = weakMat,
                weaponMaterial = weaponMat
            };
        }

        private static Material CreateMaterial(string path, Shader shader, Color baseColor, Color emissionColor)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = baseColor;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyTexture(Material material, Texture2D texture, Vector2 tiling)
        {
            if (material == null || texture == null)
            {
                return;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }
            else if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", tiling);
            }

            EditorUtility.SetDirty(material);
        }

        private static Font LoadUIFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Orbitron-Regular.ttf");
            if (font == null)
            {
                font = AssetDatabase.GetBuiltinExtraResource<Font>("Arial.ttf");
            }

            return font;
        }

        private static GameObject CreateOrUpdatePlayerPrefab(WeaponStats weaponStats, Material weaponMaterial)
        {
            GameObject root = new GameObject("Player");
            root.layer = LayerMask.NameToLayer("Ignore Raycast");

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.42f;
            capsule.center = new Vector3(0f, 1f, 0f);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            root.AddComponent<PlayerHealth>();

            GameObject cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(root.transform, false);
            cameraPivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            GameObject cameraGO = new GameObject("PlayerCamera");
            cameraGO.transform.SetParent(cameraPivot.transform, false);
            Camera camera = cameraGO.AddComponent<Camera>();
            camera.fieldOfView = 80f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 250f;
            cameraGO.AddComponent<AudioListener>();

            GameObject weaponRoot = new GameObject("WeaponRoot");
            weaponRoot.transform.SetParent(cameraGO.transform, false);
            weaponRoot.transform.localPosition = new Vector3(0.37f, -0.26f, 0.52f);
            weaponRoot.transform.localRotation = Quaternion.Euler(6f, -10f, 0f);

            GameObject shotgunModelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Art/ThirdParty/OpenGameArt_LowPolyAnimatedGuns/FBX/Shotgun.fbx");
            GameObject shotgunVisual = InstantiateModelVisual(
                shotgunModelAsset,
                weaponRoot.transform,
                new Vector3(0f, -0.06f, 0.32f),
                new Vector3(0f, 180f, 0f),
                0.22f,
                true);

            if (shotgunVisual != null)
            {
                Renderer[] renderers = shotgunVisual.GetComponentsInChildren<Renderer>();
                if (weaponMaterial != null)
                {
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer.sharedMaterial == null)
                        {
                            renderer.sharedMaterial = weaponMaterial;
                        }
                    }
                }
            }
            else
            {
                GameObject gunBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gunBody.name = "GunBody";
                gunBody.transform.SetParent(weaponRoot.transform, false);
                gunBody.transform.localScale = new Vector3(0.24f, 0.16f, 0.7f);
                gunBody.transform.localPosition = new Vector3(0f, -0.04f, 0.25f);
                if (gunBody.TryGetComponent(out Collider bodyCollider))
                {
                    Object.DestroyImmediate(bodyCollider);
                }

                GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barrel.name = "Barrel";
                barrel.transform.SetParent(weaponRoot.transform, false);
                barrel.transform.localScale = new Vector3(0.16f, 0.12f, 0.45f);
                barrel.transform.localPosition = new Vector3(0f, -0.04f, 0.72f);
                if (barrel.TryGetComponent(out Collider barrelCollider))
                {
                    Object.DestroyImmediate(barrelCollider);
                }

                if (weaponMaterial != null)
                {
                    gunBody.GetComponent<Renderer>().sharedMaterial = weaponMaterial;
                    barrel.GetComponent<Renderer>().sharedMaterial = weaponMaterial;
                }
            }

            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(weaponRoot.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, -0.04f, 0.82f);

            PlayerController controller = root.AddComponent<PlayerController>();
            PlayerCameraEffects cameraEffects = root.AddComponent<PlayerCameraEffects>();

            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.95f;
            AudioSource reloadAudioSource = root.AddComponent<AudioSource>();
            reloadAudioSource.playOnAwake = false;
            reloadAudioSource.spatialBlend = 0f;
            reloadAudioSource.volume = 0.9f;
            AudioSource damageAudioSource = root.AddComponent<AudioSource>();
            damageAudioSource.playOnAwake = false;
            damageAudioSource.spatialBlend = 0f;
            damageAudioSource.volume = 0.72f;

            ShotgunWeapon shotgun = root.AddComponent<ShotgunWeapon>();
            PlayerHealth playerHealth = root.GetComponent<PlayerHealth>();
            AudioClip fireClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/_Game/Audio/ThirdParty/OpenGameArt_50_CC0_SciFi_SFX/shoot_01.ogg");
            AudioClip reloadClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/_Game/Audio/ThirdParty/OpenGameArt_HandgunReloadCC0/reload.wav");
            if (reloadClip == null)
            {
                reloadClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/_Game/Audio/ThirdParty/OpenGameArt_50_CC0_SciFi_SFX/terminal_07.ogg");
            }
            AudioClip reloadCompleteClip = null;
            AudioClip damageClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/_Game/Audio/ThirdParty/OpenGameArt_50_CC0_SciFi_SFX/misc_09.ogg");

            AssignObject(controller, "cameraPivot", cameraPivot.transform);
            AssignObject(controller, "playerCamera", camera);

            AssignObject(cameraEffects, "targetCamera", camera);

            AssignObject(shotgun, "stats", weaponStats);
            AssignObject(shotgun, "playerController", controller);
            AssignObject(shotgun, "cameraEffects", cameraEffects);
            AssignObject(shotgun, "aimCamera", camera);
            AssignObject(shotgun, "muzzlePoint", muzzle.transform);
            AssignObject(shotgun, "weaponVisualRoot", weaponRoot.transform);
            AssignObject(shotgun, "audioSource", audioSource);
            AssignObject(shotgun, "reloadAudioSource", reloadAudioSource);
            AssignObject(shotgun, "fireClip", fireClip);
            AssignObject(shotgun, "reloadClip", reloadClip);
            AssignObject(shotgun, "reloadCompleteClip", reloadCompleteClip);
            AssignObject(playerHealth, "damageAudioSource", damageAudioSource);
            AssignObject(playerHealth, "damageClip", damageClip);

            return SaveAndDestroyTemporaryPrefab(root, PlayerPrefabPath);
        }

        private static GameObject CreateOrUpdateEnemyPrefab(
            string path,
            bool isTank,
            EnemyStats stats,
            Material bodyMaterial,
            Material weakSpotMaterial,
            float weakSpotScale,
            string modelPath,
            float modelHeight,
            Vector3 modelEuler)
        {
            GameObject body = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));

            CapsuleCollider capsule = body.AddComponent<CapsuleCollider>();
            if (isTank)
            {
                capsule.radius = 0.62f;
                capsule.height = 2.35f;
                capsule.center = new Vector3(0f, 1.12f, 0f);
            }
            else
            {
                capsule.radius = 0.44f;
                capsule.height = 1.86f;
                capsule.center = new Vector3(0f, 0.92f, 0f);
            }

            Rigidbody rb = body.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            NavMeshAgent agent = body.AddComponent<NavMeshAgent>();
            agent.radius = isTank ? 0.82f : 0.48f;
            agent.height = isTank ? 2.2f : 2f;

            EnemyBase enemyBase = body.AddComponent<EnemyBase>();

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject visualModel = InstantiateModelVisual(
                modelAsset,
                body.transform,
                Vector3.zero,
                modelEuler,
                modelHeight,
                false);

            Renderer bodyRenderer = null;
            if (visualModel != null)
            {
                bodyRenderer = FindFirstRenderer(visualModel);
            }

            if (bodyRenderer == null)
            {
                GameObject fallbackBody = GameObject.CreatePrimitive(isTank ? PrimitiveType.Cube : PrimitiveType.Capsule);
                fallbackBody.name = "BodyFallback";
                fallbackBody.transform.SetParent(body.transform, false);
                fallbackBody.transform.localPosition = isTank ? new Vector3(0f, 1f, 0f) : new Vector3(0f, 0.9f, 0f);
                fallbackBody.transform.localScale = isTank ? new Vector3(1.2f, 1.6f, 1.2f) : new Vector3(1f, 1.6f, 1f);
                if (fallbackBody.TryGetComponent(out Collider fallbackCollider))
                {
                    Object.DestroyImmediate(fallbackCollider);
                }

                bodyRenderer = fallbackBody.GetComponent<Renderer>();
                if (bodyMaterial != null)
                {
                    bodyRenderer.sharedMaterial = bodyMaterial;
                }
            }

            GameObject weakSpot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            weakSpot.name = "WeakSpot";
            weakSpot.transform.SetParent(body.transform, false);
            weakSpot.transform.localScale = Vector3.one * weakSpotScale;
            weakSpot.transform.localPosition = new Vector3(0f, 0.2f, 0.3f);

            Renderer weakRenderer = weakSpot.GetComponent<Renderer>();
            if (weakSpotMaterial != null)
            {
                weakRenderer.sharedMaterial = weakSpotMaterial;
            }

            WeakSpot weakSpotComponent = weakSpot.AddComponent<WeakSpot>();

            AssignObject(enemyBase, "stats", stats);
            AssignObject(enemyBase, "bodyRenderer", bodyRenderer);
            AssignObject(enemyBase, "weakSpot", weakSpotComponent);
            AssignObject(weakSpotComponent, "visualRenderer", weakRenderer);

            return SaveAndDestroyTemporaryPrefab(body, path);
        }

        private static void CreateOrUpdateArenaScene(
            GameObject playerPrefab,
            GameObject meleePrefab,
            GameObject tankPrefab,
            EnemyStats meleeStats,
            EnemyStats tankStats,
            Font uiFont,
            MaterialSet materials)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.13f, 0.16f, 0.2f);

            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(0.68f, 0.74f, 0.94f);
            lightGO.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

            Transform arenaRoot = new GameObject("Arena").transform;
            BuildArenaGeometry(arenaRoot, materials, out Transform[] spawnPoints, out Transform playerSpawn);

            NavMeshSurface navMeshSurface = arenaRoot.gameObject.AddComponent<NavMeshSurface>();
            navMeshSurface.collectObjects = CollectObjects.All;
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navMeshSurface.BuildNavMesh();

            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.name = "Player";
            playerInstance.transform.position = playerSpawn.position;
            playerInstance.transform.rotation = playerSpawn.rotation;

            PlayerController playerController = playerInstance.GetComponent<PlayerController>();
            PlayerHealth playerHealth = playerInstance.GetComponent<PlayerHealth>();
            PlayerCameraEffects cameraEffects = playerInstance.GetComponent<PlayerCameraEffects>();
            ShotgunWeapon shotgun = playerInstance.GetComponent<ShotgunWeapon>();

            GameUIController uiController = BuildUI(uiFont, out _);

            GameObject systems = new GameObject("GameSystems");
            UpgradeSystem upgradeSystem = systems.AddComponent<UpgradeSystem>();
            WaveDirector waveDirector = systems.AddComponent<WaveDirector>();
            GameSession gameSession = systems.AddComponent<GameSession>();

            AssignObject(waveDirector, "meleePrefab", meleePrefab.GetComponent<EnemyBase>());
            AssignObject(waveDirector, "tankPrefab", tankPrefab.GetComponent<EnemyBase>());
            AssignObject(waveDirector, "meleeStats", meleeStats);
            AssignObject(waveDirector, "tankStats", tankStats);
            AssignTransformArray(waveDirector, "spawnPoints", spawnPoints);
            AssignObject(waveDirector, "playerTarget", playerController.transform);
            AssignObject(waveDirector, "playerHealth", playerHealth);

            AssignObject(gameSession, "playerController", playerController);
            AssignObject(gameSession, "playerHealth", playerHealth);
            AssignObject(gameSession, "cameraEffects", cameraEffects);
            AssignObject(gameSession, "shotgun", shotgun);
            AssignObject(gameSession, "waveDirector", waveDirector);
            AssignObject(gameSession, "upgradeSystem", upgradeSystem);
            AssignObject(gameSession, "gameUI", uiController);
            AssignObject(gameSession, "playerSpawnPoint", playerSpawn);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        private static void BuildArenaGeometry(Transform root, MaterialSet materials, out Transform[] spawnPoints, out Transform playerSpawn)
        {
            CreateCube("MainPlatform", root, new Vector3(0f, -0.5f, 0f), new Vector3(36f, 1f, 36f), materials.arenaMaterial);
            CreateCube("InnerRing", root, new Vector3(0f, -0.9f, 0f), new Vector3(24f, 0.35f, 24f), materials.obstacleMaterial);

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 8.8f, 0.5f, Mathf.Sin(angle) * 8.8f);
                Vector3 scale = i % 2 == 0 ? new Vector3(2f, 3.2f, 2f) : new Vector3(3.4f, 2f, 1.4f);
                CreateCube($"Obstacle_{i + 1}", root, pos, scale, materials.obstacleMaterial);
            }

            CreateCube("Wall_North_Left", root, new Vector3(-10f, 2f, 19f), new Vector3(16f, 4f, 2f), materials.obstacleMaterial);
            CreateCube("Wall_North_Right", root, new Vector3(10f, 2f, 19f), new Vector3(16f, 4f, 2f), materials.obstacleMaterial);
            CreateCube("Wall_South_Left", root, new Vector3(-10f, 2f, -19f), new Vector3(16f, 4f, 2f), materials.obstacleMaterial);
            CreateCube("Wall_South_Right", root, new Vector3(10f, 2f, -19f), new Vector3(16f, 4f, 2f), materials.obstacleMaterial);
            CreateCube("Wall_East_Left", root, new Vector3(19f, 2f, 10f), new Vector3(2f, 4f, 16f), materials.obstacleMaterial);
            CreateCube("Wall_East_Right", root, new Vector3(19f, 2f, -10f), new Vector3(2f, 4f, 16f), materials.obstacleMaterial);
            CreateCube("Wall_West_Left", root, new Vector3(-19f, 2f, 10f), new Vector3(2f, 4f, 16f), materials.obstacleMaterial);
            CreateCube("Wall_West_Right", root, new Vector3(-19f, 2f, -10f), new Vector3(2f, 4f, 16f), materials.obstacleMaterial);

            GameObject lavaVisual = CreateCube("LavaVisual", root, new Vector3(0f, -2.4f, 0f), new Vector3(80f, 1f, 80f), materials.lavaMaterial);
            Object.DestroyImmediate(lavaVisual.GetComponent<BoxCollider>());

            GameObject lavaTrigger = new GameObject("LavaHazard");
            lavaTrigger.transform.SetParent(root, false);
            lavaTrigger.transform.localPosition = new Vector3(0f, -2.2f, 0f);
            BoxCollider lavaCollider = lavaTrigger.AddComponent<BoxCollider>();
            lavaCollider.size = new Vector3(72f, 2.4f, 72f);
            lavaCollider.isTrigger = true;
            HazardVolume hazard = lavaTrigger.AddComponent<HazardVolume>();
            AssignFloat(hazard, "damagePerSecond", 42f);
            AssignFloat(hazard, "safeHeightY", -0.35f);

            GameObject killVolume = new GameObject("KillVolume");
            killVolume.transform.SetParent(root, false);
            killVolume.transform.localPosition = new Vector3(0f, -11f, 0f);
            BoxCollider killCollider = killVolume.AddComponent<BoxCollider>();
            killCollider.size = new Vector3(160f, 4f, 160f);
            killCollider.isTrigger = true;
            killVolume.AddComponent<KillVolume>();

            List<Transform> points = new List<Transform>();
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 14.8f, 0.3f, Mathf.Sin(angle) * 14.8f);
                Transform spawn = new GameObject($"Spawn_{i + 1}").transform;
                spawn.SetParent(root, false);
                spawn.localPosition = pos;
                spawn.localRotation = Quaternion.LookRotation(-pos.normalized, Vector3.up);
                points.Add(spawn);
            }

            playerSpawn = new GameObject("PlayerSpawn").transform;
            playerSpawn.SetParent(root, false);
            playerSpawn.localPosition = new Vector3(0f, 0.5f, 0f);
            playerSpawn.localRotation = Quaternion.identity;

            spawnPoints = points.ToArray();
        }

        private static GameUIController BuildUI(Font font, out Canvas canvas)
        {
            GameObject canvasGO = new GameObject("GameUI");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.65f;

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
            }

            GameUIController uiController = canvasGO.AddComponent<GameUIController>();

            GameObject hudPanel = CreatePanel("HUD", canvas.transform, new Color(0f, 0f, 0f, 0f));
            GameObject mainMenuPanel = CreatePanel("MainMenu", canvas.transform, new Color(0.03f, 0.06f, 0.1f, 0.88f));
            GameObject pausePanel = CreatePanel("PauseMenu", canvas.transform, new Color(0f, 0f, 0f, 0.68f));
            GameObject gameOverPanel = CreatePanel("GameOver", canvas.transform, new Color(0.08f, 0f, 0f, 0.75f));
            GameObject upgradePanel = CreatePanel("UpgradePanel", canvas.transform, new Color(0f, 0f, 0f, 0.72f));

            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            Text hp = CreateText("HP", hudPanel.transform, font, 34, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(380f, 60f), "HP 100/100");
            Text ammo = CreateText("Ammo", hudPanel.transform, font, 34, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 26f), new Vector2(380f, 60f), "AMMO 6/42");
            Text wave = CreateText("Wave", hudPanel.transform, font, 34, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(380f, 60f), "WAVE 0");
            Text score = CreateText("Score", hudPanel.transform, font, 34, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(420f, 60f), "SCORE 0");
            Text status = CreateText("Status", hudPanel.transform, font, 40, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(900f, 80f), string.Empty);
            status.color = new Color(1f, 0.82f, 0.32f, 1f);
            status.gameObject.SetActive(false);

            Text crosshair = CreateText("Crosshair", hudPanel.transform, font, 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f), "+");
            crosshair.color = new Color(0.92f, 0.98f, 1f, 0.95f);

            Image hitMarker = CreateImage("HitMarker", hudPanel.transform, uiSprite, new Color(1f, 1f, 1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));

            BuildMainMenu(mainMenuPanel.transform, font, uiSprite, out Button startButton, out Button quitButton);
            BuildPauseMenu(pausePanel.transform, font, uiSprite, out Button resumeButton, out Button restartFromPauseButton, out Button pauseMainMenuButton);
            BuildGameOverMenu(gameOverPanel.transform, font, uiSprite, out Text gameOverSummary, out Button gameOverRestartButton, out Button gameOverMainMenuButton);
            BuildUpgradeMenu(upgradePanel.transform, font, uiSprite, out Text upgradeTitle, out Button[] upgradeButtons, out Text[] upgradeTexts);

            AssignObject(uiController, "mainMenuPanel", mainMenuPanel);
            AssignObject(uiController, "hudPanel", hudPanel);
            AssignObject(uiController, "pausePanel", pausePanel);
            AssignObject(uiController, "gameOverPanel", gameOverPanel);
            AssignObject(uiController, "upgradePanel", upgradePanel);

            AssignObject(uiController, "startButton", startButton);
            AssignObject(uiController, "quitButton", quitButton);

            AssignObject(uiController, "resumeButton", resumeButton);
            AssignObject(uiController, "pauseRestartButton", restartFromPauseButton);
            AssignObject(uiController, "pauseMainMenuButton", pauseMainMenuButton);

            AssignObject(uiController, "gameOverSummaryText", gameOverSummary);
            AssignObject(uiController, "gameOverRestartButton", gameOverRestartButton);
            AssignObject(uiController, "gameOverMainMenuButton", gameOverMainMenuButton);

            AssignObject(uiController, "hpText", hp);
            AssignObject(uiController, "ammoText", ammo);
            AssignObject(uiController, "waveText", wave);
            AssignObject(uiController, "scoreText", score);
            AssignObject(uiController, "statusBannerText", status);
            AssignObject(uiController, "hitMarkerImage", hitMarker);

            AssignObject(uiController, "upgradeTitleText", upgradeTitle);
            AssignButtonArray(uiController, "upgradeButtons", upgradeButtons);
            AssignTextArray(uiController, "upgradeButtonTexts", upgradeTexts);

            return uiController;
        }

        private static void BuildMainMenu(Transform parent, Font font, Sprite sprite, out Button startButton, out Button quitButton)
        {
            CreateText("Title", parent, font, 82, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(1500f, 130f), "RECOIL BREACH");
            CreateText("Subtitle", parent, font, 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(1300f, 60f), "Use shotgun recoil to reposition and survive endless waves.");

            startButton = CreateButton("StartButton", parent, font, sprite, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(360f, 76f), "START RUN");
            quitButton = CreateButton("QuitButton", parent, font, sprite, new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.43f), Vector2.zero, new Vector2(360f, 76f), "QUIT");
        }

        private static void BuildPauseMenu(Transform parent, Font font, Sprite sprite, out Button resumeButton, out Button restartButton, out Button mainMenuButton)
        {
            CreateText("PauseTitle", parent, font, 68, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(1000f, 120f), "PAUSED");
            resumeButton = CreateButton("ResumeButton", parent, font, sprite, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(360f, 72f), "RESUME");
            restartButton = CreateButton("PauseRestartButton", parent, font, sprite, new Vector2(0.5f, 0.47f), new Vector2(0.5f, 0.47f), Vector2.zero, new Vector2(360f, 72f), "RESTART");
            mainMenuButton = CreateButton("PauseMainMenuButton", parent, font, sprite, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(360f, 72f), "MAIN MENU");
        }

        private static void BuildGameOverMenu(Transform parent, Font font, Sprite sprite, out Text summaryText, out Button restartButton, out Button mainMenuButton)
        {
            CreateText("GameOverTitle", parent, font, 78, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(1200f, 120f), "SYSTEM FAILURE");
            summaryText = CreateText("Summary", parent, font, 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.63f), new Vector2(0.5f, 0.63f), Vector2.zero, new Vector2(900f, 60f), "Wave 0 | Score 0");

            restartButton = CreateButton("GameOverRestartButton", parent, font, sprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 74f), "RESTART");
            mainMenuButton = CreateButton("GameOverMainMenuButton", parent, font, sprite, new Vector2(0.5f, 0.41f), new Vector2(0.5f, 0.41f), Vector2.zero, new Vector2(380f, 74f), "MAIN MENU");
        }

        private static void BuildUpgradeMenu(Transform parent, Font font, Sprite sprite, out Text title, out Button[] buttons, out Text[] buttonTexts)
        {
            title = CreateText("UpgradeTitle", parent, font, 46, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.79f), new Vector2(0.5f, 0.79f), Vector2.zero, new Vector2(1300f, 90f), "Wave Cleared - Choose Upgrade");

            buttons = new Button[3];
            buttonTexts = new Text[3];

            float startY = 0.62f;
            for (int i = 0; i < 3; i++)
            {
                Button button = CreateButton($"UpgradeButton_{i + 1}", parent, font, sprite,
                    new Vector2(0.5f, startY - i * 0.14f), new Vector2(0.5f, startY - i * 0.14f), Vector2.zero, new Vector2(980f, 112f),
                    "Upgrade");

                Text label = button.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleCenter;
                label.resizeTextForBestFit = false;
                label.fontSize = 30;

                buttons[i] = button;
                buttonTexts[i] = label;
            }
        }
        private static Button CreateButton(string name, Transform parent, Font font, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, string label)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);

            RectTransform rt = buttonGO.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.13f, 0.22f, 0.34f, 0.94f);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;

            Button button = buttonGO.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.26f, 0.4f, 1f);
            colors.highlightedColor = new Color(0.24f, 0.39f, 0.58f, 1f);
            colors.pressedColor = new Color(0.1f, 0.18f, 0.3f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            Text text = CreateText("Label", buttonGO.transform, font, 30, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, label);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image image = panel.AddComponent<Image>();
            image.color = color;

            return panel;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size,
            string content)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);

            RectTransform rt = textGO.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Text text = textGO.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;
            text.color = new Color(0.93f, 0.97f, 1f, 1f);

            return text;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            GameObject imageGO = new GameObject(name);
            imageGO.transform.SetParent(parent, false);

            RectTransform rt = imageGO.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image image = imageGO.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;

            return image;
        }

        private static GameObject InstantiateModelVisual(
            GameObject modelAsset,
            Transform parent,
            Vector3 localPosition,
            Vector3 localEuler,
            float targetHeight,
            bool weaponModel)
        {
            if (modelAsset == null)
            {
                return null;
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            visual.name = "VisualModel";
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = Quaternion.Euler(localEuler);
            visual.transform.localScale = Vector3.one;

            RemoveCollidersInChildren(visual);

            if (TryGetCombinedBounds(visual, out Bounds bounds))
            {
                float scaleMultiplier = targetHeight / Mathf.Max(bounds.size.y, 0.001f);
                visual.transform.localScale *= scaleMultiplier;

                if (TryGetCombinedBounds(visual, out Bounds scaledBounds))
                {
                    if (!weaponModel)
                    {
                        visual.transform.position += Vector3.up * (-scaledBounds.min.y);
                    }
                }
            }

            return visual;
        }

        private static bool TryGetCombinedBounds(GameObject target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
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

        private static void RemoveCollidersInChildren(GameObject target)
        {
            Collider[] colliders = target.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Renderer FindFirstRenderer(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            return target.GetComponentInChildren<Renderer>();
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            if (material != null && cube.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = material;
            }

            return cube;
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static GameObject SaveAndDestroyTemporaryPrefab(GameObject instance, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static void AssignObject(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignTransformArray(Object target, string propertyName, Transform[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignButtonArray(Object target, string propertyName, Button[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignTextArray(Object target, string propertyName, Text[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private class MaterialSet
        {
            public Material arenaMaterial;
            public Material obstacleMaterial;
            public Material lavaMaterial;
            public Material meleeMaterial;
            public Material tankMaterial;
            public Material weakSpotMaterial;
            public Material weaponMaterial;
        }
    }
}

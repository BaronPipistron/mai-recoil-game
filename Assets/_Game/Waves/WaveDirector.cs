using System;
using System.Collections;
using System.Collections.Generic;
using RecoilArena.Enemies;
using RecoilArena.Player;
using UnityEngine;

namespace RecoilArena.Waves
{
    public class WaveDirector : MonoBehaviour
    {
        [SerializeField] private EnemyBase meleePrefab;
        [SerializeField] private EnemyBase tankPrefab;
        [SerializeField] private EnemyStats meleeStats;
        [SerializeField] private EnemyStats tankStats;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float baseSpawnInterval = 0.65f;
        [SerializeField] private float minSpawnInterval = 0.24f;

        [Header("Wave Composition")]
        [SerializeField] private int baseMeleeCount = 2;
        [SerializeField] private float meleePerWave = 1.15f;
        [SerializeField] private int maxMeleePerWave = 12;
        [SerializeField] private int tankStartWave = 3;
        [SerializeField] private float tankPerWave = 0.45f;
        [SerializeField] private int maxTankPerWave = 4;

        [SerializeField] private Transform playerTarget;
        [SerializeField] private PlayerHealth playerHealth;

        public event Action<int> WaveStarted;
        public event Action<int> WaveCleared;
        public event Action<int> ScoreChanged;

        public int CurrentWave { get; private set; }
        public int Score { get; private set; }

        private readonly List<EnemyBase> _aliveEnemies = new List<EnemyBase>();
        private Coroutine _waveRoutine;
        private bool _runActive;
        private bool _spawningWave;
        private int _remainingToSpawn;

        public void Setup(Transform target, PlayerHealth health)
        {
            playerTarget = target;
            playerHealth = health;
        }

        public void StartRun()
        {
            StopRun();
            _runActive = true;
            CurrentWave = 0;
            Score = 0;
            ScoreChanged?.Invoke(Score);
            RequestNextWave(1f);
        }

        public void StopRun()
        {
            _runActive = false;
            _spawningWave = false;
            _remainingToSpawn = 0;

            if (_waveRoutine != null)
            {
                StopCoroutine(_waveRoutine);
                _waveRoutine = null;
            }

            for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (_aliveEnemies[i] != null)
                {
                    Destroy(_aliveEnemies[i].gameObject);
                }
            }

            _aliveEnemies.Clear();
        }

        public void RequestNextWave(float delay = 1.5f)
        {
            if (!_runActive)
            {
                return;
            }

            if (_waveRoutine != null)
            {
                StopCoroutine(_waveRoutine);
            }

            _waveRoutine = StartCoroutine(SpawnWaveRoutine(delay));
        }

        public void NotifyEnemyKilled(EnemyBase enemy, bool weakSpotKill)
        {
            _aliveEnemies.Remove(enemy);

            int reward = enemy != null && enemy.Stats != null ? enemy.Stats.scoreReward : 50;
            if (weakSpotKill)
            {
                reward += 35;
            }

            Score += reward;
            ScoreChanged?.Invoke(Score);

            if (_runActive && !_spawningWave && _remainingToSpawn <= 0 && _aliveEnemies.Count == 0)
            {
                WaveCleared?.Invoke(CurrentWave);
            }
        }

        private IEnumerator SpawnWaveRoutine(float delay)
        {
            _spawningWave = true;

            yield return new WaitForSeconds(delay);
            if (!_runActive)
            {
                yield break;
            }

            CurrentWave++;
            WaveStarted?.Invoke(CurrentWave);

            GetWaveComposition(CurrentWave, out int meleeCount, out int tankCount);
            _remainingToSpawn = meleeCount + tankCount;

            List<bool> spawnOrder = BuildSpawnOrder(meleeCount, tankCount);
            float spawnInterval = Mathf.Clamp(baseSpawnInterval - CurrentWave * 0.02f, minSpawnInterval, baseSpawnInterval);

            foreach (bool spawnTank in spawnOrder)
            {
                if (!_runActive || playerHealth == null || !playerHealth.IsAlive)
                {
                    _spawningWave = false;
                    yield break;
                }

                SpawnEnemy(spawnTank ? tankPrefab : meleePrefab, spawnTank ? tankStats : meleeStats);
                _remainingToSpawn--;

                if (spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            _spawningWave = false;

            if (_runActive && _remainingToSpawn <= 0 && _aliveEnemies.Count == 0)
            {
                WaveCleared?.Invoke(CurrentWave);
            }
        }

        private void SpawnEnemy(EnemyBase prefab, EnemyStats stats)
        {
            if (prefab == null || stats == null || playerTarget == null || playerHealth == null)
            {
                return;
            }

            Transform spawn = PickSpawnPoint();
            EnemyBase enemy = Instantiate(prefab, spawn.position, spawn.rotation);
            enemy.Setup(stats, playerTarget, playerHealth, this);
            _aliveEnemies.Add(enemy);
        }

        private Transform PickSpawnPoint()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            }

            return transform;
        }

        private void GetWaveComposition(int wave, out int meleeCount, out int tankCount)
        {
            int waveIndex = Mathf.Max(0, wave - 1);
            meleeCount = baseMeleeCount + Mathf.FloorToInt(waveIndex * meleePerWave);
            meleeCount = Mathf.Clamp(meleeCount, 1, maxMeleePerWave);

            if (wave < tankStartWave)
            {
                tankCount = 0;
                return;
            }

            int tankWaveIndex = wave - tankStartWave;
            tankCount = 1 + Mathf.FloorToInt(tankWaveIndex * tankPerWave);
            tankCount = Mathf.Clamp(tankCount, 0, maxTankPerWave);
        }

        private static List<bool> BuildSpawnOrder(int meleeCount, int tankCount)
        {
            List<bool> order = new List<bool>(meleeCount + tankCount);
            for (int i = 0; i < meleeCount; i++)
            {
                order.Add(false);
            }

            for (int i = 0; i < tankCount; i++)
            {
                int insert = UnityEngine.Random.Range(0, order.Count + 1);
                order.Insert(insert, true);
            }

            return order;
        }
    }
}

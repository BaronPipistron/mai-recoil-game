using System;
using System.Collections.Generic;
using RecoilArena.Player;
using RecoilArena.Weapons;
using UnityEngine;

namespace RecoilArena.Upgrades
{
    public readonly struct UpgradeChoice
    {
        public readonly UpgradeType Type;
        public readonly string Title;
        public readonly string Description;
        public readonly float Value;

        public UpgradeChoice(UpgradeType type, string title, string description, float value)
        {
            Type = type;
            Title = title;
            Description = description;
            Value = value;
        }
    }

    public class UpgradeSystem : MonoBehaviour
    {
        private static readonly UpgradeType[] UpgradePool =
        {
            UpgradeType.RecoilBoost,
            UpgradeType.RecoilDampen,
            UpgradeType.DashUnlockOrBoost,
            UpgradeType.MaxHealthBoost,
            UpgradeType.DamageBoost,
            UpgradeType.ReloadBoost
        };

        private readonly Dictionary<UpgradeType, int> _levels = new Dictionary<UpgradeType, int>();

        public void ResetRun()
        {
            _levels.Clear();
        }

        public UpgradeChoice[] BuildChoices(int count)
        {
            count = Mathf.Clamp(count, 1, UpgradePool.Length);

            List<UpgradeType> options = new List<UpgradeType>(UpgradePool);
            UpgradeChoice[] choices = new UpgradeChoice[count];

            for (int i = 0; i < count; i++)
            {
                int pick = UnityEngine.Random.Range(0, options.Count);
                UpgradeType type = options[pick];
                options.RemoveAt(pick);
                choices[i] = CreateChoice(type);
            }

            return choices;
        }

        public void ApplyChoice(UpgradeChoice choice, PlayerController player, PlayerHealth playerHealth, ShotgunWeapon shotgun)
        {
            if (!_levels.TryGetValue(choice.Type, out int currentLevel))
            {
                currentLevel = 0;
            }

            _levels[choice.Type] = currentLevel + 1;

            switch (choice.Type)
            {
                case UpgradeType.RecoilBoost:
                    player.AddRecoilMultiplier(choice.Value);
                    break;
                case UpgradeType.RecoilDampen:
                    player.AddRecoilMultiplier(-choice.Value);
                    break;
                case UpgradeType.DashUnlockOrBoost:
                    player.EnableOrBoostDash(choice.Value);
                    break;
                case UpgradeType.MaxHealthBoost:
                    playerHealth.AddMaxHealth(choice.Value);
                    break;
                case UpgradeType.DamageBoost:
                    shotgun.AddDamageMultiplier(choice.Value);
                    break;
                case UpgradeType.ReloadBoost:
                    shotgun.AddReloadMultiplier(-choice.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public int GetUpgradeLevel(UpgradeType type)
        {
            return _levels.TryGetValue(type, out int level) ? level : 0;
        }

        private UpgradeChoice CreateChoice(UpgradeType type)
        {
            int level = GetUpgradeLevel(type);

            switch (type)
            {
                case UpgradeType.RecoilBoost:
                    return new UpgradeChoice(type,
                        "Overcharged Shells",
                        "Increase recoil force for stronger blast-jumps and evasive bursts.",
                        0.18f + level * 0.02f);
                case UpgradeType.RecoilDampen:
                    return new UpgradeChoice(type,
                        "Stability Dampener",
                        "Reduce recoil force for steadier aim and tighter close-range control.",
                        0.15f + level * 0.02f);
                case UpgradeType.DashUnlockOrBoost:
                    return new UpgradeChoice(type,
                        level == 0 ? "Directional Dash" : "Dash Thrusters",
                        level == 0
                            ? "Unlock a directional air dash on Shift."
                            : "Boost dash distance and burst mobility.",
                        3.6f + level * 1.2f);
                case UpgradeType.MaxHealthBoost:
                    return new UpgradeChoice(type,
                        "Reinforced Suit",
                        "Increase max HP and heal for the same amount.",
                        18f + level * 2f);
                case UpgradeType.DamageBoost:
                    return new UpgradeChoice(type,
                        "Payload Compression",
                        "Increase shotgun pellet damage.",
                        0.12f + level * 0.02f);
                case UpgradeType.ReloadBoost:
                    return new UpgradeChoice(type,
                        "Magnetic Feed",
                        "Reduce reload time.",
                        0.12f + level * 0.015f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}

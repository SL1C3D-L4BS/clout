using System.Collections.Generic;
using UnityEngine;
using Clout.Core;

namespace Clout.Combat
{
    /// <summary>
    /// Per-character ammo inventory — tracks ammo reserves by type.
    /// Phase 2 singleplayer — FishNet SyncVars will be restored in Phase 4.
    /// </summary>
    public class AmmoCacheManager : MonoBehaviour
    {
        [Header("Starting Ammo")]
        public int startingPistol = 24;
        public int startingSMG = 60;
        public int startingRifle = 30;
        public int startingShotgun = 16;
        public int startingSniper = 10;
        public int startingExplosive = 4;

        [Header("Max Ammo")]
        public int maxPistol = 96;
        public int maxSMG = 240;
        public int maxRifle = 120;
        public int maxShotgun = 48;
        public int maxSniper = 30;
        public int maxExplosive = 8;

        private Dictionary<AmmoType, int> _ammoReserves = new Dictionary<AmmoType, int>();
        private Dictionary<AmmoType, int> _ammoCaps = new Dictionary<AmmoType, int>();

        // SyncVar<string> for Phase 4 multiplayer

        public System.Action<AmmoType, int> OnAmmoChanged;

        private void Awake()
        {
            InitializeAmmo();
        }

        private void InitializeAmmo()
        {
            _ammoReserves[AmmoType.Pistol] = startingPistol;
            _ammoReserves[AmmoType.SMG] = startingSMG;
            _ammoReserves[AmmoType.Rifle] = startingRifle;
            _ammoReserves[AmmoType.Shotgun] = startingShotgun;
            _ammoReserves[AmmoType.Sniper] = startingSniper;
            _ammoReserves[AmmoType.Explosive] = startingExplosive;
            _ammoReserves[AmmoType.Infinite] = 999;

            _ammoCaps[AmmoType.Pistol] = maxPistol;
            _ammoCaps[AmmoType.SMG] = maxSMG;
            _ammoCaps[AmmoType.Rifle] = maxRifle;
            _ammoCaps[AmmoType.Shotgun] = maxShotgun;
            _ammoCaps[AmmoType.Sniper] = maxSniper;
            _ammoCaps[AmmoType.Explosive] = maxExplosive;
            _ammoCaps[AmmoType.Infinite] = 999;
        }

        public int GetAmmo(AmmoType type)
        {
            if (type == AmmoType.Infinite) return 999;
            return _ammoReserves.TryGetValue(type, out int count) ? count : 0;
        }

        public int GetMaxAmmo(AmmoType type)
        {
            return _ammoCaps.TryGetValue(type, out int cap) ? cap : 0;
        }

        public int ConsumeAmmo(AmmoType type, int amount)
        {
            if (type == AmmoType.Infinite) return amount;
            if (!_ammoReserves.ContainsKey(type)) return 0;

            int available = _ammoReserves[type];
            int consumed = Mathf.Min(available, amount);
            _ammoReserves[type] -= consumed;

            OnAmmoChanged?.Invoke(type, _ammoReserves[type]);
            return consumed;
        }

        public int AddAmmo(AmmoType type, int amount)
        {
            if (type == AmmoType.Infinite) return 0;
            if (!_ammoReserves.ContainsKey(type))
                _ammoReserves[type] = 0;

            int max = GetMaxAmmo(type);
            int current = _ammoReserves[type];
            int space = max - current;
            int added = Mathf.Min(amount, space);

            _ammoReserves[type] += added;
            OnAmmoChanged?.Invoke(type, _ammoReserves[type]);
            return added;
        }

        public bool HasAmmo(AmmoType type)
        {
            if (type == AmmoType.Infinite) return true;
            return GetAmmo(type) > 0;
        }
    }
}

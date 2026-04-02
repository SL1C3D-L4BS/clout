using UnityEngine;
using Clout.Core;

namespace Clout.Combat
{
    /// <summary>
    /// Ammo type definition — each ammo type defines its own behavior,
    /// damage modifier, and visual feedback.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Ammo/Ammo Definition")]
    public class AmmoDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string ammoName;
        public AmmoType type;
        public Sprite icon;

        [Header("Damage")]
        public float damageMultiplier = 1f;
        public float armorPenetration = 0f;
        public float poiseDamageMultiplier = 1f;

        [Header("Ballistics")]
        public float velocityMultiplier = 1f;
        public float gravityScale = 0f;
        public int pelletsPerShot = 1;

        [Header("Visuals")]
        public GameObject impactFX;
        public GameObject tracerPrefab;
        public GameObject projectilePrefab;
        public Color tracerColor = Color.yellow;

        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip impactSound;
    }
}

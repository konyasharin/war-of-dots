using UnityEngine;

namespace DotWars.Map
{
    [CreateAssetMenu(fileName = "TerrainConfig", menuName = "DotWars/Terrain Data")]
    public class TerrainConfig : ScriptableObject
    {
        public TerrainType terrainType;

        [Header("Movement")]
        public bool isPassable = true;

        [Range(0f, 1f)]
        public float speedModifier = 1f;

        [Tooltip("Override speed for infantry (e.g. forest: 0.85 vs 0.7 for others). -1 = use speedModifier.")]
        public float infantrySpeedModifier = -1f;

        [Header("Combat")]
        [Range(0f, 1f)]
        public float damageModifier = 1f;

        [Tooltip("Override damage for tanks (e.g. forest: 0.7). -1 = use damageModifier.")]
        public float tankDamageModifier = -1f;

        public float GetSpeedModifier(bool isInfantry)
        {
            if (isInfantry && infantrySpeedModifier >= 0f)
                return infantrySpeedModifier;
            return speedModifier;
        }

        public float GetDamageModifier(bool isTank)
        {
            if (isTank && tankDamageModifier >= 0f)
                return tankDamageModifier;
            return damageModifier;
        }
    }
}

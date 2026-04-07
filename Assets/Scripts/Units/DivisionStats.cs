using UnityEngine;

namespace DotWars.Units
{
    [CreateAssetMenu(fileName = "DivisionStats", menuName = "DotWars/Division Stats")]
    public class DivisionStats : ScriptableObject
    {
        public DivisionType divisionType;

        [Header("Health")]
        public float maxHP = 100f;

        [Header("Combat")]
        public float damagePerSec = 10f;

        [Header("Movement")]
        public float baseSpeed = 1f;

        [Header("Economy")]
        public int cost = 100;
    }
}

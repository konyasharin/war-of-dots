using UnityEngine;
using DotWars.Core;

namespace DotWars.Economy
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        public float[] Gold { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var config = GameManager.Instance?.Config;
            int startGold = config != null ? config.startingGold : 300;
            Gold = new float[] { startGold, startGold };
        }


        public void AddGold(int playerIndex, float amount)
        {
            if (playerIndex < 0 || playerIndex >= Gold.Length) return;
            Gold[playerIndex] += amount;
            EventBus.OnGoldChanged?.Invoke(playerIndex, (int)Gold[playerIndex]);
        }

        public bool SpendGold(int playerIndex, int amount)
        {
            if (playerIndex < 0 || playerIndex >= Gold.Length) return false;
            if (Gold[playerIndex] < amount) return false;
            Gold[playerIndex] -= amount;
            EventBus.OnGoldChanged?.Invoke(playerIndex, (int)Gold[playerIndex]);
            return true;
        }
    }
}

using UnityEngine;
using DotWars.Economy;
using DotWars.Core;

namespace DotWars.UI
{
    public class EconomyHUD : MonoBehaviour
    {
        private GUIStyle _playerStyle;
        private GUIStyle _enemyStyle;
        private GUIStyle _labelStyle;
        private bool _stylesInit;

        private void InitStyles()
        {
            _playerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            _playerStyle.normal.textColor = new Color(0.3f, 0.6f, 1f);

            _enemyStyle = new GUIStyle(_playerStyle);
            _enemyStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleRight
            };
            _labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            _stylesInit = true;
        }

        private void OnGUI()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (EconomyManager.Instance == null) return;

            if (!_stylesInit) InitStyles();

            float x = Screen.width - 180;
            float y = Screen.height - 80;

            // Background
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(new Rect(x - 10, y - 5, 185, 75), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int playerGold = (int)EconomyManager.Instance.Gold[0];
            int enemyGold = (int)EconomyManager.Instance.Gold[1];

            GUI.Label(new Rect(x, y, 165, 25), "You", _labelStyle);
            GUI.Label(new Rect(x, y + 18, 165, 30), $"${playerGold}", _playerStyle);

            GUI.Label(new Rect(x, y + 42, 165, 20), $"~${enemyGold} enemy", _enemyStyle);
        }
    }
}

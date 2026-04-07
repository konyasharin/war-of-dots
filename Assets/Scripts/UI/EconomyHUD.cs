using UnityEngine;
using DotWars.Economy;
using DotWars.Core;

namespace DotWars.UI
{
    public class EconomyHUD : MonoBehaviour
    {
        private GUIStyle _playerGoldStyle;
        private GUIStyle _enemyGoldStyle;
        private GUIStyle _labelStyle;
        private bool _stylesInit;

        private void InitStyles()
        {
            _playerGoldStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            _playerGoldStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);

            _enemyGoldStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleRight
            };
            _enemyGoldStyle.normal.textColor = new Color(1f, 0.4f, 0.4f, 0.7f);

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleRight
            };
            _labelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            _stylesInit = true;
        }

        private void OnGUI()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (EconomyManager.Instance == null) return;
            if (!_stylesInit) InitStyles();

            float w = 300;
            float h = 110;
            float x = Screen.width - w - 15;
            float y = Screen.height - h - 15;

            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.85f, 0.2f, 0.4f);
            DrawBorder(new Rect(x, y, w, h), 2);
            GUI.color = Color.white;

            int playerGold = (int)EconomyManager.Instance.Gold[0];
            int enemyGold = (int)EconomyManager.Instance.Gold[1];

            GUI.Label(new Rect(x + 10, y + 8, w - 25, 22), "Your Gold", _labelStyle);
            GUI.Label(new Rect(x + 10, y + 28, w - 25, 40), $"$ {playerGold}", _playerGoldStyle);
            GUI.Label(new Rect(x + 10, y + 72, w - 25, 28), $"Enemy: ~$ {enemyGold}", _enemyGoldStyle);
        }

        private void DrawBorder(Rect r, float t)
        {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
        }
    }
}

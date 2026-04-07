using UnityEngine;
using DotWars.Core;

namespace DotWars.UI
{
    public class TimeControlHUD : MonoBehaviour
    {
        private GUIStyle _speedBtnStyle;
        private GUIStyle _speedBtnActiveStyle;
        private GUIStyle _labelStyle;
        private bool _stylesInit;

        private void InitStyles()
        {
            _speedBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                fixedHeight = 44,
                fixedWidth = 56
            };

            _speedBtnActiveStyle = new GUIStyle(_speedBtnStyle);
            _speedBtnActiveStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            _speedBtnActiveStyle.normal.background = Texture2D.whiteTexture;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            _labelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            _stylesInit = true;
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            // Keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var gm = GameManager.Instance;
                if (gm.TimeScaleIndex == 0)
                    gm.SetTimeScaleIndex(1);
                else
                    gm.SetTimeScaleIndex(0);
            }

            for (int i = 1; i <= 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                    GameManager.Instance.SetTimeScaleIndex(i);
            }
        }

        private void OnGUI()
        {
            if (GameManager.Instance == null || GameManager.Instance.State == GameState.Menu) return;
            if (!_stylesInit) InitStyles();

            var gm = GameManager.Instance;
            var labels = gm.GetSpeedLabels();
            int current = gm.TimeScaleIndex;

            float btnW = 56;
            float gap = 5;
            float totalW = labels.Length * btnW + (labels.Length - 1) * gap;
            float h = 60;
            float x = Screen.width - totalW - 15;
            float y = 15;

            // Background
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(x - 8, y - 5, totalW + 16, h + 40), Texture2D.whiteTexture);
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            DrawBorder(new Rect(x - 8, y - 5, totalW + 16, h + 40), 1);
            GUI.color = Color.white;

            GUI.Label(new Rect(x - 8, y - 2, totalW + 16, 18), "Game Speed", _labelStyle);

            float bx = x;
            float by = y + 18;

            for (int i = 0; i < labels.Length; i++)
            {
                bool isActive = (i == current);

                if (isActive)
                {
                    GUI.color = new Color(0.2f, 0.3f, 0.5f, 1f);
                    GUI.DrawTexture(new Rect(bx - 1, by - 1, btnW + 2, 38), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                var style = isActive ? _speedBtnActiveStyle : _speedBtnStyle;
                if (GUI.Button(new Rect(bx, by, btnW, 36), labels[i], style))
                {
                    gm.SetTimeScaleIndex(i);
                }

                bx += btnW + gap;
            }

            // Keyboard hint
            GUI.color = Color.white;
            string hint = current == 0 ? "PAUSED — Space to resume" : $"Speed: {labels[current]} — Space to pause";
            _labelStyle.fontSize = 16;
            GUI.Label(new Rect(x - 8, by + 48, totalW + 16, 22), hint, _labelStyle);
            _labelStyle.fontSize = 20;
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

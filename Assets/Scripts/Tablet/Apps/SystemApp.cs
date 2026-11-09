using UnityEngine;
using UnityEngine.Events;

namespace NoOneCameBack.Tablet
{
    public class SystemApp : TabletApp
    {
        private static readonly string[] Options = { "Continue", "Options", "Quit to Menu", "Quit Game" };

        [SerializeField] private MenuListView menu;
        // TODO(diseño): options screen content (volume, controls, brightness...).
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private UnityEvent onQuitToMenu = new UnityEvent();

        public override bool PausesGame => true;

        protected override void OnOpen()
        {
            SetOptionsVisible(false);
            menu.SetOptions(Options);
        }

        protected override void OnClose() => SetOptionsVisible(false);

        public override void Tick(float unscaledDeltaTime)
        {
            if (optionsPanel != null && optionsPanel.activeSelf) return;

            switch (menu.HandleInput(Input))
            {
                case 0:
                    Tablet.Close();
                    break;
                case 1:
                    SetOptionsVisible(true);
                    break;
                case 2:
                    Tablet.Close();
                    onQuitToMenu.Invoke();
                    break;
                case 3:
                    QuitGame();
                    break;
            }
        }

        public override bool TryCancel()
        {
            if (optionsPanel == null || !optionsPanel.activeSelf) return false;

            SetOptionsVisible(false);
            return true;
        }

        private void SetOptionsVisible(bool visible)
        {
            if (optionsPanel != null) optionsPanel.SetActive(visible);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

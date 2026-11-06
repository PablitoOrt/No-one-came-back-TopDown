using Unity.Cinemachine;
using UnityEngine;

namespace NoOneCameBack.Tablet
{
    // Activates a close-up camera (e.g. a CinemachineCamera aimed at the tablet screen) while the tablet is out.
    // Cinemachine gives priority ties to the most recently activated camera, so no priority juggling is needed.
    // Also blocks aiming until the blend back to the gameplay camera ends: the mouse ray moves with the camera
    // during the blend, which would otherwise spin the player.
    public class TabletCameraFocus : MonoBehaviour, IPlayerActionBlocker
    {
        [SerializeField] private TabletController tablet;
        [Tooltip("Camera object enabled while the tablet is opening/open. Set the CinemachineBrain to Ignore Time Scale so the blend also runs while the System app pauses the game.")]
        [SerializeField] private GameObject focusCamera;
        [Tooltip("Left empty, taken from the main camera.")]
        [SerializeField] private CinemachineBrain brain;

        private CinemachineVirtualCameraBase focusVirtualCamera;
        private bool releasing;

        public bool BlocksCombat => (tablet != null && tablet.State != TabletState.Closed) || IsReleasing();
        public bool BlocksInteraction => false;

        private void Awake()
        {
            if (focusCamera != null) focusVirtualCamera = focusCamera.GetComponent<CinemachineVirtualCameraBase>();
            if (brain == null && Camera.main != null) brain = Camera.main.GetComponent<CinemachineBrain>();
        }

        private void OnEnable()
        {
            if (tablet == null) return;

            tablet.StateChanged += OnStateChanged;
            OnStateChanged(tablet.State);
        }

        private void OnDisable()
        {
            if (tablet != null) tablet.StateChanged -= OnStateChanged;
            releasing = false;
        }

        private void OnStateChanged(TabletState state)
        {
            bool active = state == TabletState.Opening || state == TabletState.Open;
            if (focusCamera != null)
            {
                if (focusCamera.activeSelf && !active) releasing = true;
                focusCamera.SetActive(active);
            }
            if (active) releasing = false;
        }

        // The brain only notices the deactivated camera on its next update, so "still on the focus camera" counts as blending too.
        private bool IsReleasing()
        {
            if (!releasing) return false;
            if (brain == null || focusVirtualCamera == null)
            {
                releasing = false;
                return false;
            }

            bool stillOnFocus = ReferenceEquals(brain.ActiveVirtualCamera, focusVirtualCamera);
            if (!brain.IsBlending && !stillOnFocus) releasing = false;
            return releasing;
        }
    }
}

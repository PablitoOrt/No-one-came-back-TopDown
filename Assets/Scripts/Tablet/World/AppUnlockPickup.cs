using UnityEngine;

namespace NoOneCameBack.Tablet
{
    // E.g. blueprints that unlock the Map app.
    [RequireComponent(typeof(Collider))]
    public class AppUnlockPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string appId = "map";
        [SerializeField] private string prompt = "Recoger planos";
        [SerializeField] private bool destroyOnPickup = true;

        public string InteractPrompt => prompt;

        public bool CanInteract(GameObject interactor) => TabletController.FindFor(interactor) != null;

        public void Interact(GameObject interactor)
        {
            TabletController tablet = TabletController.FindFor(interactor);
            if (tablet == null) return;

            if (!tablet.UnlockApp(appId))
            {
                Debug.LogWarning($"[AppUnlockPickup] No tablet app with id '{appId}'.", this);
                return;
            }

            if (destroyOnPickup) Destroy(gameObject);
        }
    }
}

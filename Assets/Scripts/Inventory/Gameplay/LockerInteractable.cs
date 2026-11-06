using NoOneCameBack.Tablet;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    // Every locker in the game opens the same shared grid (owned by PlayerInventoryHolder).
    [RequireComponent(typeof(Collider))]
    public class LockerInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Abrir casillero";

        public string InteractPrompt => prompt;

        public bool CanInteract(GameObject interactor)
        {
            TabletController tablet = TabletController.FindFor(interactor);
            InventoryApp app = tablet != null ? tablet.GetApp<InventoryApp>() : null;
            return app != null && tablet.IsUsable(app);
        }

        public void Interact(GameObject interactor)
        {
            TabletController tablet = TabletController.FindFor(interactor);
            InventoryApp app = tablet != null ? tablet.GetApp<InventoryApp>() : null;
            if (app != null) app.OpenWithLocker();
        }
    }
}

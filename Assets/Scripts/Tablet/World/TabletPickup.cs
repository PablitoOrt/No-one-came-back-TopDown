using UnityEngine;

namespace NoOneCameBack.Tablet
{
    [RequireComponent(typeof(Collider))]
    public class TabletPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Pick up tablet";

        public string InteractPrompt => prompt;

        public bool CanInteract(GameObject interactor)
        {
            TabletController tablet = TabletController.FindFor(interactor);
            return tablet != null && !tablet.IsFound;
        }

        public void Interact(GameObject interactor)
        {
            TabletController tablet = TabletController.FindFor(interactor);
            if (tablet == null) return;

            tablet.MarkFound();
            Destroy(gameObject);
        }
    }
}

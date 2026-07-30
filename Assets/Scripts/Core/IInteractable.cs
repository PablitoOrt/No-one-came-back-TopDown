using UnityEngine;

// Implemented by anything the player can walk up to and interact with
// (weapon pickups, doors, ...), so PlayerInteractor never needs to know
// their concrete type. Mirrors the IDamageable pattern.
public interface IInteractable
{
    string InteractPrompt { get; }

    bool CanInteract(GameObject interactor);

    void Interact(GameObject interactor);
}

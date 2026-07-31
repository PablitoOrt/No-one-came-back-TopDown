using UnityEngine;

public interface IInteractable
{
    string InteractPrompt { get; }

    bool CanInteract(GameObject interactor);

    void Interact(GameObject interactor);
}

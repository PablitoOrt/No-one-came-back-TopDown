using System;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    // Placeholder until a real bleeding system exists. Toggle it from the component's context menu to test bandages.
    public class BleedingStub : MonoBehaviour, IBleeding
    {
        [SerializeField] private bool isBleeding;

        public event Action Changed;

        public bool IsBleeding => isBleeding;

        public void StartBleeding() => Set(true);

        public void StopBleeding() => Set(false);

        private void Set(bool value)
        {
            if (isBleeding == value) return;

            isBleeding = value;
            Changed?.Invoke();
        }

        [ContextMenu("Debug/Start Bleeding")]
        private void DebugStartBleeding() => StartBleeding();
    }
}

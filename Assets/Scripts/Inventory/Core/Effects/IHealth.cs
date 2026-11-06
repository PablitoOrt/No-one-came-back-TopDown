using System;

namespace NoOneCameBack.Inventory
{
    public interface IHealth
    {
        event Action Changed;

        float Current { get; }
        float Max { get; }

        void Heal(float amount);
    }
}

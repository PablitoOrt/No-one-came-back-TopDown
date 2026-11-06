using System;

namespace NoOneCameBack.Inventory
{
    public interface IPsychosis
    {
        event Action Changed;

        // 0 = lucid, 1 = fully psychotic.
        float Level01 { get; }

        void Reduce(float amount);
    }
}

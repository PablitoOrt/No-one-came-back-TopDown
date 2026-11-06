using System;

namespace NoOneCameBack.Inventory
{
    public interface IBleeding
    {
        event Action Changed;

        bool IsBleeding { get; }

        void StopBleeding();
    }
}

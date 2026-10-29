public interface IPlayerActionBlocker
{
    bool BlocksCombat { get; }

    bool BlocksInteraction { get; }
}

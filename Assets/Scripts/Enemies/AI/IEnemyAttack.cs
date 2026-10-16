public interface IEnemyAttack
{
    bool IsBusy { get; }

    bool IsInRange(Health target);
    bool CanStartAttack(Health target);
    void StartAttack(Health target);
    void Cancel();
}

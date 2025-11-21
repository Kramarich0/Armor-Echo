public interface IDamageable
{
    void TakeDamage(int amount, string source = null);
}

public interface ITeamProvider
{
    TeamEnum Team { get; }
}

public interface IObjectPool<T>
{
    T Get();
    void Release(T item);
}

public interface IPersistentAttribute<T>
{
    void SetPersistent(T value);
    void SetPersistentRef(ref T value);

    void UpdatePersistentValue(T value);

    T Attribute();
}

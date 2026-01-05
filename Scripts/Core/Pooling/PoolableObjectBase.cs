using UnityEngine;

public abstract class PoolableObjectBase : MonoBehaviour
{
    public ObjectPoolCategory Category { get; private set; }
    public ulong ID { get; private set; }
    public string Key { get; private set; }

    public bool IsActivated { get; private set; }

    public float SpawnedAt { get; private set; }
    public float LengthSinceSpawned => Time.time - SpawnedAt;

    public float ActivatedAt { get; private set; }
    public float LengthSinceActivated => Time.time - ActivatedAt;

    public float DeactivatedAt { get; private set; }
    public float LengthSinceDeactivated => Time.time - DeactivatedAt;

    public virtual void OnSpawned(ObjectPoolCategory category, string key)
    {
        Category = category;
        Key = key;
        SpawnedAt = Time.time;
    }

    public virtual void OnActivated(ulong id)
    {
        ID = id;
        ActivatedAt = Time.time;
        IsActivated = true;
    }

    public virtual void OnInactivated()
    {
        DeactivatedAt = Time.time;
        IsActivated = false;
    }

    public virtual void OnRemoved() { }

    public void Return()
    {
        bool success = PoolManager.Instance.Return(this);
        if (success == false)
        {
            OnInactivated();
            OnRemoved();
            Destroy(gameObject);
        }
    }
}

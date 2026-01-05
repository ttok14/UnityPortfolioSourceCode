using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesHandleWrap<T>
{
    public AsyncOperationHandle<T> Handle { get; private set; }

    public AddressablesHandleWrap(AsyncOperationHandle<T> handle)
    {
        Handle = handle;
    }
}

public class AddressablesHandleWrap
{
    public AsyncOperationHandle Handle { get; private set; }

    public AddressablesHandleWrap(AsyncOperationHandle handle)
    {
        Handle = handle;
    }
}

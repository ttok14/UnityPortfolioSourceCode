using UnityEngine;

public class PopupManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
}

using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
}

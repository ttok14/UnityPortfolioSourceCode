using UnityEngine;

public class SceneBase : MonoBehaviour
{
    private void Start()
    {
        GameManager.Instance.SetScene(this);
    }
}

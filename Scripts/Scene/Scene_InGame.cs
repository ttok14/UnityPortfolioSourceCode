public class Scene_InGame : SceneBase
{
    private void Awake()
    {
        TEMP_Logger.Deb("Scene_InGame 로드됨.");
    }

    public void TEMP_SwitchToNext()
    {
        GameSceneManager.Instance.LoadSceneSync(SCENES.Auth);
    }
}

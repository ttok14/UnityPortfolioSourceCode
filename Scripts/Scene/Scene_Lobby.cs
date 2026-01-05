public class Scene_Lobby : SceneBase
{
    private void Awake()
    {
        TEMP_Logger.Deb("Scene_Lobby 로드됨.");
    }

    public void TEMP_SwitchToNext()
    {
        GameSceneManager.Instance.LoadSceneSync(SCENES.InGame);
    }
}

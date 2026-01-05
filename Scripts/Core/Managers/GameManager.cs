using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : SingletonBase<GameManager>
{
    /// <summary> 게임 메타데이터 정보 </summary>
    [SerializeField]
    private GameMetaDataBase _metaData;
    public GameMetaDataBase MetaDataBase => _metaData;
    public GameMetaData MetaData => _metaData as GameMetaData;

    private TableMetadata _tableMetadata;
    public TableMetadata TableMetadata => _tableMetadata != null ? _tableMetadata.Copy() : null;

    private MapDataMetadata _mapMetaData;
    public MapDataMetadata MapMetaData => _mapMetaData != null ? _mapMetaData.Copy() : null;

    public SceneBase CurrentScene { get; private set; }

    // public AddressablesPatcher Patcher { get; private set; }

    public MainStatetFSM FSM { get; private set; }

    public override void Initialize()
    {
        Application.targetFrameRate = 60;

        base.Initialize();

        EventManager.Instance.Initialize();
        MainThreadDispatcher.Instance.Initialize();
        UpdateManager.Instance.Initialize();
        InputManager.Instance.Initialize();
        AddressablesManager.Instance.Initialize();
        CoroutineRunner.Instance.Initialize();
        PoolManager.Instance.Initialize();
        PatchManager.Instance.Initialize();
        AssetManager.Instance.Initialize();
        CameraManager.Instance.Initialize();
        GameDBManager.Instance.Initialize();
        AudioManager.Instance.Initialize();
        GameSceneManager.Instance.Initialize();
        UIManager.Instance.Initialize();
        BGMManager.Instance.Initialize();
        MapManager.Instance.Initialize();
        EntityManager.Instance.Initialize();
        PlayerInteractionManager.Instance.Initialize();
        PathFindingManager.Instance.Initialize();
        // SpawnManager.Instance.Initialize();
        InGameManager.Instance.Initialize();
        LightManager.Instance.Initialize();
        EntityPlacementManager.Instance.Initialize();
        WaveManager.Instance.Initialize();

        SetCollisionLayerMatrixManually();

        // Patcher = new AddressablesPatcher();

        // ScenePrepareService.Instance.Initialize();

        SetupEvents();

        // StartCoroutine(StartApp());
        StartGame();
    }

    private void Update()
    {

    }

    void SetupEvents()
    {
        EventManager.Instance.Register(GLOBAL_EVENT.NEW_SCENE_LOADED, OnSceneLoaded);
        EventManager.Instance.Register(GLOBAL_EVENT.TABLE_PATCH_COMPLETED, OnTablePatchCompleted);
        EventManager.Instance.Register(GLOBAL_EVENT.MAP_DATA_PATCH_COMPLETED, OnMapDataPatchCompleted);
    }

    public void SetScene(SceneBase newScene)
    {
        CurrentScene = newScene;
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();

        EventManager.Instance.Unregister(GLOBAL_EVENT.NEW_SCENE_LOADED, OnSceneLoaded);
        EventManager.Instance.Unregister(GLOBAL_EVENT.TABLE_PATCH_COMPLETED, OnTablePatchCompleted);
        EventManager.Instance.Unregister(GLOBAL_EVENT.MAP_DATA_PATCH_COMPLETED, OnMapDataPatchCompleted);
    }

    private void OnSceneLoaded(EventContext cxt)
    {
        CurrentScene = FindAnyObjectByType<SceneBase>();
        if (CurrentScene == null)
        {
            TEMP_Logger.Err($"Failed to find {nameof(SceneBase)} in the current scene : {(cxt.Arg as SceneLoadedEventArgs).newScene}");
        }
    }

    private void OnTablePatchCompleted(EventContext cxt)
    {
        _tableMetadata = (cxt.Arg as TablePatchCompleteEventArg).metadata;
        if (_tableMetadata == null)
        {
            TEMP_Logger.Err($"Table Metadata retrieved is invalid");
        }
    }

    private void OnMapDataPatchCompleted(EventContext cxt)
    {
        _mapMetaData = (cxt.Arg as MapDataPatchCompleteEventArg).metadata;
        if (_mapMetaData == null)
        {
            TEMP_Logger.Err($"MapData Metadata retrieved is not valid");
        }
    }

    void StartGame()
    {
        if (File.Exists(Constants.Paths.TableMetadataPath))
        {
            _tableMetadata = JsonConvert.DeserializeObject<TableMetadata>(File.ReadAllText(Constants.Paths.TableMetadataPath, System.Text.Encoding.UTF8));
            TEMP_Logger.Deb(@$"Cahed Table Metadata Read At : {Constants.Paths.TableMetadataPath} | Version : {_tableMetadata.Version} , TotalHash : {_tableMetadata.TotalHash} , TableCount: {_tableMetadata.Files.Count}");
        }

        if (File.Exists(Constants.Paths.MapDataMetadataPath))
        {
            _mapMetaData = JsonConvert.DeserializeObject<MapDataMetadata>(File.ReadAllText(Constants.Paths.MapDataMetadataPath, System.Text.Encoding.UTF8));
            TEMP_Logger.Deb(@$"Cahed Map Metadata Read At : {Constants.Paths.MapDataMetadataPath} | Version : {_mapMetaData.Version} , TotalHash : {_mapMetaData.TotalHash} , TableCount: {_mapMetaData.Files.Count}");
        }

        FSM = new MainStatetFSM(this);

        FSM.AddState(GameState.BootStrap, gameObject.AddComponent<BootstrapState>());
        FSM.AddState(GameState.TransitionState, gameObject.AddComponent<TransitionState>());
        FSM.AddState(GameState.Loading, gameObject.AddComponent<LoadingState>());
        FSM.AddState(GameState.Auth, gameObject.AddComponent<AuthState>());
        FSM.AddState(GameState.Lobby, gameObject.AddComponent<LobbyState>());
        FSM.AddState(GameState.InGame, gameObject.AddComponent<InGameState>());

        FSM.Enable(GameState.BootStrap);
    }

    void SetCollisionLayerMatrixManually()
    {
        for (int i = 0; i < 32; i++)
        {
            for (int j = 0; j < 32; j++)
            {
                Physics.IgnoreLayerCollision(i, j, true);
            }
        }

        Physics.IgnoreLayerCollision(
            LayerUtils.GetLayer((EntityTeamType.Player, GameDB.E_EntityType.Character)),
            LayerUtils.GetLayer((EntityTeamType.Player, GameDB.E_EntityType.Structure)),
            false);

        Physics.IgnoreLayerCollision(
            LayerUtils.GetLayer((EntityTeamType.Player, GameDB.E_EntityType.Character)),
            LayerUtils.GetLayer((EntityTeamType.Enemy, GameDB.E_EntityType.Structure)),
            false);

        Physics.IgnoreLayerCollision(
            LayerUtils.GetLayer_String((EntityTeamType.Player, "Projectile")),
            LayerUtils.GetLayer((EntityTeamType.Enemy, GameDB.E_EntityType.Character)),
            false);

        Physics.IgnoreLayerCollision(
            LayerUtils.GetLayer_String((EntityTeamType.Player, "Projectile")),
            LayerUtils.GetLayer((EntityTeamType.Enemy, GameDB.E_EntityType.Structure)),
            false);

        Physics.IgnoreLayerCollision(
            LayerUtils.GetLayer_String((EntityTeamType.Enemy, "Projectile")),
            LayerUtils.GetLayer((EntityTeamType.Player, GameDB.E_EntityType.Character)),
            false);

        Physics.IgnoreLayerCollision(
            LayerUtils.GetLayer_String((EntityTeamType.Enemy, "Projectile")),
            LayerUtils.GetLayer((EntityTeamType.Player, GameDB.E_EntityType.Structure)),
            false);

        Physics.IgnoreLayerCollision(
            LayerUtils.GetLayer((EntityTeamType.Player, GameDB.E_EntityType.Character)),
            LayerUtils.GetLayer((EntityTeamType.Neutral, GameDB.E_EntityType.Environment)),
            false);
    }
}

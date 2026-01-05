using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public class GameStateMetaData
{
    public GameState state;
    public SCENES scene;

    // Type 으로 변환도 가능해야 하기 때문에
    // Type 의 풀네임이 들어가 있어야만함 
    public string[] uiTypes;

    public Type[] UITypes
    {
        get
        {
            return uiTypes.Select(t => Type.GetType(t)).ToArray();
        }
    }

    public GameStateMetaData Copy()
    {
        return (GameStateMetaData)MemberwiseClone();
    }
}

[Serializable, CreateAssetMenu]
public class GameMetaData : GameMetaDataBase
{
    [SerializeField]
    private List<GameStateMetaData> _stateMetaData;
    public List<GameStateMetaData> StateMetaData => new List<GameStateMetaData>(_stateMetaData);
    public GameStateMetaData Find(GameState state)
    {
        var target = _stateMetaData.Find(t => t.state == state);
        if (target == null)
        {
            TEMP_Logger.Err($"target state does not exist : {state}");
            return null;
        }
        return target.Copy();
    }

    [SerializeField]
    private List<AssetLabelReference> _addressablesLabelsToDownload;
    public List<string> AddressablesLabelsToDownload => _addressablesLabelsToDownload.Select(t => t.labelString).ToList();

    // 참고로 Addressables 는 Remote Path 같은 경우 전용 파일에서 별도 설정해줘야함 
    [SerializeField]
    private string _baseRemoteUrl;
    public string BaseRemoteURL => _baseRemoteUrl;
}

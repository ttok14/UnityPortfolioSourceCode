using System.Collections;

public class MapDataLoadProcessor : ILoadProcessor
{
    int _totalMapDataCount;
    int _currentMapDataDoneCount;
    float _progress;

    LoadingProcessResult _result;

    public float Progress
    {
        get
        {
            return _result == LoadingProcessResult.Success ? 1 : _progress;
        }
    }
    public string CurrentStatus
    {
        get
        {
            return $"Loading MapData.. {_currentMapDataDoneCount}/{_totalMapDataCount}";
        }
    }

    public LoadingProcessResult Result
    {
        get
        {
            return _result;
        }
    }

    public IEnumerator Process()
    {
        var metaData = GameManager.Instance.MapMetaData;
        _totalMapDataCount = metaData.Files.Count;

        if (_totalMapDataCount > 0)
        {
            foreach (var fileName in metaData.Files)
            {
                var deserialized = MapDataHelper.LoadMapDataReadingFile(fileName.Name);
                MapManager.Instance.AddMapData(deserialized);
                _currentMapDataDoneCount++;

                _progress = (float)_currentMapDataDoneCount / _totalMapDataCount;

                yield return null;
            }
        }

        if (_currentMapDataDoneCount != _totalMapDataCount)
        {
            TEMP_Logger.Err($"MapData Done Count must match with totalCount | Current : {_currentMapDataDoneCount}, Total : {_totalMapDataCount}");
        }

        _result = LoadingProcessResult.Success;
    }
}

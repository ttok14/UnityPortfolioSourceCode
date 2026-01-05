using System;
using System.Collections;
using System.IO;
using System.Reflection;
using GameDB;
using UnityEngine;

public class GameDBLoadProcessor : ILoadProcessor
{
    int _totalTableCount;
    int _currentTableDoneCount;
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
            return $"Loading Table.. {_currentTableDoneCount}/{_totalTableCount}";
        }
    }

    public LoadingProcessResult Result
    {
        get
        {
            return _result;
        }
    }

    /// <summary>
    /// 데이터 로드하기
    /// </summary>
    /// <param name="bytes">테이블 데이터들의 MessagePack 바이너리 형태</param>
    /// <returns></returns>
    public IEnumerator Process()
    {
        if (GameDBManager.Instance == null)
        {
            yield break;
        }

        var metaData = GameManager.Instance.TableMetadata;
        _totalTableCount = metaData.Files.Count;

        if (_totalTableCount > 0)
        {
            // Deserialze - 데이터 조립
            foreach (var field in GameDBHelper.ContainerFieldsCache)
            {
                var deserialized = GameDBHelper.LoadTableBinaryReadingFile(field);
                field.SetValue(GameDBManager.Instance.Container, deserialized);
                _currentTableDoneCount++;

                _progress = (float)_currentTableDoneCount / _totalTableCount;

                yield return null;
            }
        }

        if (_currentTableDoneCount != _totalTableCount)
        {
            TEMP_Logger.Err($"TableDontCount must match with totalCount | Current : {_currentTableDoneCount}, Total : {_totalTableCount}");
        }

        _result = LoadingProcessResult.Success;
    }
}

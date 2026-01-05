using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class PathVisualizer
{
    PathLineRenderController _pathController;

    public void Show(Vector3[] path)
    {
        RefreshIndicators(path).Forget();
    }

    public void Hide()
    {
        if (_pathController)
        {
            _pathController.Return();
            _pathController = null;
        }
    }

    public void Release()
    {
        Hide();
    }

    async UniTaskVoid RefreshIndicators(Vector3[] path)
    {
        if (_pathController == null)
        {
            var res = await PoolManager.Instance.RequestSpawnAsync<PathLineRenderController>(
                ObjectPoolCategory.Default,
                "EnemyPathIndicator");

            if (res.opRes == PoolOpResult.Successs)
            {
                _pathController = res.instance;
            }
            else
            {
                TEMP_Logger.Err($"Failed to spawn pathController");
                return;
            }
        }

        _pathController.gameObject.SetActive(true);
        _pathController.ChangeSettings(path);
    }
}

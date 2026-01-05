using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class EntityPlacementGhost
{
    FXGridIndicator _gridIndicator;

    Material _enabledGhostMaterial;
    Material _disabledGhostMaterial;

    // bool _loadingModel;

    EntityModel _currentEntityModel;

    Vector2Int _lastTilePosition;
    int _lastEulerY;

    public bool CanPlace { get; private set; }
    public uint ModelID { get; private set; }
    public Vector3 ModelPosition => _currentEntityModel?.transform.position ?? default;
    public int ModelEulerY => _currentEntityModel ? (int)_currentEntityModel.transform.eulerAngles.y : 0;
    public float VolumeRadius => _currentEntityModel?.VolumeRadius ?? 1f;

    public async UniTask Initialize()
    {
        _gridIndicator = await FXSystem.PlayFXAsync<FXGridIndicator>("FXGridIndicator");

        _gridIndicator.gameObject.SetActive(false);

        var enabledMat = await AssetManager.Instance.LoadAsync<Material>("GhostEnabledMaterial");
        _enabledGhostMaterial = enabledMat;

        var disabledMat = await AssetManager.Instance.LoadAsync<Material>("GhostDisabledMaterial");
        _disabledGhostMaterial = disabledMat;
    }

    public void Release()
    {
        Hide();

        if (_gridIndicator)
        {
            _gridIndicator.Return();
            _gridIndicator = null;
        }
    }

    public void DoUpdate_NewPos(Vector3 newPos)
    {
        if (_currentEntityModel == null || _gridIndicator == null)
            return;

        DoUpdate_PosRot(newPos, (int)_currentEntityModel.transform.eulerAngles.y);
    }

    public void DoUpdate_Rotate(int eulerY)
    {
        if (_currentEntityModel == null || _gridIndicator == null)
            return;

        DoUpdate_PosRot(_currentEntityModel.transform.position, (int)_currentEntityModel.transform.eulerAngles.y + eulerY);
    }

    public void DoUpdate_PosRot(Vector3 newModelPosition, int newEulerY)
    {
        if (_currentEntityModel == null || _gridIndicator == null)
            return;

        // 타일 위치랑은 관계없이 월드위치는 미세 조정 허용
        _currentEntityModel.transform.position = newModelPosition;
        _currentEntityModel.transform.rotation = Quaternion.Euler(0, newEulerY, 0);

        var tilePos = MapUtils.WorldPosToTilePos(newModelPosition);
        if (_lastTilePosition == tilePos && _lastEulerY == newEulerY)
            return;

        _lastTilePosition = tilePos;
        _lastEulerY = newEulerY;

        UpdateState(newModelPosition, newEulerY);
    }

    void UpdateState(Vector3 pos, int newEulerY, bool force = false)
    {
        bool prevPlace = CanPlace;
        CanPlace = EntityHelper.CanPlace(ModelID, pos, newEulerY);

        if (force == false && prevPlace == CanPlace)
            return;

        if (CanPlace)
        {
            _currentEntityModel.ChangeAllMaterials(_enabledGhostMaterial);
            _gridIndicator.SetGridColor(new Color(0.2f, 1f, 0.15f));
        }
        else
        {
            _currentEntityModel.ChangeAllMaterials(_disabledGhostMaterial);
            _gridIndicator.SetGridColor(new Color(1f, 0.32f, 0.15f));
        }
    }

    public async UniTask ShowGhostCo(uint entityTid, Vector3 position, CancellationToken token)
    {
        if (ModelID != entityTid && _currentEntityModel)
        {
            _currentEntityModel.Return();
            _currentEntityModel = null;
        }

        var resKey = DBEntity.GetResourceKey(entityTid);
        if (string.IsNullOrEmpty(resKey))
        {
            TEMP_Logger.Err($"Given entity not valid | Tid : {entityTid}");
            return;
        }

        ModelID = entityTid;

        if (_currentEntityModel == null)
        {
            var result = await PoolManager.Instance.RequestSpawnAsync<EntityModel>(ObjectPoolCategory.Entity, resKey, position, null);

            token.ThrowIfCancellationRequested();

            _currentEntityModel = result.instance;

            UpdateState(result.instance.transform.position, 0, force: true);

            _gridIndicator.SetTarget(result.instance.transform);
            _gridIndicator.gameObject.SetActive(true);

            var skillData = DBSkill.GetByEntityID(ModelID);
            if (skillData != null)
            {
                Color color = skillData.SkillType == GameDB.E_SkillType.Attack ? Color.red : Color.green;
                color.a = 0.4f;
                _gridIndicator.ShowRange(color, skillData.Range);
            }
            else
            {
                _gridIndicator.HideRange();
            }
        }
    }

    public void Hide()
    {
        _lastTilePosition = Vector2Int.zero;
        _lastEulerY = -1;
        CanPlace = false;
        ModelID = 0;

        if (_currentEntityModel)
        {
            _currentEntityModel.Return();
            _currentEntityModel = null;

            _gridIndicator.gameObject.SetActive(false);
            _gridIndicator.SetTarget(null);
        }
    }
}

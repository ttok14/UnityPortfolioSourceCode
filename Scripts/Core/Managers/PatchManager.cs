using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PatchEvents;

public class PatchManager : SingletonBase<PatchManager>
{
    public PatchStateFlags StateFlags { get; private set; }

    private Dictionary<PatchUnitType, PatchUnitBase> _units = new Dictionary<PatchUnitType, PatchUnitBase>();

    public override void Initialize()
    {
        _units.Add(PatchUnitType.Addressables, new AddressablesPatchUnit());
        _units.Add(PatchUnitType.Table, new TablePatchUnit());
        _units.Add(PatchUnitType.MapData, new MapDataPatchUnit());

        foreach (var u in _units)
        {
            u.Value.Initialize();
        }
    }

    public IEnumerator Prepare(PatchUnitType type, OnPreparationCompleted onCompleted, OnFailed onFailed)
    {
        yield return _units[type].Prepare(onCompleted, onFailed);
    }

    public IEnumerator FetchMetadata(PatchUnitType type, OnMetadataFetchProgressed onProgressed, OnMetadataFetchCompleted onCompleted, OnFailed onFailed)
    {
        yield return _units[type].FetchMetadata(onProgressed, onCompleted, onFailed);
    }

    public IEnumerator DownloadContents(PatchUnitType type, OnContentsDownloadProgressed onProgress, OnContentsDownloadCompleted onCompleted, OnFailed onFailed)
    {
        yield return _units[type].DownloadContents(onProgress, onCompleted, onFailed);
    }

    //---------------------------//

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
    }
}

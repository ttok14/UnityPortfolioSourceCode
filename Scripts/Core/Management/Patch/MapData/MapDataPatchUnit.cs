using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using static PatchEvents;

public class MapDataPatchUnit : PatchUnitBase
{
    MapDataMetadata _metadata;

    private string[] _localMapDataPaths { get; set; }
    private List<string> _fileNamesToDownload { get; set; } = new List<string>();
    private long _totalDownloadSize { get; set; }

    public override PatchUnitType Type => PatchUnitType.MapData;

    public override void Initialize()
    {
        base.Initialize();

        Directory.CreateDirectory(Constants.Paths.MapDataBaseDirectory);
        Directory.CreateDirectory(Constants.Paths.MapDataJsonFileDirectory);
    }

    public override IEnumerator Prepare(OnPreparationCompleted onCompleted, OnFailed onFailed)
    {
#if USE_REMOTE
        yield return base.Prepare(onCompleted, onFailed);

        var metadataUrl = new Uri(new Uri(GameManager.Instance.MetaData.BaseRemoteURL), $"{Constants.Paths.MapDataBaseRelativeDirectory}/{Constants.Paths.MapDataMetadataFileName}");
        using (var req = UnityWebRequest.Get(metadataUrl))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                TEMP_Logger.Deb($"New MapDataMetadata Retrieved Successfully");

                _metadata = GameManager.Instance.MapMetaData;
                MapDataMetadata updatedMetadata = JsonConvert.DeserializeObject<MapDataMetadata>(req.downloadHandler.text);
                bool refresh = _metadata == null || _metadata.Version != updatedMetadata.Version;
                if (refresh)
                {
                    _metadata = updatedMetadata;

                    try
                    {
                        File.WriteAllText(Constants.Paths.MapDataMetadataPath, req.downloadHandler.text);
                        TEMP_Logger.Deb($"New MapDataMetadata Saved : {Constants.Paths.TableMetadataPath}");
                    }
                    catch (Exception exp)
                    {
                        TEMP_Logger.Err($"Failed to save New MapDataMetadata : {exp.Message}");
                        base.OnPrepareFailed(new Exception($"Failed to save New MapDataMetadata : {exp.Message}"));
                        yield break;
                    }
                }
            }
            else if (req.result == UnityWebRequest.Result.ConnectionError ||
                   req.result == UnityWebRequest.Result.ProtocolError ||
                   req.result == UnityWebRequest.Result.DataProcessingError)
            {
                TEMP_Logger.Err($"Download MapDataMetadata Failed : {req.result} | URL : {metadataUrl}");
                base.OnPrepareFailed(new Exception($"Download MapDataMetadata Failed : {req.result} | URL : {metadataUrl}"));
                yield break;
            }
        }
#else
        yield return base.Prepare(onCompleted, onFailed);

        var txtAsset = Resources.Load<TextAsset>(Constants.Paths.MapDataBaseRelativeDirectory + "/" + "map_metadata");
        if (txtAsset == null)
        {
            TEMP_Logger.Err($"[BuiltIn] Failed to load MapMetadata");
        }
        else
        {
            _metadata = JsonConvert.DeserializeObject<MapDataMetadata>(txtAsset.text);
        }
#endif

        base.OnPrepared();
    }

    public override IEnumerator FetchMetadata(OnMetadataFetchProgressed onProgressed, OnMetadataFetchCompleted onCompleted, OnFailed onFailed)
    {
#if USE_REMOTE
        yield return base.FetchMetadata(onProgressed, onCompleted, onFailed);

        _totalDownloadSize = 0;
        _fileNamesToDownload.Clear();

        _localMapDataPaths = Directory.GetFiles(Constants.Paths.MapDataJsonFileDirectory);
        var validExistings = new HashSet<string>();

        // 로컬에 이미 있는 테이블들 순회
        foreach (var path in _localMapDataPaths)
        {
            string hash = string.Empty;
            try
            {
                hash = Helper.GetHash(File.ReadAllBytes(path));
            }
            catch (Exception exp)
            {
                TEMP_Logger.Err($"Possible Error During Reading Bytes IO Operation MUST CHECK!! {exp}");
                base.OnMetadataFetchFailed(exp);
            }

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);

            TEMP_Logger.Deb($"Browsing Local Table : {path}");

            // 현재 로컬에 있는 파일이 최선 버전의 메타데이터에 존재하는지(Valid체크)
            if (_metadata.IsFileValid(fileNameWithoutExtension, hash))
                validExistings.Add(fileNameWithoutExtension);

            yield return null;
        }

        foreach (var file in _metadata.Files)
        {
            if (_fileNamesToDownload.Contains(file.Name) == false && validExistings.Contains(file.Name) == false)
            {
                _fileNamesToDownload.Add(file.Name);
                _totalDownloadSize += _metadata.GetFileSize(file.Name);
            }
        }

        /// 만약 다운로드 받을 게 없다면, 곧 바로 Invalid 한 로컬 파일들을 정리한다
        ///      => 왜냐면, <see cref="DownloadContents"/> 에서 처리하기에는 다운로드 받을 게 없는 상황에도
        ///          외부에서 <see cref="DownloadContents"/> 를 호출하게끔 강제하는 것은
        ///          암묵적이고 직관적이지 않은 동작임.
        ///      => 다운로드 할 것이 있다면 , <see cref="DownloadContents"/> 에서 하도록 처리
        ///          왜냐면 미리 삭제해놓고 중간에 다운로드가 실패하면 파일이 비어버릴 수 있는 상태 방지.
        if (_totalDownloadSize == 0)
            DiscardInvalidLocalFiles();

        base.OnMetadataFetched(_totalDownloadSize, _fileNamesToDownload.ToList());

#else
        yield return base.FetchMetadata(onProgressed, onCompleted, onFailed);
        base.OnMetadataFetched(0, new List<string>());
        EventManager.Instance.Publish(GLOBAL_EVENT.MAP_DATA_PATCH_COMPLETED, new MapDataPatchCompleteEventArg(_metadata));
#endif
    }

    public override IEnumerator DownloadContents(OnContentsDownloadProgressed onProgress, OnContentsDownloadCompleted onCompleted, OnFailed onFailed)
    {
#if USE_REMOTE
        yield return base.DownloadContents(onProgress, onCompleted, onFailed);

        if (_totalDownloadSize == 0)
        {
            OnDownloaded(new DownloadResultReport(0, 0));
            yield break;
        }

        float beginTime = Time.time;
        var mapDataUrl = new Uri(new Uri(GameManager.Instance.MetaData.BaseRemoteURL), $"{Constants.Paths.MapDataBaseRelativeDirectory}/{Constants.Paths.MapDataSubDirectoryNameFromMetadata}");
        ulong downloadedBytes = 0;
        foreach (var fileName in _fileNamesToDownload)
        {
            TEMP_Logger.Deb($"Downloading MapData : {fileName}");

            var url = $"{mapDataUrl}/{fileName}.json";
            using (var req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    downloadedBytes += req.downloadedBytes;
                    File.WriteAllBytes(Path.Combine(Constants.Paths.MapDataJsonFileDirectory, $"{fileName}.json"), req.downloadHandler.data);
                }
                else if (req.result == UnityWebRequest.Result.ConnectionError ||
                    req.result == UnityWebRequest.Result.DataProcessingError ||
                    req.result == UnityWebRequest.Result.ProtocolError)
                {
                    base.OnDownloadFailed(new Exception($"DownloadFailed | {req.result} | Url : {url}"));
                    yield break;
                }
            }

            base.OnDownloadProgressed(new DownloadProgressStatus((long)downloadedBytes, (float)((double)downloadedBytes / _totalDownloadSize)));

            TEMP_Logger.Deb($"Downloading Table End: {fileName}");
        }

        if (_totalDownloadSize != (long)downloadedBytes)
        {
            var names = string.Join(",", _fileNamesToDownload);
            TEMP_Logger.Err(@$"Downloaded Bytes does not match with Metadata based Size |
TotalDownloadSize(Metadata) : {_totalDownloadSize}
DownloadedBytes : {downloadedBytes}
DownloadTableFileNameList : {names}");

            base.OnDownloadFailed(new Exception($"Download Size does not match MUST CHECK"));
        }

        OnDownloaded(new DownloadResultReport((long)downloadedBytes, Time.time - beginTime));

#else
        yield return base.DownloadContents(onProgress, onCompleted, onFailed);
        OnDownloaded(new DownloadResultReport(0, 0));
#endif
    }

    protected override void OnDownloaded(DownloadResultReport report)
    {
        DiscardInvalidLocalFiles();
        base.OnDownloaded(report);
        EventManager.Instance.Publish(GLOBAL_EVENT.MAP_DATA_PATCH_COMPLETED, new MapDataPatchCompleteEventArg(_metadata));
    }

    void DiscardInvalidLocalFiles()
    {
        if (_localMapDataPaths != null)
        {
            TEMP_Logger.Deb($"Deleting Not Invalid Local Existing Files..");

            // 제거해야할 로컬 파일들 검출 및 제거 처리
            foreach (var path in _localMapDataPaths)
            {
                bool discard = _metadata.Files.Exists(t => t.Name == Path.GetFileNameWithoutExtension(path)) == false;
                if (discard)
                {
                    try
                    {
                        TEMP_Logger.Deb($"Deleting : {path}");
                        File.Delete(path);
                    }
                    catch (Exception exp)
                    {
                        TEMP_Logger.Err($"Possible Error During Delete Files IO Operation MUST CHECK!! {exp}");
                        base.OnDownloadFailed(exp);
                    }
                }
            }

            _localMapDataPaths = null;
        }
    }
}

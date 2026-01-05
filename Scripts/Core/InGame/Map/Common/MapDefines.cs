using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapFileInfo
{
    public string Name;
    public string Hash;
    public long ByteSize;
}

[Serializable]
public class MapDataMetadata
{
    public string Version;
    public string TotalHash;
    public List<MapFileInfo> Files;

    public MapDataMetadata()
    {
        Files = new List<MapFileInfo>();
        Version = DateTime.Now.ToString("yyMMdd_HHmmss");
        TotalHash = Helper.GetHash("");
    }

    public bool IsFileValid(string name, string hash)
    {
        return Files.Exists(t => t.Name == name && t.Hash == hash);
    }

    public long GetFileSize(string name)
    {
        return Files.Find(t => t.Name == name).ByteSize;
    }

    public bool IsSame(MapDataMetadata other)
    {
        return TotalHash == other.TotalHash;
    }

    public MapDataMetadata Copy()
    {
        return base.MemberwiseClone() as MapDataMetadata;
    }
}


public class MapPatchCompleteEventArg : EventArgBase
{
    public MapDataMetadata metadata;

    public MapPatchCompleteEventArg(MapDataMetadata metadata)
    {
        this.metadata = metadata;
    }
}


public class MapDataPatchCompleteEventArg : EventArgBase
{
    public MapDataMetadata metadata { get; private set; }
    public MapDataPatchCompleteEventArg(MapDataMetadata metadata)
    {
        this.metadata = metadata;
    }
}

using System.Collections.Generic;

public class TableFileInfo
{
    public string Name { get; set; }
    public string Hash { get; set; }
    public long ByteSize { get; set; }
}

public class TableMetadata
{
    public string Version { get; set; }
    public string TotalHash { get; set; }
    public List<TableFileInfo> Files { get; set; } = new List<TableFileInfo>();

    public bool IsFileValid(string name, string hash)
    {
        return Files.Exists(t => t.Name == name && t.Hash == hash);
    }

    public long GetFileSize(string name)
    {
        return Files.Find(t => t.Name == name).ByteSize;
    }

    public TableMetadata Copy()
    {
        return base.MemberwiseClone() as TableMetadata;
    }
}


public class TablePatchCompleteEventArg : EventArgBase
{
    public TableMetadata metadata;

    public TablePatchCompleteEventArg(TableMetadata metadata)
    {
        this.metadata = metadata;
    }
}

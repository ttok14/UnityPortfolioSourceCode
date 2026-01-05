
using System;

public class AddressablesDownloadFinishEvtArg : EventArgBase
{
    public long bytesDownloaded;
    public float secondsTaken;

    public AddressablesDownloadFinishEvtArg(long bytesDownloaded, float secondsTaken)
    {
        this.bytesDownloaded = bytesDownloaded;
        this.secondsTaken = secondsTaken;
    }
}

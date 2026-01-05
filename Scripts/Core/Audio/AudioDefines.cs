
public struct AudioSettings
{
    public readonly static AudioSettings Defualt = new AudioSettings(false);

    public bool loop;
    public bool enableAutoStop;

    public AudioSettings(bool loop)
    {
        this.loop = loop;
        enableAutoStop = true;
    }

    public AudioSettings(bool loop, bool enableAutoStop)
    {
        this.loop = loop;
        this.enableAutoStop = enableAutoStop;
    }
}

public class EventArgBase { }

public class EventContext
{
    public EventArgBase Arg;
    public bool IsUsed { get; private set; }
    public void Use()
    {
        IsUsed = true;
    }

    public EventContext(EventArgBase arg)
    {
        Arg = arg;
        IsUsed = false;
    }
}

public class SceneLoadEnterEventArgs : EventArgBase
{
    public SCENES prevScene;
    public SCENES newScene;
    public bool isAsync;

    public SceneLoadEnterEventArgs(SCENES prevScene, SCENES newScene)
    {
        this.prevScene = prevScene;
        this.newScene = newScene;
    }
}

public class SceneLoadedEventArgs : EventArgBase
{
    public SCENES prevScene;
    public SCENES newScene;

    public SceneLoadedEventArgs(SCENES prevScene, SCENES newScene)
    {
        this.prevScene = prevScene;
        this.newScene = newScene;
    }
}

public class SceneUnloadEnterEventArgs : EventArgBase
{
    public SCENES scene;

    public SceneUnloadEnterEventArgs(SCENES scene)
    {
        this.scene = scene;
    }

    public override string ToString()
    {
        return $"Unload Scene : {scene}";
    }
}

public class SceneUnloadedEventArgs : EventArgBase
{
    public SCENES scene;

    public SceneUnloadedEventArgs(SCENES scene)
    {
        this.scene = scene;
    }
}


public class GameStateChangeEventArg : EventArgBase
{
    public GameStateChangeEventArg(GameState from, GameState to)
    {
        From = from;
        To = to;
    }

    public GameState From { get; private set; }
    public GameState To { get; private set; }
}

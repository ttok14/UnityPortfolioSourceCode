
public enum PlayerInteractionType
{
    None = 0,

    WithEntity,
}

public class PlayerInteractionManager : SingletonBase<PlayerInteractionManager>
{
    public PlayerInteractionType CurrentInteraction { get; private set; }

    public void BeginInteraction(PlayerInteractionType type)
    {
        if (CurrentInteraction != PlayerInteractionType.None)
        {
            TEMP_Logger.Err($"Player is already interacting");
            return;
        }

        CurrentInteraction = type;
    }

    public void EndInteraction()
    {
        if (CurrentInteraction != PlayerInteractionType.None)
        {
            CurrentInteraction = PlayerInteractionType.None;
            EventManager.Instance.Publish(GLOBAL_EVENT.PLAYER_INTERACTION_ENDED);
        }
    }
}

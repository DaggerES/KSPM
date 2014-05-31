
public class GameError : KSPM.Network.Common.Error
{
    public enum GameErrorType : byte
    {
        Ok = 0,

        MessageNullSourceMessage,
        MessageInvalidSourceMessage,
    };
}

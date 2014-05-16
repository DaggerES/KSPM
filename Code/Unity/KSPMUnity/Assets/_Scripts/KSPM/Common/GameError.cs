
public class GameError : KSPM.Network.Common.Error
{
    public enum ErrorType : byte
    {
        Ok = 0,

        MessageNullSourceMessage,
        MessageInvalidSourceMessage,
    };
}

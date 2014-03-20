using KSPM.Network.Chat.Group;
using KSPM.Network.Chat.Messages;

namespace KSPM.Network.Chat.Filter
{
    public abstract class ChatFilter
    {
        public abstract bool Query(ChatMessage message);
    }
}

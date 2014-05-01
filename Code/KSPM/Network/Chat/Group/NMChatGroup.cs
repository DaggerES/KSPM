namespace KSPM.Network.Chat.Group
{
    public class NMChatGroup : PersistentChatGroup
    {
        public NMChatGroup()
            : base()
        {
        }

        public NMChatGroup(short id) : base()
        {
            ChatGroup.ChatGroupCounter = id;
            this.id = ChatGroup.ChatGroupCounter++;
            this.Name = string.Format("Chatgroup-{0}", this.id);
        }
    }
}

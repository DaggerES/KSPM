namespace KSPM.Network.Chat.Group
{
    public class NonPersistentNMChatgroup : NonPersistenChatGroup
    {
        public NonPersistentNMChatgroup()
            : base()
        {
        }

        public NonPersistentNMChatgroup( short id) : base()
        {
            ChatGroup.ChatGroupCounter = id;
            this.id = ChatGroup.ChatGroupCounter++;
            this.Name = string.Format("Chatgroup-{0}", this.id);
        }
    }
}

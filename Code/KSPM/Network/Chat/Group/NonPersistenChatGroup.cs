namespace KSPM.Network.Chat.Group
{
    /// <summary>
    /// Non-persistent group, which means that no message is stored by the sistem.
    /// </summary>
    public class NonPersistenChatGroup : ChatGroup
    {
        /// <summary>
        /// Will count how many messages has been asigned to this group.
        /// </summary>
        protected uint messageCounter;

        /// <summary>
        /// Creates an empty group.
        /// </summary>
        public NonPersistenChatGroup()
            : base()
        {
            this.messageCounter = 0;
        }

        /// <summary>
        /// Does not perform any adding process, only increments the message counter.
        /// </summary>
        /// <param name="newMessage"></param>
        public override void AddMessage(Messages.ChatMessage newMessage)
        {
            newMessage.Release();
            this.messageCounter++;
        }

        /// <summary>
        /// Resets the message counter to zero.
        /// </summary>
        public override void Purge()
        {
            this.messageCounter = 0;
        }

        /// <summary>
        /// Releases all messages holded by the group, calling its Release method on each one.
        /// </summary>
        public override void Release()
        {
            this.id = -1;
            this.members.Clear();
            this.performanceDataStructureMembers.Clear();
            this.messageCounter = 0;
            this.Name = null;
        }
    }
}

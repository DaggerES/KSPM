using KSPM.Network.Chat.Messages;

namespace KSPM.Network.Chat.Group
{
    /// <summary>
    /// Persistent chat group, designed to hold those messages sent to their members.
    /// </summary>
    public abstract class PersistentChatGroup : ChatGroup
    {
        /// <summary>
        /// List of the messages that belongs to this group.
        /// Kept as a record.
        /// </summary>
        protected System.Collections.Generic.List<ChatMessage> messages;

        /// <summary>
        /// Static property to set the amount of messages that should be kept by the list before purge them.
        /// Default value is set to 16.
        /// </summary>
        public static uint MaximunMessageListCount = 16;

        /// <summary>
        /// Creates a PersistenChatGroup reference ready to use.
        /// </summary>
        public PersistentChatGroup() : base()
        {
            this.messages = new System.Collections.Generic.List<ChatMessage>();
        }

        /// <summary>
        /// Adds a message to the message list.
        /// </summary>
        /// <param name="newMessage">Message to be added to the list.</param>
        public override void AddMessage(ChatMessage newMessage)
        {
            lock (this.messages)
            {
                this.messages.Add(newMessage);
            }
            if (this.messages.Count > PersistentChatGroup.MaximunMessageListCount)
            {
                this.Purge();
            }
        }

        /// <summary>
        /// Purges the messages list, Releasing each message.
        /// </summary>
        public override void Purge()
        {
            lock (this.messages)
            {
                for (int i = 0; i < this.messages.Count; i++)
                {
                    this.messages[i].Release();
                    this.messages[i] = null;
                }
                this.messages.Clear();
            }
        }

        /// <summary>
        /// Releases all messages holded by the group, calling its Release method on each one.<b>THIS IS NOT THREAD SAFE.</b>
        /// </summary>
        public override void Release()
        {
            this.id = -1;
            this.members.Clear();
            this.performanceDataStructureMembers.Clear();
            KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(this.messages.Count.ToString());
            for (int i = 0; i < this.messages.Count; i++)
            {
                this.messages[i].Release();
                this.messages[i] = null;
            }
            this.messages.Clear();
            this.messages = null;
            this.Name = null;
        }
    }
}

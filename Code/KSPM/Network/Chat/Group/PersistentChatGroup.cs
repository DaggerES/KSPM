using KSPM.Network.Chat.Messages;

namespace KSPM.Network.Chat.Group
{
    public abstract class PersistentChatGroup : ChatGroup
    {
        /// <summary>
        /// List of the messages that belongs to this group.
        /// </summary>
        protected System.Collections.Generic.List<ChatMessage> messages;

        public PersistentChatGroup() : base()
        {
            this.messages = new System.Collections.Generic.List<ChatMessage>();
        }

        public override void AddMessage(ChatMessage newMessage)
        {
            this.messages.Add(newMessage);
            if (this.messages.Count > 1000)
            {
                this.Purge();
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Purge");
            }
        }

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
        /// Releases all messages holded by the group, calling its Release method on each one.
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

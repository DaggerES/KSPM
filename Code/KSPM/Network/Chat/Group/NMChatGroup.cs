namespace KSPM.Network.Chat.Group
{
    public class NMChatGroup : ChatGroup
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

        /// <summary>
        /// Releases all messages holded by the group, calling its Release method on each one.
        /// </summary>
        public override void Release()
        {
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

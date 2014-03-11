namespace KSPM.Network.Chat.Group
{
    public class NMChatGroup : ChatGroup
    {
        public override void Release()
        {
            this.members.Clear();
            this.performanceDataStructureMembers.Clear();
            for (int i = 0; i < this.messages.Count; i++)
            {
                this.messages[i].Release();
                this.messages[i] = null;
            }
            this.messages.Clear();
            this.messages = null;
        }
    }
}

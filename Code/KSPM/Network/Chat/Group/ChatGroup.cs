using System.Collections.Generic;

using KSPM.Network.Chat.Messages;
using KSPM.Network.Common;

namespace KSPM.Network.Chat.Group
{
    public abstract class ChatGroup
    {
        protected static short ChatGroupCounter = 1;

        /// <summary>
        /// Unique id
        /// </summary>
        protected short id;

        /// <summary>
        /// List of the messages that belongs to this group.
        /// </summary>
        protected List<ChatMessage> messages;

        /// <summary>
        /// Dictionary with the member of this group.
        /// </summary>
        protected Dictionary<System.Guid, NetworkEntity> members;

        /// <summary>
        /// Performance data structure iterate throught it.
        /// </summary>
        protected List<NetworkEntity> performanceDataStructureMembers;

        public ChatGroup()
        {
            this.id = ChatGroup.ChatGroupCounter++;
            this.messages = new List<ChatMessage>();
            this.members = new Dictionary<System.Guid, NetworkEntity>();
            this.performanceDataStructureMembers = new List<NetworkEntity>();
        }

        public void AddNewMember(NetworkEntity newMember)
        {
            if (!this.members.ContainsKey(newMember.Id))
            {
                this.members.Add(newMember.Id, newMember);
                this.performanceDataStructureMembers.Add(newMember);
            }
        }

        public void RemoveMember(NetworkEntity memberToRemove)
        {
            if (this.members.ContainsKey(memberToRemove.Id))
            {
                this.performanceDataStructureMembers.Remove(memberToRemove);
                this.members.Remove(memberToRemove.Id);
            }
        }

        public void AddMessage(ChatMessage newMessage)
        {
            this.messages.Add(newMessage);
        }

        public short Id
        {
            get
            {
                return this.id;
            }
        }

        public List<NetworkEntity> MembersAsList
        {
            get
            {
                return this.performanceDataStructureMembers;
            }
        }

        public abstract void Release();
    }
}

using System.Collections.Generic;

using KSPM.Network.Chat.Group;
using KSPM.Network.Chat.Messages;

namespace KSPM.Network.Chat
{
    public class ChatManager
    {
        /// <summary>
        /// Holds those ChatGroups registered into the KSPM Chat system.
        /// </summary>
        protected Dictionary<short, ChatGroup> chatGroups;

        /// <summary>
        /// Default chat group where each incoming chat message which does not belong to any group will be stored.
        /// </summary>
        protected ChatGroup defaultChatGroup;

        public ChatManager()
        {
            this.chatGroups = new Dictionary<short, ChatGroup>();
            this.defaultChatGroup= new NMChatGroup();
            this.chatGroups.Add(this.defaultChatGroup.Id, this.defaultChatGroup);
        }

        /// <summary>
        /// Register a new group into the system.
        /// </summary>
        /// <param name="newGroup">New chatgroup.</param>
        public void RegisterChatGroup(ChatGroup newGroup)
        {
            if (!this.chatGroups.ContainsKey(newGroup.Id))
            {
                this.chatGroups.Add(newGroup.Id, newGroup);
            }
        }

        public void Release()
        {
            foreach (KeyValuePair< short, ChatGroup> entry in this.chatGroups )
            {
                entry.Value.Release();
            }
            this.chatGroups.Clear();
            this.chatGroups = null;
        }

        /// <summary>
        /// Attaches a new message into the specified group id, if the group is not found then the message is added to the default group.
        /// </summary>
        /// <param name="incomingMessage"></param>
        /// <returns></returns>
        public ChatGroup AttachMessage(ChatMessage incomingMessage)
        {
            ChatGroup referredGroup = null;
            if (incomingMessage == null)
            {
                return this.defaultChatGroup;
            }
            if (this.chatGroups.TryGetValue(incomingMessage.GroupId, out referredGroup))
            {
                referredGroup.AddMessage(incomingMessage);
                return referredGroup;
            }
            else
            {
                this.defaultChatGroup.AddMessage(incomingMessage);
            }
            return this.defaultChatGroup;
        }
    }
}

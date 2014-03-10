using System.Collections.Generic;

using KSPM.Network.Chat.Group;

namespace KSPM.Network.Chat
{
    public class ChatManager
    {
        /// <summary>
        /// Holds those ChatGroups registered into the KSPM Chat system.
        /// </summary>
        protected Dictionary<short, ChatGroup> chatGroups;

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
    }
}

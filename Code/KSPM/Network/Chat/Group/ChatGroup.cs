using System.Collections.Generic;

using KSPM.Network.Chat.Messages;
using KSPM.Network.Common;

namespace KSPM.Network.Chat.Group
{
    /// <summary>
    /// Abstract Chatgroup class to gather members an create a group.
    /// </summary>
    public abstract class ChatGroup
    {
        /// <summary>
        /// Static counter to create sucesive an unique group ids.
        /// Up to 65535 unique ids.
        /// </summary>
        protected static short ChatGroupCounter = 1;

        /// <summary>
        /// Unique id
        /// </summary>
        protected short id;

        /// <summary>
        /// Dictionary with the members of this group.
        /// </summary>
        protected Dictionary<System.Guid, NetworkEntity> members;

        /// <summary>
        /// Performance data structure, use it to iterate throught it.
        /// </summary>
        protected List<NetworkEntity> performanceDataStructureMembers;

        /// <summary>
        /// Tells if the chat groups is private or not.
        /// </summary>
        protected bool privateGroup;

        /// <summary>
        /// Chatgroup's name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Creates a new ChatGroup, setting it as public.
        /// </summary>
        public ChatGroup()
        {
            this.id = ChatGroup.ChatGroupCounter++;
            this.members = new Dictionary<System.Guid, NetworkEntity>();
            this.performanceDataStructureMembers = new List<NetworkEntity>();
            this.privateGroup = false;
            this.Name = string.Format("Chatgroup-{0}", this.id);
        }

        /// <summary>
        /// Adds a new member to the group.
        /// </summary>
        /// <param name="newMember"></param>
        public void AddNewMember(NetworkEntity newMember)
        {
            if (!this.members.ContainsKey(newMember.Id))
            {
                this.members.Add(newMember.Id, newMember);
                this.performanceDataStructureMembers.Add(newMember);
            }
        }

        /// <summary>
        /// Removes a member from the group.
        /// </summary>
        /// <param name="memberToRemove"></param>
        public void RemoveMember(NetworkEntity memberToRemove)
        {
            if (this.members.Count > 0)
            {
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Removing: " + memberToRemove.Id);
                if (this.members.ContainsKey(memberToRemove.Id))
                {
                    this.performanceDataStructureMembers.Remove(memberToRemove);
                    this.members.Remove(memberToRemove.Id);
                }
            }
        }

        /// <summary>
        /// Removes the whole members from this group.
        /// </summary>
        public void RemoveAllMembers()
        {
            if (this.members.Count > 0)
            {
                this.performanceDataStructureMembers.Clear();
                this.members.Clear();
            }
        }

        #region Getters/Setters

        /// <summary>
        /// Gets the group Id assigned.
        /// </summary>
        public short Id
        {
            get
            {
                return this.id;
            }
        }

        /// <summary>
        /// Gets the group members as a list.<b>To a fast iteration through them.</b>
        /// </summary>
        public List<NetworkEntity> MembersAsList
        {
            get
            {
                return this.performanceDataStructureMembers;
            }
        }

        /// <summary>
        /// Gets if the current group is private or not.
        /// </summary>
        public bool IsPrivate
        {
            get
            {
                return this.privateGroup;
            }
        }

        #endregion

        /// <summary>
        /// Abstract method to add a message to the group.
        /// </summary>
        /// <param name="newMessage"></param>
        public abstract void AddMessage(ChatMessage newMessage);

        /// <summary>
        /// Abstract method that must be used to Purge the group.
        /// </summary>
        public abstract void Purge();

        /// <summary>
        /// Abstract method that must be used to Release the group.
        /// </summary>
        public abstract void Release();
    }
}
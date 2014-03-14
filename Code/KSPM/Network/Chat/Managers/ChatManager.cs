using System.Collections.Generic;

using KSPM.Network.Chat.Group;
using KSPM.Network.Chat.Messages;
using KSPM.Network.Common;

using KSPM.Network.Common.Messages;
using KSPM.Network.Common.Packet;

using KSPM.Network.Client;

using KSPM.Globals;


namespace KSPM.Network.Chat.Managers
{
    public class ChatManager
    {
        /// <summary>
        /// Tells in which groups an user is going to be registered.
        /// </summary>
        public enum UserRegisteringMode:byte { Public, Private, Both };

        /// <summary>
        /// Holds those ChatGroups registered into the KSPM Chat system.
        /// </summary>
        protected Dictionary<short, ChatGroup> chatGroups;

        /// <summary>
        /// Default chat group where each incoming chat message which does not belong to any group will be stored.
        /// </summary>
        protected ChatGroup defaultChatGroup;

        protected NetworkEntity owner;

        public ChatManager()
        {
            this.chatGroups = new Dictionary<short, ChatGroup>();
            this.defaultChatGroup= new NMChatGroup();
            this.chatGroups.Add(this.defaultChatGroup.Id, this.defaultChatGroup);
        }

        /// <summary>
        /// Releases all the ChatGroups, calling Release method on each one.
        /// </summary>
        public void Release()
        {
            foreach (KeyValuePair<short, ChatGroup> entry in this.chatGroups)
            {
                entry.Value.Release();
            }
            this.chatGroups.Clear();
            this.chatGroups = null;
        }

        #region GroupHandling

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

        /// <summary>
        /// Gets the available chats registered on the system populated into a List.
        /// </summary>
        public List<ChatGroup> AvailableGroupList
        {
            get
            {
                List<ChatGroup> groups = new List<ChatGroup>();
                foreach (KeyValuePair<short, ChatGroup> entry in this.chatGroups)
                {
                    groups.Add(entry.Value);
                }
                return groups;
            }
        }

        /// <summary>
        /// Gets how many chat groups are registered inside the manager.
        /// </summary>
        public int RegisteredGroups
        {
            get
            {
                return this.chatGroups.Count;
            }
        }

        public static Error.ErrorType CreateChatManagerFromMessage(byte[] rawBytes, out ChatManager manager)
        {
            short shortBuffer;
            short availableGroups;
            ChatGroup chatRoom = null;
            manager = null;
            int readingOffset = (int)KSPM.Network.Common.Packet.PacketHandler.RawMessageHeaderSize + 1;
            availableGroups = System.BitConverter.ToInt16(rawBytes, readingOffset);
            if (availableGroups < 0)
            {
                return Error.ErrorType.ChatInvalidAvailableGroups;
            }
            manager = new ChatManager();
            readingOffset += 2;
            for (int i = 0; i < availableGroups; i++)
            {
                ///Getting the group Id
                shortBuffer = System.BitConverter.ToInt16(rawBytes, readingOffset);
                readingOffset += 2;
                chatRoom = new NMChatGroup(shortBuffer);
                ///Getting the name's length.
                shortBuffer = (byte)System.BitConverter.ToChar(rawBytes, readingOffset);
                readingOffset += 1;

                ///Getting the name.
                KSPM.Globals.KSPMGlobals.Globals.StringEncoder.GetString(rawBytes, readingOffset, shortBuffer, out chatRoom.Name);
                readingOffset += shortBuffer;
                manager.RegisterChatGroup(chatRoom);
            }
            return Error.ErrorType.Ok;
        }

        #endregion

        #region MessageHandling

        public void SendChatMessage(ChatGroup targetGroup, string bodyMessage)
        {
            Message chatMessage = null;
            ManagedMessage managedReference;
            if(ChatMessage.CreateChatMessage(this.owner, targetGroup, bodyMessage, out chatMessage) == Error.ErrorType.Ok )
            {
                managedReference = (ManagedMessage) chatMessage;
                PacketHandler.EncodeRawPacket(ref managedReference.OwnerNetworkEntity.ownerNetworkCollection.rawBuffer);
                ((GameClient)this.owner).OutgoingTCPQueue.EnqueueCommandMessage(ref chatMessage);
                //KSPMGlobals.Globals.KSPMServer.outgoingMessagesQueue.EnqueueCommandMessage(ref chatMessage);
            }
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

        #endregion

        #region UserHandling

        public int RegisterUser(NetworkEntity newUser, UserRegisteringMode mode)
        {
            int matchingGroups = 0;
            foreach (KeyValuePair<short, ChatGroup> entry in this.chatGroups)
            {
                if (!entry.Value.IsPrivate)
                {
                    entry.Value.AddNewMember(newUser);
                    matchingGroups++;
                }
            }
            return matchingGroups;
        }

        public void UnregisterUser(NetworkEntity userToRemove)
        {
            foreach (KeyValuePair<short, ChatGroup> entry in this.chatGroups)
            {
                entry.Value.RemoveMember(userToRemove);
            }
        }

        public NetworkEntity Owner
        {
            get
            {
                return this.owner;
            }
            set
            {
                this.owner = value;
            }
        }

        #endregion
    }
}

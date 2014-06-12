using System.Collections.Generic;

using KSPM.Network.Chat.Group;
using KSPM.Network.Chat.Messages;
using KSPM.Network.Chat.Filter;
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
        /// Tells in what groups an user is going to be registered.
        /// </summary>
        public enum UserRegisteringMode:byte { Public, Private, Both };

        public enum FilteringMode : byte
        {
            /// <summary>
            /// Tells that the message must fit all the filters to be filtered.
            /// </summary>
            And,

            /// <summary>
            /// Tells that the message must fit at least one filter to be filtered.
            /// </summary>
            Or,
        };

        /// <summary>
        /// Holds those ChatGroups registered into the KSPM Chat system.
        /// </summary>
        protected Dictionary<short, ChatGroup> chatGroups;

        /// <summary>
        /// Default chat group where each incoming chat message which does not belong to any group will be stored.
        /// </summary>
        protected ChatGroup defaultChatGroup;

        /// <summary>
        /// Holds those filters what would be applied to the incoming messages.
        /// </summary>
        protected List<ChatFilter> availableFilters;

        /// <summary>
        /// Network Entity to whom belongs this chat manager.
        /// </summary>
        protected NetworkEntity owner;

        /// <summary>
        /// Defines how the default chat group would be created.
        /// </summary>
        public enum DefaultStorageMode : byte { Persistent, NonPersistent };

        public ChatManager( DefaultStorageMode mode)
        {
            this.chatGroups = new Dictionary<short, ChatGroup>();

            if (mode == DefaultStorageMode.Persistent)
            {
                this.defaultChatGroup = new NMChatGroup();
            }
            else
            {
                this.defaultChatGroup = new NonPersistentNMChatgroup();
            }
            
            this.chatGroups.Add(this.defaultChatGroup.Id, this.defaultChatGroup);
            this.availableFilters = new List<ChatFilter>();
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

            for (int i = 0; i < this.availableFilters.Count; i++)
            {
                this.availableFilters[i].Release();
            }
            this.availableFilters.Clear();
            this.availableFilters = null;
        }

        #region GroupHandling

        /// <summary>
        /// Register a new group into the system.
        /// </summary>
        /// <param name="newGroup">New chatgroup.</param>
        public void RegisterChatGroup(ChatGroup newGroup)
        {
            if (newGroup == null)
                return;
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
        /// Tries to get a chatgroup identified with the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The ChatGroup identified by the given id or the default ChatGroup if there is no chatGroup identified by the given id.</returns>
        public ChatGroup GetChatGroupById(short id)
        {
            ChatGroup requestedGroup = null;
            if (this.chatGroups.ContainsKey(id))
            {
                this.chatGroups.TryGetValue(id, out requestedGroup);
            }
            else
            {
                return this.defaultChatGroup;
            }
            return requestedGroup;
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

        public static Error.ErrorType CreateChatManagerFromMessage(byte[] rawBytes,ChatManager.DefaultStorageMode mode , out ChatManager manager)
        {
            short shortBuffer;
            short availableGroups;
            ChatGroup chatRoom = null;
            manager = null;
            int readingOffset = (int)KSPM.Network.Common.Packet.PacketHandler.RawMessageHeaderSize + 1 + 4;
            availableGroups = System.BitConverter.ToInt16(rawBytes, readingOffset);
            if (availableGroups < 0)
            {
                return Error.ErrorType.ChatInvalidAvailableGroups;
            }
            manager = new ChatManager(mode);
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

        /// <summary>
        /// Takes a string and sends it to the server using the TCP connection.
        /// </summary>
        /// <param name="targetGroup"></param>
        /// <param name="bodyMessage"></param>
        public void SendChatMessage(ChatGroup targetGroup, string bodyMessage)
        {
            Message chatMessage = null;
            ManagedMessage managedReference;
            if(ChatMessage.CreateChatMessage(this.owner, targetGroup, bodyMessage, out chatMessage) == Error.ErrorType.Ok )
            {
                managedReference = (ManagedMessage) chatMessage;
                PacketHandler.EncodeRawPacket(ref managedReference.OwnerNetworkEntity.ownerNetworkCollection.rawBuffer);
                if (!((GameClient)this.owner).OutgoingTCPQueue.EnqueueCommandMessage(ref chatMessage))
                {
                    chatMessage.Release();
                    chatMessage = null;
                }
                //KSPMGlobals.Globals.KSPMServer.outgoingMessagesQueue.EnqueueCommandMessage(ref chatMessage);
            }
        }

        public void SendUDPChatMessage(ChatGroup targetGroup, string bodyMessage)
        {
            Message chatMessage = ((GameClient)this.owner).UDPIOMessagesPool.BorrowMessage;
            if (ChatMessage.LoadUDPChatMessage(this.owner, targetGroup, bodyMessage, ref chatMessage) == Error.ErrorType.Ok)
            {
                if (!((GameClient)this.owner).OutgoingUDPQueue.EnqueueCommandMessage(ref chatMessage))
                {
                    ///UDP queue is full, so recycling the message.
                    ((GameClient)this.owner).UDPIOMessagesPool.Recycle(chatMessage);
                }
                //KSPMGlobals.Globals.KSPMServer.outgoingMessagesQueue.EnqueueCommandMessage(ref chatMessage);
            }
            else
            {
                ///Something went wrong loading the UDP chat message.
                ((GameClient)this.owner).UDPIOMessagesPool.Recycle(chatMessage);
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

        public bool ApplyFilters(ChatMessage targetMessage, FilteringMode mode)
        {
            bool filtered = true;
            if (targetMessage == null)
                return false;
            lock (this.availableFilters)
            {
                if (this.availableFilters.Count == 0)
                    return false;
                switch (mode)
                {
                    case FilteringMode.And:
                        for (int i = 0; i < this.availableFilters.Count; i++)
                        {
                            if (!this.availableFilters[i].Query(targetMessage))
                            {
                                filtered = false;
                                break;
                            }
                        }
                        break;
                    case FilteringMode.Or:
                        for (int i = 0; i < this.availableFilters.Count; i++)
                        {
                            if (this.availableFilters[i].Query(targetMessage))
                            {
                                filtered = true;
                                break;
                            }
                        }
                        break;
                }
            }
            return filtered;
        }

        #endregion

        #region UserHandling

        public int RegisterUser(NetworkEntity newUser, UserRegisteringMode mode)
        {
            int matchingGroups = 0;
            if (newUser == null)
                return 0;
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
            if (userToRemove == null)
                return;
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

        #region Filtering

        /// <summary>
        /// Adds a new filter to the available filters.
        /// </summary>
        /// <param name="newFilter"></param>
        public void RegisterFilter(ChatFilter newFilter)
        {
            if (newFilter == null)
                return;
            lock (this.availableFilters)
            {
                this.availableFilters.Add(newFilter);
            }
        }

        /// <summary>
        /// Tries to remove the given filter from the available filters.
        /// </summary>
        /// <param name="filter"></param>
        public void UnregisterFilter(ChatFilter filter)
        {
            if (filter == null)
                return;
            lock (this.availableFilters)
            {
                this.availableFilters.Remove(filter);
            }
        }

        /// <summary>
        /// Gets the available Filters as an Array.
        /// </summary>
        public ChatFilter[] AvailableFilters
        {
            get
            {
                ChatFilter[] returnedList = new ChatFilter[this.availableFilters.Count];
                lock (this.availableFilters)
                {
                    this.availableFilters.CopyTo(returnedList);
                }
                return returnedList;
            }
        }

        #endregion
    }
}

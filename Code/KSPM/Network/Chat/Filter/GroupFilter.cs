using System.Collections.Generic;

using KSPM.Network.Chat.Group;

namespace KSPM.Network.Chat.Filter
{
    /// <summary>
    /// Works as BlackList of chat groups.
    /// </summary>
    public class GroupFilter : ChatFilter
    {
        /// <summary>
        /// Set of groups to be filtered.
        /// </summary>
        protected Dictionary<short, ChatGroup> filterStatement;

        /// <summary>
        /// Creates an empty filter.<b>None is filtered.</b>
        /// </summary>
        public GroupFilter() : base()
        {
            this.filterStatement = new Dictionary<short, ChatGroup>();
        }

        /// <summary>
        /// Adds a group to the filter.
        /// </summary>
        /// <param name="groupToBeFiltered"></param>
        public void AddToFilter(ChatGroup groupToBeFiltered)
        {
            if (groupToBeFiltered == null)
                return;
            if (!this.filterStatement.ContainsKey(groupToBeFiltered.Id))
            {
                this.filterStatement.Add(groupToBeFiltered.Id, groupToBeFiltered);
            }
        }

        /// <summary>
        /// Removes a group from the filter.
        /// </summary>
        /// <param name="referredGroup"></param>
        public void RemoveFromFilter(ChatGroup referredGroup)
        {
            if (referredGroup == null)
                return;
            if (this.filterStatement.ContainsKey(referredGroup.Id))
            {
                this.filterStatement.Remove(referredGroup.Id);
            }
        }

        /// <summary>
        /// Applies the filtering statement to the given message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>True if the message fits the filtering statement.</returns>
        public override bool Query(Messages.ChatMessage message)
        {
            return message != null && this.filterStatement.ContainsKey(message.GroupId);
        }

        /// <summary>
        /// Releases the filter.
        /// </summary>
        public override void Release()
        {
            this.filterStatement.Clear();
            this.filterStatement = null;
        }
    }
}

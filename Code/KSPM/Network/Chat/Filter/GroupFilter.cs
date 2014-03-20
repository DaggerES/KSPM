using System.Collections.Generic;

using KSPM.Network.Chat.Group;

namespace KSPM.Network.Chat.Filter
{
    /// <summary>
    /// Works as BlackList of chat groups.
    /// </summary>
    public class GroupFilter : ChatFilter
    {
        protected Dictionary<short, ChatGroup> filterStatement;

        public GroupFilter()
        {
            this.filterStatement = new Dictionary<short, ChatGroup>();
        }

        public void AddToFilter(ChatGroup groupToBeFiltered)
        {
            if (groupToBeFiltered == null)
                return;
            if (!this.filterStatement.ContainsKey(groupToBeFiltered.Id))
            {
                this.filterStatement.Add(groupToBeFiltered.Id, groupToBeFiltered);
            }
        }

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
    }
}

using KSPM.Network.Chat.Group;
using KSPM.Network.Chat.Messages;

namespace KSPM.Network.Chat.Filter
{
    public abstract class ChatFilter
    {
        protected static short IdGenerator = 0;
        protected short id;

        public ChatFilter()
        {
            this.id = ChatFilter.IdGenerator++;
        }

        /// <summary>
        /// Applies the filter statemen over the given ChatMessage reference.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public abstract bool Query(ChatMessage message);

        /// <summary>
        /// Releases all resources handled by the filter.
        /// </summary>
        public abstract void Release();

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                ChatFilter reference = (ChatFilter)obj;
                return reference.id == this.id;
            }
        }

        public override int GetHashCode()
        {
            return this.id.GetHashCode();
        }
    }
}

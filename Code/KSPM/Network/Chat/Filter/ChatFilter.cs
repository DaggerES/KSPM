using KSPM.Network.Chat.Group;
using KSPM.Network.Chat.Messages;

namespace KSPM.Network.Chat.Filter
{
    /// <summary>
    /// Abstrac class to every filter to be applied on chat messages.
    /// </summary>
    public abstract class ChatFilter
    {
        /// <summary>
        /// Counter to geneterate unique ids to every filter created.<b>Static.</b>
        /// </summary>
        protected static short IdGenerator = 0;

        /// <summary>
        /// Unique id.
        /// </summary>
        protected short id;

        /// <summary>
        /// Creates a new reference and sets the id to a proper one.
        /// </summary>
        public ChatFilter()
        {
            this.id = ChatFilter.IdGenerator++;
        }

        /// <summary>
        /// Applies the filter statement over the given ChatMessage reference.<b>Abstract.</b>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public abstract bool Query(ChatMessage message);

        /// <summary>
        /// Releases all resources handled by the filter.<b>Abstract.</b>
        /// </summary>
        public abstract void Release();

        /// <summary>
        /// Tells if this objects is the same as the given as argument.
        /// </summary>
        /// <param name="obj">Another object to be tested.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the hash code of this reference.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.id.GetHashCode();
        }
    }
}

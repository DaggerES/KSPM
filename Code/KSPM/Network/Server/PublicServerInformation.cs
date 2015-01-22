/*
	<file name="ServerInformation.cs"/>
	<author>Scr_Ra</author>
	<date>1/8/2015 12:20:33 PM</date>
*/

using System;
using System.Collections.Generic;

namespace KSPM.Network.Server
{
    /// <sumary>
    /// Definition for ServerInformation.
    ///	ServerInformation is a: human readable information.
    /// </sumary>
    public class PublicServerInformation
    {
        /// <summary>
        /// Amount of characters available to the name as maximun size.
        /// </summary>
        protected static int MaxNameLength = 32;

        /// <summary>
        /// Name of the server, used by external agents.
        /// </summary>
        protected string name;

        /// <summary>
        /// Number of connected players.<b>It is a snapshot so this can change at any time.</b>
        /// </summary>
        protected short connectedPlayers;

        /// <summary>
        /// Byte array to hold the information in raw format, ready to be sent.
        /// </summary>
        protected internal byte[] informationBuffer;

        /// <summary>
        /// Number of bytes with an usable information.
        /// </summary>
        protected internal short usableBytes;

        
        /// <summary>
        /// Creates an empty reference, setting the information buffer to 256 characters.
        /// </summary>
        public PublicServerInformation()
        {
            this.connectedPlayers = 0;
            this.informationBuffer = new byte[256];
            this.UpdateInformation("MyServer");
        }

        /// <summary>
        /// Gets the server name.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Gets the number of connected players.
        /// </summary>
        public short ConnectedPlayers
        {
            get
            {
                return this.connectedPlayers;
            }
            set
            {
                if( value >= 0 )
                {
                    this.connectedPlayers = value;
                    ///Reading the size of the name on the bit-0  and plus 1
                    Buffer.BlockCopy(System.BitConverter.GetBytes(this.connectedPlayers), 0, this.informationBuffer, this.informationBuffer[2] + 1, 2);
                }
            }
        }

        /// <summary>
        /// Updates the buffer filling it with the current information held by the reference itself.
        /// </summary>
        /// <param name="newName"></param>
        public void UpdateInformation(string newName)
        {
            byte [] buffer = null;
            if( newName != null && newName.Length > 0)
            {
                this.usableBytes = 2;///At least the size of the buffer.
                KSPM.Globals.KSPMGlobals.Globals.StringEncoder.GetBytes( newName, out buffer );
                Buffer.BlockCopy(buffer, 0, this.informationBuffer, 3, buffer.Length);
                ///Maximun 255 characters
                this.informationBuffer[2] = (byte)buffer.Length;
                this.usableBytes = (short)(buffer.Length + 1);
                ///Writing the connected players bytes.
                Buffer.BlockCopy(System.BitConverter.GetBytes(this.connectedPlayers), 0, this.informationBuffer, this.usableBytes, 2);
                this.usableBytes += 2;

                Buffer.BlockCopy(System.BitConverter.GetBytes(this.usableBytes), 0, this.informationBuffer, 0, 2);
            }
        }

        /// <summary>
        /// Releases all resources used by the object, becoming unusable.
        /// </summary>
        public void Release()
        {
            this.name = null;
            this.connectedPlayers = -1;
            this.informationBuffer = null;
        }
    }
}

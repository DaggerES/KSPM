using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;

using KSPM.Network.Common;
using KSPM.Network.Common.Packet;
using KSPM.Game;

using KSPM.Network.Client;
using KSPM.Network.NAT;

namespace FakeKSPMClient
{
    public partial class Form1 : Form
    {
        Socket clientSocket;
        Socket udpSocket;
        IPEndPoint serverIPEndPoint;
        NetworkEntity myNetworkEntity;

        GameClient client;
        GameUser gameUser;
        ServerInformation serverInformation;
        UTF8Encoding utf8Strings;

        string userName;

        public Form1()
        {
            InitializeComponent();
            KSPM.Globals.KSPMGlobals.Globals.InitiLogging(KSPM.IO.Logging.Log.LogginMode.File, false);

            this.comboBoxCommands.Items.AddRange( Enum.GetNames( typeof(KSPM.Network.Common.Messages.Message.CommandType) ) );
            System.Guid asd = Guid.NewGuid();
            this.client = new GameClient();
            this.utf8Strings = new UTF8Encoding();
            
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            serverIPEndPoint = new IPEndPoint(IPAddress.Parse(textBoxIP.Text), Int32.Parse(textBoxPort.Text));
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
            clientSocket.Bind( new IPEndPoint( IPAddress.Any, 0 ));
            myNetworkEntity = new NetworkEntity(ref this.clientSocket);
            clientSocket.Connect(serverIPEndPoint);
            if (clientSocket.Poll(1000, SelectMode.SelectWrite))
            {
                checkBox1.Checked = clientSocket.Connected;
            }            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            KSPM.Network.Common.Messages.Message messageToSend = null;
            string tmpUserName;
            string[] splitedUserInfo;
            int bytesCount;
            byte[] utf8Bytes;
            byte[] hashCode;
            GameUser user;
            switch ((KSPM.Network.Common.Messages.Message.CommandType)this.comboBoxCommands.SelectedIndex)
            {
                case KSPM.Network.Common.Messages.Message.CommandType.NewClient:
                    KSPM.Network.Common.Messages.Message.NewUserMessage(myNetworkEntity, out messageToSend);
                    PacketHandler.EncodeRawPacket(ref myNetworkEntity.ownerNetworkCollection.rawBuffer);
                    this.myNetworkEntity.ownerNetworkCollection.socketReference.Send(myNetworkEntity.ownerNetworkCollection.rawBuffer, (int)messageToSend.MessageBytesSize, SocketFlags.None);
                    //this.myNetworkEntity.ownerNetworkCollection.socketReference.Send(myNetworkEntity.ownerNetworkCollection.rawBuffer);
                    break;
                case KSPM.Network.Common.Messages.Message.CommandType.Disconnect:
                    KSPM.Network.Common.Messages.Message.DisconnectMessage(myNetworkEntity, out messageToSend);
                    PacketHandler.EncodeRawPacket(ref myNetworkEntity.ownerNetworkCollection.rawBuffer);
                    this.myNetworkEntity.ownerNetworkCollection.socketReference.Send(myNetworkEntity.ownerNetworkCollection.rawBuffer, (int)messageToSend.MessageBytesSize, SocketFlags.None);
                    //this.myNetworkEntity.ownerNetworkCollection.socketReference.Send(myNetworkEntity.ownerNetworkCollection.rawBuffer);
                    break;
                case  KSPM.Network.Common.Messages.Message.CommandType.Authentication:
                    tmpUserName = textBoxCommands.Text;
                    utf8Bytes = utf8Strings.GetBytes(tmpUserName);
                    KSPM.IO.Security.Hash.GetHash(ref utf8Bytes, 0, (uint)utf8Bytes.Length, out hashCode);
                    user = new GameUser(ref tmpUserName, ref hashCode);
                    User asd = user;
                    KSPM.Network.Common.Messages.Message.AuthenticationMessage(myNetworkEntity, asd, out messageToSend);
                    this.myNetworkEntity.ownerNetworkCollection.socketReference.Send(myNetworkEntity.ownerNetworkCollection.rawBuffer, (int)messageToSend.MessageBytesSize, SocketFlags.None);
                    //this.myNetworkEntity.ownerNetworkCollection.socketReference.Send(myNetworkEntity.ownerNetworkCollection.rawBuffer);
                    break;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            KSPM.Network.Common.Messages.Message messageToSend = null;
            if (myNetworkEntity.ownerNetworkCollection.socketReference.Connected)
            {
                KSPM.Network.Common.Messages.Message.DisconnectMessage(myNetworkEntity, out messageToSend);
                PacketHandler.EncodeRawPacket(ref myNetworkEntity.ownerNetworkCollection.rawBuffer);
                this.myNetworkEntity.ownerNetworkCollection.socketReference.Send(myNetworkEntity.ownerNetworkCollection.rawBuffer);
                this.myNetworkEntity.ownerNetworkCollection.socketReference.Disconnect(true);
                this.checkBox1.Checked = this.myNetworkEntity.ownerNetworkCollection.socketReference.Connected;
            }
        }

        /// <summary>
        /// Connect button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            byte[] utf8Bytes;
            this.userName = textBoxUsername.Text;
            utf8Bytes = utf8Strings.GetBytes( this.userName );
            this.gameUser = new GameUser(ref this.userName, ref utf8Bytes);
            this.serverInformation = new ServerInformation();
            this.serverInformation.ip = textBoxServerInfoIP.Text;
            this.serverInformation.port = int.Parse(textBoxServerInfoPort.Text);

            this.client.SetGameUser(this.gameUser);
            this.client.SetServerHostInformation(this.serverInformation);
            this.client.InitializeClient();

            this.client.Connect();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.client.Release();
        }
    }
}

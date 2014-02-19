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

namespace FakeKSPMClient
{
    public partial class Form1 : Form
    {
        Socket clientSocket;
        Socket udpSocket;
        IPEndPoint serverIPEndPoint;
        NetworkEntity myNetworkEntity;
        string userName;

        public Form1()
        {
            InitializeComponent();
            this.comboBoxCommands.Items.AddRange( Enum.GetNames( typeof(KSPM.Network.Common.Messages.Message.CommandType) ) );
            System.Guid asd = Guid.NewGuid();
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
            UTF8Encoding utf8Strings = new UTF8Encoding();
            switch ((KSPM.Network.Common.Messages.Message.CommandType)this.comboBoxCommands.SelectedIndex)
            {
                case KSPM.Network.Common.Messages.Message.CommandType.NewClient:
                    KSPM.Network.Common.Messages.Message.NewUserMessage(myNetworkEntity, out messageToSend);
                    PacketHandler.EncodeRawPacket(ref myNetworkEntity.ownerNetworkCollection.rawBuffer);
                    this.myNetworkEntity.ownerNetworkCollection.socketReference.Send(myNetworkEntity.ownerNetworkCollection.rawBuffer);
                    break;
                case KSPM.Network.Common.Messages.Message.CommandType.Disconnect:
                    KSPM.Network.Common.Messages.Message.DisconnectMessage(myNetworkEntity, out messageToSend);
                    PacketHandler.EncodeRawPacket(ref myNetworkEntity.ownerNetworkCollection.rawBuffer);
                    this.myNetworkEntity.ownerNetworkCollection.socketReference.Send(myNetworkEntity.ownerNetworkCollection.rawBuffer);
                    break;
                case  KSPM.Network.Common.Messages.Message.CommandType.Authentication:
                    tmpUserName = textBoxCommands.Text;
                    utf8Bytes = utf8Strings.GetBytes(tmpUserName);
                    KSPM.IO.Security.Hash.GetHash(ref utf8Bytes, 0, (uint)utf8Bytes.Length, out hashCode);
                    user = new GameUser(ref tmpUserName, ref hashCode);
                    User asd = user;
                    KSPM.Network.Common.Messages.Message.AuthenticationMessage(myNetworkEntity, ref asd, out messageToSend);
                    this.myNetworkEntity.ownerNetworkCollection.socketReference.Send(myNetworkEntity.ownerNetworkCollection.rawBuffer);
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
    }
}

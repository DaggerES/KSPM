﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Net.Sockets;
using System.Net;

using KSPM.Network.Common;
using KSPM.Network.Common.Packet;

namespace FakeKSPMClient
{
    public partial class Form1 : Form
    {
        Socket clientSocket;
        IPEndPoint serverIPEndPoint;
        NetworkEntity myNetworkEntity;

        public Form1()
        {
            InitializeComponent();
            this.comboBoxCommands.Items.AddRange( Enum.GetNames( typeof(KSPM.Network.Common.Message.CommandType) ) );
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            serverIPEndPoint = new IPEndPoint(IPAddress.Parse(textBoxIP.Text), Int32.Parse(textBoxPort.Text));
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Bind( new IPEndPoint( IPAddress.Any, 0 ));
            myNetworkEntity = new NetworkEntity(ref this.clientSocket);
            clientSocket.Connect(serverIPEndPoint);
            checkBox1.Checked = clientSocket.Connected;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            KSPM.Network.Common.Message messageToSend = null;
            switch ((KSPM.Network.Common.Message.CommandType)this.comboBoxCommands.SelectedIndex)
            {
                case KSPM.Network.Common.Message.CommandType.NewClient:
                    KSPM.Network.Common.Message.NewUserMessage(ref myNetworkEntity, out messageToSend);
                    PacketHandler.EncodeRawPacket(ref myNetworkEntity);
                    this.myNetworkEntity.ownerSocket.Send(myNetworkEntity.rawBuffer);
                    break;

                case KSPM.Network.Common.Message.CommandType.Disconnect:
                    KSPM.Network.Common.Message.DisconnectMessage(ref myNetworkEntity, out messageToSend);
                    PacketHandler.EncodeRawPacket(ref myNetworkEntity);
                    this.myNetworkEntity.ownerSocket.Send(myNetworkEntity.rawBuffer);
                    break;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            KSPM.Network.Common.Message messageToSend = null;
            if (myNetworkEntity.ownerSocket.Connected)
            {
                KSPM.Network.Common.Message.DisconnectMessage(ref myNetworkEntity, out messageToSend);
                PacketHandler.EncodeRawPacket(ref myNetworkEntity);
                this.myNetworkEntity.ownerSocket.Send(myNetworkEntity.rawBuffer);
                this.myNetworkEntity.ownerSocket.Disconnect(true);
                this.checkBox1.Checked = this.myNetworkEntity.ownerSocket.Connected;
            }
        }
    }
}

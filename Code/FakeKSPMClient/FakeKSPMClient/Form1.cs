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

using KSPM.Network.Common;

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
            switch ((KSPM.Network.Common.Message.CommandType)this.comboBoxCommands.SelectedIndex)
            {
                case KSPM.Network.Common.Message.CommandType.NewClient:
                    KSPM.Network.Common.Message.NewUserMessage(ref myNetworkEntity);
                    this.myNetworkEntity.ownerSocket.Send(myNetworkEntity.rawBuffer);
                    break;
            }
        }
    }
}

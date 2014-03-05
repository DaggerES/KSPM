using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using System.IO;


namespace KSPM.Network.Client.RemoteServer
{
    public class ServerList : System.IDisposable
    {
        [XmlIgnore]
        protected static string ServerListFilename = "KSPMServerList.xml";

        [XmlArray("KSPMServers" )]
        protected List<ServerInformation> hosts;

        /// <summary>
        /// Reads a file a tries to inflate a ServerList with the file's content.<b>If the file can not be read or it were some error a new ServeList is created, written and set as output.</b>
        /// </summary>
        /// <param name="list">Out reference to the ServerList.</param>
        /// <returns>Ok or IOFileCanNotBeWritten.</returns>
        public static KSPM.Network.Common.Error.ErrorType ReadServerList(out ServerList list)
        {
            KSPM.Network.Common.Error.ErrorType result = Common.Error.ErrorType.Ok;
			StreamReader serverListStreamReader = null;
			XmlSerializer serverListSerializer = null;
			XmlTextReader serverListReader = null;
            list = null;
            try
            {
                serverListStreamReader = new StreamReader(ServerList.ServerListFilename, System.Text.UTF8Encoding.UTF8);
                serverListReader = new XmlTextReader(serverListStreamReader);
                serverListSerializer = new XmlSerializer(typeof(ServerList));
                list = (ServerList)serverListSerializer.Deserialize(serverListReader);
                serverListReader.Close();
            }
            catch (FileNotFoundException)///If the file can not be loaded a default one is created iand written.
            {
                list = new ServerList();
				list.hosts.Add (ServerInformation.LoopbackServerInformation);
                result = ServerList.WriteServerList(ref list);
            }
			///Something went wrong trying to parse the XML file.
			catch( XmlException)
			{
				serverListReader.Close();
				list = new ServerList();
				list.hosts.Add (ServerInformation.LoopbackServerInformation);
				result = ServerList.WriteServerList(ref list);
			}
            catch (DirectoryNotFoundException)
            { }
            catch (IOException)
            { }
            catch (System.InvalidOperationException)
            { }
            return result;
        }

        /// <summary>
        /// Writes the ServerList into a file.<b>If the reference is null a new ServerList is created and written.</b>
        /// </summary>
        /// <param name="list">Reference to the ServerList.</param>
        /// <returns>Ok or IOFileCanNotBeWritten.</returns>
        public static KSPM.Network.Common.Error.ErrorType WriteServerList(ref ServerList list)
        {
            XmlTextWriter serverListWriter;
            XmlSerializer serverListSerializer;
            KSPM.Network.Common.Error.ErrorType result = Common.Error.ErrorType.Ok;
            if (list == null)
            {
                list = new ServerList();
            }
            try
            {
                serverListWriter = new XmlTextWriter(ServerList.ServerListFilename, System.Text.UTF8Encoding.UTF8);
                serverListWriter.Formatting = Formatting.Indented;
                serverListSerializer = new XmlSerializer(typeof(ServerList));
                serverListSerializer.Serialize(serverListWriter, list);
                serverListWriter.Close();
            }
            catch (System.Exception ex)
            {
                result = Common.Error.ErrorType.IOFileCanNotBeWritten;
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(ex.Message);
            }
            return result;
        }

        public ServerList()
        {
            this.hosts = new List<ServerInformation>();
        }

        /// <summary>
        /// Gets the ServerInformation List.
        /// </summary>
        [XmlArray("KSPMServers" )]
        public List<ServerInformation> Hosts
        {
            get
            {
                if (this.hosts == null)
                    this.hosts = new List<ServerInformation>();
                return this.hosts;
            }
        }

        /// <summary>
        /// Releases all resources and write itself into a file.
        /// </summary>
        public void Dispose()
        {
            ServerList mutableReference = this;
            ServerList.WriteServerList(ref mutableReference);
            for (int i = 0; i < this.hosts.Count; i++)
            {
                this.hosts[i].Dispose();
            }
            this.hosts.Clear();
        }
    }
}

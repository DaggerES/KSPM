using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSPM.Network.Client;
using KSPM.Network.Client.RemoteServer;
using KSPM.IO.Logging;
using KSPM.Diagnostics;
using KSPM.Globals;
using KSPM.Game;
using KSPM.Network.Chat.Filter;

namespace ConsoleFakeClient
{
    class Program
    {
        public delegate void FloodAsync( ChatBot botReference, ChatBot.FloodMode floodingMode, int targetsIds, long delay);
        public static volatile bool flooding;

        static void Main(string[] args)
        {
            KSPMGlobals.Globals.InitiLogging(Log.LogginMode.Console, false);
            KSPMGlobals.Globals.ChangeIOFilePath(Environment.CurrentDirectory + "/config/");
            ServerInformation server = new ServerInformation();
            ServerList hosts = null;
            ServerList.ReadServerList(out hosts);
            ChatBot.FloodMode mode = ChatBot.FloodMode.TCP;
            
            string delayAsString;
            string idsAsString;
            long delay = 250;
            int targetsIds =0;
            int flag;
            bool exit = false;

            ServerList.WriteServerList(ref hosts);
            GameClient client = new GameClient();
            client.TCPMessageArrived += client_TCPMessageArrived;
            client.UDPMessageArrived += client_UDPMessageArrived;
            ConsoleKeyInfo pressedKey;
            ChatBot bot = new ChatBot(client);
            bot.InitFromFile("chatRecords.txt");
            bot.botClient.SetServerHostInformation(hosts.Hosts[0]);
            client.InitializeClient();
            client.UserDisconnected += new KSPM.Network.Common.Events.UserDisconnectedEventHandler(client_UserDisconnected);
            /*
            GroupFilter group = new GroupFilter();
            group.AddToFilter(client.ChatSystem.AvailableGroupList[0]);
            */

                while (!exit)
                {   
                    Console.WriteLine("Press q to quit");
                    Console.WriteLine("Press c to connect");
                    Console.WriteLine("Press d to disconnect");
                    Console.WriteLine("Press f to flood using TCP");
                    Console.WriteLine("Press g to flood using UDP");
                    Console.WriteLine("Press t to flood UserCommands using TCP");
                    Console.WriteLine("Press y to flood UserCommands using YCP");
                    pressedKey = Console.ReadKey();
                    switch( pressedKey.Key )
                    {
                        case ConsoleKey.Q:
                            if (client.IsAlive())
                            {
                                Program.flooding = false;
                                client.Disconnect();
                            }
                            exit = true;
                            break;
                        case ConsoleKey.C:
                            ///client.SetGameUser(myUser);
                            bot.GenerateRandomUser();
                            client.SetServerHostInformation(hosts.Hosts[ 0 ]);
                            if (client.Connect() == KSPM.Network.Common.Error.ErrorType.Ok)
                            {
                                Console.WriteLine("Connected.............");
                                //System.Threading.Thread.Sleep(1000);
                                //client.ChatSystem.SendChatMessage(client.ChatSystem.AvailableGroupList[0], "HOLA a todos!!!");
                            }
                            break;
                        case ConsoleKey.D:
                            Program.flooding = false;
                            client.Disconnect();
                            break;
                        case ConsoleKey.F:
                            if (Program.flooding)
                                return;
                            bot.GenerateRandomUser();
                            client.SetServerHostInformation(hosts.Hosts[ 0 ]);
                            Console.WriteLine("Type the delay time in miliseconds between each message.");
                            delayAsString = Console.ReadLine();
                            delay = long.Parse(delayAsString);
                            if (client.Connect() == KSPM.Network.Common.Error.ErrorType.Ok)
                            {
                                Console.WriteLine("Connected.............");
                                FloodAsync flooder = new FloodAsync(Program.AsyncFlooder);
                                flooder.BeginInvoke(bot, ChatBot.FloodMode.TCP, 0, delay, Program.OnFloodTerminated, flooder);
                                Program.flooding = true;
                                //System.Threading.Thread.Sleep(1000);
                                //client.ChatSystem.SendChatMessage(client.ChatSystem.AvailableGroupList[0], "HOLA a todos!!!");
                            }
                            break;
                        case ConsoleKey.G:
                            if (Program.flooding)
                                return;
                            bot.GenerateRandomUser();
                            client.SetServerHostInformation(hosts.Hosts[ 0 ]);
                            Console.WriteLine("Type the delay time in miliseconds between each message.");
                            delayAsString = Console.ReadLine();
                            delay = long.Parse(delayAsString);
                            if (client.Connect() == KSPM.Network.Common.Error.ErrorType.Ok)
                            {
                                Console.WriteLine("Connected.............");
                                FloodAsync flooder = new FloodAsync(Program.AsyncFlooder);
                                flooder.BeginInvoke(bot, ChatBot.FloodMode.UDP, 0, delay, Program.OnFloodTerminated, flooder);
                                Program.flooding = true;
                                //System.Threading.Thread.Sleep(1000);
                                //client.ChatSystem.SendChatMessage(client.ChatSystem.AvailableGroupList[0], "HOLA a todos!!!");
                            }
                            break;
                        case ConsoleKey.T:
                            if (Program.flooding)
                                return;
                            bot.GenerateRandomUser();
                            client.SetServerHostInformation(hosts.Hosts[ 0 ]);
                            Console.WriteLine("Type the delay time in miliseconds between each message.");
                            delayAsString = Console.ReadLine();
                            delay = long.Parse(delayAsString);
                            Console.WriteLine("Type the ids of the clients to be reached by your messages, separate them by commas.");
                            idsAsString = Console.ReadLine();
                            if (idsAsString.Length == 0)
                            {
                                targetsIds = int.MaxValue;
                                targetsIds |= -1;
                            }
                            else
                            {
                                targetsIds = 0;
                                foreach (string idStr in idsAsString.Split(','))
                                {
                                    flag = 0;
                                    flag = 1 << (int.Parse(idStr) - 1);
                                    targetsIds |= flag;
                                }
                            }
                            if (client.Connect() == KSPM.Network.Common.Error.ErrorType.Ok)
                            {
                                Console.WriteLine("Connected.............");
                                FloodAsync flooder = new FloodAsync(Program.AsyncFlooder);
                                flooder.BeginInvoke(bot, ChatBot.FloodMode.TCPUser, targetsIds, delay, Program.OnFloodTerminated, flooder);
                                Program.flooding = true;
                                //System.Threading.Thread.Sleep(1000);
                                //client.ChatSystem.SendChatMessage(client.ChatSystem.AvailableGroupList[0], "HOLA a todos!!!");
                            }
                            break;
                        case ConsoleKey.Y:
                            if (Program.flooding)
                                return;
                            bot.GenerateRandomUser();
                            client.SetServerHostInformation(hosts.Hosts[ 0 ]);
                            Console.WriteLine("Type the delay time in miliseconds between each message.");
                            delayAsString = Console.ReadLine();
                            delay = long.Parse(delayAsString);
                            Console.WriteLine("Type the ids of the clients to be reached by your messages, separate them by commas.");
                            idsAsString = Console.ReadLine();
                            if (idsAsString.Length == 0)
                            {
                                targetsIds = int.MaxValue;
                                targetsIds |= -1;
                            }
                            else
                            {
                                targetsIds = 0;
                                foreach (string idStr in idsAsString.Split(','))
                                {
                                    flag = 0;
                                    flag = 1 << (int.Parse(idStr) - 1);
                                    targetsIds |= flag;
                                }
                            }
                            if (client.Connect() == KSPM.Network.Common.Error.ErrorType.Ok)
                            {
                                Console.WriteLine("Connected.............");
                                FloodAsync flooder = new FloodAsync(Program.AsyncFlooder);
                                flooder.BeginInvoke(bot, ChatBot.FloodMode.UDPUser, targetsIds, delay, Program.OnFloodTerminated, flooder);
                                Program.flooding = true;
                                //System.Threading.Thread.Sleep(1000);
                                //client.ChatSystem.SendChatMessage(client.ChatSystem.AvailableGroupList[0], "HOLA a todos!!!");
                            }
                            break;
                        default:
                            break;
                    }
                }
            client.Release();
            Console.ReadLine();
        }

        static void client_UDPMessageArrived(object sender, KSPM.Network.Common.Messages.Message message)
        {
            KSPMGlobals.Globals.Log.WriteTo("LLEGO");
            KSPMGlobals.Globals.Log.WriteTo(message.ToString());
        }

        static void client_TCPMessageArrived(object sender, KSPM.Network.Common.Messages.Message message)
        {
            KSPMGlobals.Globals.Log.WriteTo(message.ToString());
        }

        static void client_UserDisconnected(object sender, KSPM.Network.Common.Events.KSPMEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        static void OnFloodTerminated(IAsyncResult result)
        {
            FloodAsync flooder = (FloodAsync)result.AsyncState;
            flooder.EndInvoke(result);
            Console.WriteLine("Stop flooding");
        }

        static void AsyncFlooder( ChatBot botReference, ChatBot.FloodMode floodingMode, int targetsIds, long delay)
        {
            Console.WriteLine("Starting to flood Mode[{0}] ; Delay[{1}] ; Targets[{2}", floodingMode, delay, targetsIds);
            while(Program.flooding)
            {
                botReference.Flood(floodingMode, targetsIds);
                System.Threading.Thread.Sleep((int)delay);
            }
        }
    }
}

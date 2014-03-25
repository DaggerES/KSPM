using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using KSPM.Network.Client;
using KSPM.Game;

namespace ConsoleFakeClient
{
    public class ChatBot
    {
        public static string[] Names = {
                                           "Ver'an",
                                           "Hon'ran",
                                           "Esttona",
                                           "Olero",
                                            "Hat-honkim",
                                            "Turskel",
                                            "Kimash",
                                            "Yhinu",
                                            "Yerther",
                                            "Enthlor",
                                            "Vesiss",
                                            "Turtaslye",
                                            "Jaeright",
                                            "Areald",
                                            "Angwor",
                                            "Umeale",
                                            "Taiem",
                                            "Che-rynray",
                                            "Untnyny",
                                            "Necum",
                                            "Sweedran",
                                            "Vermor",
                                            "Kel'ser",
                                            "Morust",
                                            "Burdelang",
                                            "Ildas",
                                            "Athia",
                                            "Old'echy",
                                            "Swyestther",
                                            "Loten"
                                       };

        public GameClient botClient;

        protected List<string> contentList;

        public ChatBot( GameClient client)
        {
            this.botClient = client;
            this.contentList = new List<string>();
        }

        public void GenerateRandomUser()
        {
            Random r = new Random();
            string userName = ChatBot.Names[r.Next(ChatBot.Names.Length)];
            byte[] utf8Bytes;
            UTF8Encoding utf8Encoder = new UTF8Encoding();
            utf8Bytes = utf8Encoder.GetBytes(userName);
            GameUser myUser = new GameUser(ref userName, ref utf8Bytes);
            this.botClient.SetGameUser(myUser);

        }

        public void InitFromFile(string fileName)
        {
            FileStream buffer = new FileStream(fileName, FileMode.Open);
            StreamReader reader = new StreamReader(buffer);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                contentList.Add(line);
            }
            reader.Close();
        }

        public void Flood()
        {
            Random r = new Random();
            if (this.botClient.ChatSystem != null)
            {
                this.botClient.ChatSystem.SendChatMessage(botClient.ChatSystem.AvailableGroupList[0], this.contentList[r.Next(this.contentList.Count)]);
            }
        }
    }
}

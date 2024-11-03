using log4net.Repository.Hierarchy;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Steamworks;
using Terraria.Net;
using Terraria.Chat;
using Terraria.Localization;
using Terraria.GameContent.UI.Chat;

namespace BotIntegration
{
	public class BotIntegration : Mod
	{
        public override void Load()
        {
            TCPServer TCPServer = new();
            TCPServer.Run();
        }
    }
    

    public class TCPServer
    {
        IPAddress _ipaddress;
        Int32 _port;
        TcpListener _listener;
        bool _listening;
        public void Run()
        {
            _port = 8400;
            _ipaddress = IPAddress.Parse("127.0.0.1");
            _listener = new(_ipaddress, _port);
            _listening = true;

            Thread listenThread = new(Listen);
            listenThread.Start();
        }
        public void Terminate()
        {
            _listening = false;
        } 

        private void Listen()
        {
            try
            {
                _listener.Start();

                while (_listening)
                {
                    TcpClient botClient = _listener.AcceptTcpClient();
                    Thread readerThread = new(() => Read(botClient));
                    readerThread.Start();
                }
                _listener.Stop();
                return;
            }
            catch (Exception ex)
            {
                Main.NewText("The following error has occurred at step Listen: " + ex.Message);
                _listener.Stop();
                return;
            }
        }

        private void Read(TcpClient client)
        {
            try
            {
                byte[] buffer = new byte[256];

                NetworkStream stream = client.GetStream();

                int num = stream.Read(buffer, 0, buffer.Length);
                string msgStr = Encoding.UTF8.GetString(buffer, 0, num);
                Message msg = JsonSerializer.Deserialize<Message>(msgStr);

                StringBuilder response = new();

                /*  Bot json format is as follows ------------
                 *  msg.Method: string -- Determines the "command" given 
                 *  msg.Author: string -- The disc user's name who invoked the command
                 *  msg.Params: null -- Not implemented
                 */
                if (msg == null || msg.Method == null || msg.Author == null)
                {
                    Main.NewText("Bad request");
                    return;
                }
                switch (msg.Method) 
                {
                    case "GetTime": //----------\\
                        response.Append("The time is currently: ");
                        response.Append(Utils.GetDayTimeAs24FloatStartingFromMidnight());

                        /*double time = Utils.GetDayTimeAs24FloatStartingFromMidnight();    Yeahhh fuck this 
                        float hour = (float)Math.Floor(time);
                        float minutes = (float)(time - Math.Truncate(time));
                        float mins = (float)Math.Floor(MathHelper.Lerp(0, 60, minutes));

                        if (time < 12)
                        {
                            response.Append(hour);
                            response.Append(":");
                            response.Append(minutes);
                            response.Append(" AM");
                        } else
                        {
                            response.Append(hour - 12);
                            response.Append(":");
                            response.Append(minutes);
                            response.Append(" PM");
                        }*/
                        break;
                    case "Ding":
                        ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral($"Server dinged by {msg.Author}!"), Color.RoyalBlue);
                        response.Append("Server dinged!");
                        break;
                    case "GetPlayers":
                        response.Append("The following players are connected:\n");
                        foreach (Player player in Main.player)
                        {
                            response.Append("   >");
                            response.Append(player.name);
                            response.Append('\n');
                        }
                        break;
                    default:
                        response.Append("Unknown Method");
                        break;
                }

                stream.Write(Encoding.UTF8.GetBytes(response.ToString()));

                return;
            }
            catch (Exception ex)
            {
                Main.NewText("The following error has occurred at step Read: " + ex.Message);
                return;
            }
        }

        private class Message
        {
            public string Method { get; set; }
            public string Author { get; set; }
            public string[] Params { get; set; }

        }
    }
}
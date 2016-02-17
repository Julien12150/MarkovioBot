using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Markov;
using Newtonsoft.Json;

namespace MarkovioBot
{
	class Program
	{
		public static DiscordClient client = new DiscordClient();

		public static string email;
		public static string password;

        public static ulong lastServer;
		public static List<Tuple<ulong, List<string>>> servers;

        public static bool shouldExit;


        public static void Main(string[] args)
		{
			Console.Write("Email: ");
			email = Console.ReadLine();
			Console.Write("Password: ");
			password = Console.ReadLine();
            Console.WriteLine();
            Console.Clear();


            client.MessageReceived += OnMessageReceived;
            client.LoggedIn += OnLoggedIn;
            client.GatewaySocket.Connected += OnConnected;
            client.GatewaySocket.Disconnected += OnDisconnected;

            servers = JsonConvert.DeserializeObject<List<Tuple<ulong, List<string>>>>(
				File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MarkovioBot\serverData.json")
			);

			if (servers == null)
			{
				servers = new List<Tuple<ulong, List<string>>>();
			}

			client.Connect(email, password);

			while (!shouldExit)
            {
                Console.Write(">");

                string command = Console.ReadLine().Trim();

                if (command.ToUpper().StartsWith("QUIT"))
                {
                    shouldExit = true;
                } else if (command.ToUpper().StartsWith("PROB") || command.ToUpper().StartsWith("FULLPROB"))
                {
                    List<string> commandArgs = Regex.Replace(command, @" + ", " ").Split(' ','\n','\r').Skip(1).ToList();

                    if (!commandArgs.Any())
                    {
                        Console.WriteLine("Not enough arguments.");
                    }
                    else
                    {
                        List<ulong> serversInList = servers.Select(t => t.Item1).ToList();
                        MarkovChain<string> chain = new MarkovChain<string>(1);
                        foreach (var server in servers) {
                            Server currentServer = client.GetServer(server.Item1);

                            foreach (var messageFromServer in servers[serversInList.IndexOf(server.Item1)].Item2)
                            {
                                chain.Add(messageFromServer.Split(' ', '\n', '\r'));
                            }

                            List<string> words = new List<string>();

                            words.Add(commandArgs.First());

                            try
                            {
                                float totalWeight = chain.GetNextStates(words).Sum(x => x.Value);

                                Dictionary<string, int> intChain = chain.GetNextStates(words);
                                Dictionary<string, float> finalChain = new Dictionary<string, float>();

                                foreach (var word in intChain.ToArray())
                                {
                                    finalChain.Add(word.Key, word.Value / totalWeight);
                                }

                                Console.WriteLine(totalWeight + " total entries in " + currentServer.Name);

                                if (command.ToUpper().StartsWith("PROB"))
                                {
                                    int count = finalChain.Count;
                                    foreach (var prob in finalChain.OrderBy(key => key.Value).Reverse())
                                    {
                                        if (prob.Value <= 0.05 && finalChain.Count - count >= 10)
                                        {
                                            Console.WriteLine("...and " + count + " more.");
                                            break;
                                        }
                                        Console.WriteLine(Math.Round((prob.Value * 100), 3) + "%: " + prob.Key);
                                        count--;
                                    }
                                }
                                else
                                {
                                    foreach (var prob in finalChain.OrderBy(key => key.Value).Reverse())
                                    {
                                        Console.WriteLine(Math.Round((prob.Value * 100), 3) + "%: " + prob.Key);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("No data from " + currentServer.Name);
                            }
                        }
                    }
                } else
                {
                    Console.WriteLine("Unrecognized.");
                }

                Console.WriteLine();
            }
		}

		private static void OnMessageReceived(object sender, MessageEventArgs e)
		{
            lastServer = e.Server.Id;

            List<ulong> serversInList = servers.Select(t => t.Item1).ToList();

            string rawMessage = Regex.Replace(e.Message.RawText, @" + ", " ");
            string formattedMessage = Regex.Replace(e.Message.Text, @" + ", " ");

            if (e.Channel.IsPrivate || e.User.Id == client.CurrentUser.Id)
            {
                return;
            }
            else if (rawMessage.StartsWith("<@" + client.CurrentUser.Id + ">"))
            {
                MarkovChain<string> chain = new MarkovChain<string>(1);
                foreach (var messageFromServer in servers[serversInList.IndexOf(e.Server.Id)].Item2)
                {
                    chain.Add(messageFromServer.Split(' ','\n','\r'));
                }

                if (rawMessage.Split(' ','\n','\r').Length > 1)
                {
                    try
                    {
                        List<string> words = new List<string>();
                        words.Add(rawMessage.Split(' ','\n','\r').Skip(1).First());

                        string chained = rawMessage.Split(' ', '\n', '\r').Skip(1).First() + " " + string.Join(" ", chain.Chain(words, new Random()));

                        e.Channel.SendMessage(chained);
                        client.SetGame(chained);
                    }
                    catch (ArgumentException) { }
                } else
                {
                    try
                    {
                        string chained = string.Join(" ", chain.Chain(new Random()));

                        e.Channel.SendMessage(chained);
                        client.SetGame(chained);
                    }
                    catch (ArgumentException) { }
                }
			} else
			{
				if (serversInList.Contains(e.Server.Id))
				{
					servers[serversInList.IndexOf(e.Server.Id)].Item2.Add(e.Message.Text);
				} else
				{
					servers.Add(new Tuple<ulong, List<string>>(e.Server.Id, new List<string>()));
					servers[0].Item2.Add(e.Message.Text);
				}
			}

			File.WriteAllText(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MarkovioBot\serverData.json",
				JsonConvert.SerializeObject(servers)
			);
        }

        private static void OnLoggedIn(object sender, EventArgs e)
        {
            Console.WriteLine("Logged in as " + client.CurrentUser.Name);
            Console.WriteLine();
            Console.Write(">");
        }

        private static void OnConnected(object sender, EventArgs e)
        {
            Console.Title = "MarkovioBot - Connected";
        }

        private static void OnDisconnected(object sender, EventArgs e)
        {
            Console.Title = "MarkovioBot - Disconnected";
        }
    }
}
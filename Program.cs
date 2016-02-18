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

		public static List<Tuple<ulong, List<string>>> servers;

        public static bool shouldExit;

        public static bool isLogging;

        public static void Main(string[] args)
		{
            Console.Title = "MarkovioBot - Disconnected";

            Console.Write("Email: ");
			email = Console.ReadLine();

			Console.Write("Password: ");
            Console.ForegroundColor = ConsoleColor.Black;
			password = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine();
            Console.Clear();

            isLogging = false;

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
                if (client.State == ConnectionState.Connected)
                {
                    Console.Write(">");

                    string command = Console.ReadLine().Trim();

                    if (command.ToUpper().StartsWith("QUIT"))
                    {
                        shouldExit = true;
                    }
                    /*
                    else if (command.ToUpper().StartsWith("LOG"))
                    {
                        isLogging = !isLogging;
                        Console.WriteLine(string.Format("Logging is now {0:enabled;0;disabled}.", Convert.ToInt32(isLogging)));
                    }
                    */
                    else if (command.ToUpper().StartsWith("GAME"))
                    {
                        string game = String.Join(" ", Regex.Replace(command, @" + ", " ").Split(' ', '\n', '\r').Skip(1));
                        client.SetGame(game);
                        Console.WriteLine("Game set to ``" + game + "``.");
                    }
                    else if (command.ToUpper().StartsWith("PROB") || command.ToUpper().StartsWith("FULLPROB"))
                    {
                        List<string> commandArgs = Regex.Replace(command, @" + ", " ").Split(' ', '\n', '\r').Skip(1).ToList();

                        List<ulong> serversInList = servers.Select(t => t.Item1).ToList();
                        foreach (var server in servers)
                        {
                            MarkovChain<string> chain = new MarkovChain<string>(1);
                            Server currentServer = client.GetServer(server.Item1);

                            foreach (var messageFromServer in servers[serversInList.IndexOf(server.Item1)].Item2)
                            {
                                chain.Add(messageFromServer.Split(' ', '\n', '\r'));
                            }

                            List<string> words = new List<string>();

                            if (commandArgs.Any())
                            {
                                words.Add(commandArgs.First());
                            }

                            try
                            {
                                float totalWeight;
                                Dictionary<string, int> intChain;

                                if (commandArgs.Any())
                                {
                                    totalWeight = chain.GetNextStates(words).Sum(x => x.Value);
                                }
                                else
                                {
                                    totalWeight = chain.GetInitialStates().Sum(x => x.Value);
                                }

                                if (commandArgs.Any())
                                {
                                    intChain = chain.GetNextStates(words);
                                }
                                else
                                {
                                    intChain = chain.GetInitialStates();
                                }

                                Dictionary<string, float> finalChain = new Dictionary<string, float>();

                                foreach (var word in intChain.ToArray())
                                {
                                    finalChain.Add(word.Key, word.Value / totalWeight);
                                }

                                Console.ForegroundColor = ConsoleColor.Cyan;

                                if (commandArgs.Any())
                                {
                                    Console.WriteLine("**" + totalWeight + " total entries in " + currentServer.Name + " for ``" + commandArgs.First() + "``.**");
                                }
                                else
                                {
                                    Console.WriteLine("**" + totalWeight + " total entries in " + currentServer.Name + " for the first word.** ``");
                                }
                                Console.ForegroundColor = ConsoleColor.Gray;

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
                                        Console.WriteLine("-" + Math.Round((prob.Value * 100), 3) + "%: ``" + prob.Key.Replace("`", "\\`") + "``");
                                        count--;
                                    }
                                }
                                else
                                {
                                    foreach (var prob in finalChain.OrderBy(key => key.Value).Reverse())
                                    {
                                        Console.WriteLine("-" + Math.Round((prob.Value * 100), 3) + "%: ``" + prob.Key.Replace("`", "\\`") + "``");
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.WriteLine("No data from " + currentServer.Name);
                                Console.ForegroundColor = ConsoleColor.Gray;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unrecognized command.");
                    }

                    Console.WriteLine();
                }
            }
		}

		private static void OnMessageReceived(object sender, MessageEventArgs e)
		{
            List<ulong> serversInList = servers.Select(t => t.Item1).ToList();

            string rawMessage = Regex.Replace(e.Message.RawText, @" + ", " ");
            string formattedMessage = Regex.Replace(e.Message.Text, @" + ", " ");

            if (e.User.Id == client.CurrentUser.Id)
            {
                return;
            } else if (e.Channel.IsPrivate)
            {
                string finalMessage = String.Empty;
                if (rawMessage.ToUpper().StartsWith("PROB") || rawMessage.ToUpper().StartsWith("FULLPROB"))
                {
                    List<string> commandArgs = Regex.Replace(rawMessage, @" + ", " ").Split(' ', '\n', '\r').Skip(1).ToList();

                    //List<ulong> mutualServers = servers.Select(t => client.GetServer(t.Item1)).ToList().Where(x => x.GetUser(e.User.Id) != null).Select(x => x.Id).ToList();
                    List<ulong> mutualServers = serversInList;

                    foreach (var server in servers)
                    {
                        if (client.GetServer(server.Item1).GetUser(e.User.Id) == null)
                        {
                            continue;
                        }

                        MarkovChain<string> chain = new MarkovChain<string>(1);
                        Server currentServer = client.GetServer(server.Item1);

                        foreach (var messageFromServer in servers[mutualServers.IndexOf(server.Item1)].Item2)
                        {
                            chain.Add(messageFromServer.Split(' ', '\n', '\r'));
                        }

                        List<string> words = new List<string>();

                        if (commandArgs.Any())
                        {
                            words.Add(commandArgs.First());
                        }

                        try
                        {
                            float totalWeight;
                            Dictionary<string, int> intChain;

                            if (commandArgs.Any())
                            {
                                totalWeight = chain.GetNextStates(words).Sum(x => x.Value);
                            }
                            else
                            {
                                totalWeight = chain.GetInitialStates().Sum(x => x.Value);
                            }

                            if (commandArgs.Any())
                            {
                                intChain = chain.GetNextStates(words);
                            }
                            else
                            {
                                intChain = chain.GetInitialStates();
                            }

                            Dictionary<string, float> finalChain = new Dictionary<string, float>();

                            foreach (var word in intChain.ToArray())
                            {
                                finalChain.Add(word.Key, word.Value / totalWeight);
                            }

                            if (commandArgs.Any())
                            {
                                finalMessage += "**" + totalWeight + " total entries in " + currentServer.Name + " for ``" + commandArgs.First() + "``.**\n";
                            }
                            else
                            {
                                finalMessage += "**" + totalWeight + " total entries in " + currentServer.Name + " for the first word.**\n";
                            }

                            if (rawMessage.ToUpper().StartsWith("PROB"))
                            {
                                int count = finalChain.Count;
                                foreach (var prob in finalChain.OrderBy(key => key.Value).Reverse())
                                {
                                    if (prob.Value <= 0.05 && finalChain.Count - count >= 10)
                                    {
                                        finalMessage += "...and " + count + " more.\n";
                                        break;
                                    }
                                    finalMessage += "-" + Math.Round((prob.Value * 100), 3) + "%: ``" + prob.Key.Replace("`", "\\`") + "``\n";
                                    count--;
                                }
                            }
                            else
                            {
                                foreach (var prob in finalChain.OrderBy(key => key.Value).Reverse())
                                {
                                    finalMessage += "-" + Math.Round((prob.Value * 100), 3) + "%: ``" + prob.Key.Replace("`", "\\`") + "``\n";
                                }
                            }
                        }
                        catch (Exception)
                        {
                            finalMessage += "No data from " + currentServer.Name + "\n";
                        }
                    }
                }
                else
                {
                    finalMessage += "Unrecognized command.";
                }

                try
                {
                    e.Channel.SendMessage(finalMessage);
                }
                catch (ArgumentOutOfRangeException)
                {
                    e.Channel.SendMessage("The data is too long. Sending file...");

                    MemoryStream stream = new MemoryStream();
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write(finalMessage.Replace("\n", Environment.NewLine));
                    writer.Flush();
                    stream.Position = 0;
                    e.Channel.SendFile("data.txt", stream);
                    writer.Close();
                    stream.Close();
                }
                
            }
            else if (rawMessage.StartsWith("<@" + client.CurrentUser.Id + ">"))
            {
                if (isLogging)
                {
                    Console.WriteLine(String.Format("User {0} <{1}> in {2} <{3}> on {4} said:",
                        e.User.Name,
                        e.User.Id,
                        e.Server.Name,
                        e.Server.Id,
                        e.Message.Timestamp.ToString("F")));
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(e.Message.RawText);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine();
                }

                MarkovChain<string> chain = new MarkovChain<string>(1);
                foreach (var messageFromServer in servers[serversInList.IndexOf(e.Server.Id)].Item2)
                {
                    chain.Add(messageFromServer.Split(' ', '\n', '\r'));
                }

                if (rawMessage.Split(' ', '\n', '\r').Length > 1)
                {
                    try
                    {
                        List<string> words = new List<string>();
                        words.Add(rawMessage.Split(' ', '\n', '\r').Skip(1).First());

                        string chained = rawMessage.Split(' ', '\n', '\r').Skip(1).First() + " " + string.Join(" ", chain.Chain(words, new Random()));

                        e.Channel.SendMessage(chained);
                    }
                    catch (ArgumentException) { }
                } else
                {
                    try
                    {
                        string chained = string.Join(" ", chain.Chain(new Random()));

                        e.Channel.SendMessage(chained);
                    }
                    catch (ArgumentException) { }
                }
            } else
            {
                if (!rawMessage.Contains("``")) {
                    if (serversInList.Contains(e.Server.Id))
                    {
                        servers[serversInList.IndexOf(e.Server.Id)].Item2.Add(e.Message.Text);
                    } else
                    {
                        servers.Add(new Tuple<ulong, List<string>>(e.Server.Id, new List<string>()));
                        servers[0].Item2.Add(e.Message.Text);
                    }
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
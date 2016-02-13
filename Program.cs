using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public static void Main(string[] args)
		{
			Console.Write("Email: ");
			email = Console.ReadLine();
			Console.Write("Password: ");
			password = Console.ReadLine();

			client.MessageReceived += OnMessageReceived;

			servers = JsonConvert.DeserializeObject<List<Tuple<ulong, List<string>>>>(
				File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MarkovioBot\serverData.json")
			);

			if (servers == null)
			{
				servers = new List<Tuple<ulong, List<string>>>();
			}

			client.Connect(email, password);

			while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
			Environment.Exit(0);
		}

		private static void OnMessageReceived(object sender, MessageEventArgs e)
		{
			List<ulong> serversInList = servers.Select(t => t.Item1).ToList();

			if (e.Channel.IsPrivate || e.User.Id == client.CurrentUser.Id)
			{
				return;
			}
			else if (e.Message.RawText.StartsWith("<@" + client.CurrentUser.Id + ">"))
			{
				MarkovChain<string> chain = new MarkovChain<string>(1);
				foreach (var messageFromServer in servers[serversInList.IndexOf(e.Server.Id)].Item2)
				{
					chain.Add(messageFromServer.Split(' '));
				}

				//client.SendMessage(e.Channel, string.Join(" ", chain.Chain(new Random())));
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
	}
}
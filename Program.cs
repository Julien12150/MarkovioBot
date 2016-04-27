using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using Discord;
using Discord.Audio;
using Markov;
using Newtonsoft.Json;
using Discord.API.Client.Rest;

namespace MarkovioBot {
	class Program {
		public static DiscordClient client = new DiscordClient();

		public static string appData;

		public static string token;
		public static List<Tuple<ulong, List<string>>> servers;

		public static SpeechSynthesizer synth;

		public static bool shouldExit;

		public static void Main(string[] args) {
			Console.Title = "MarkovioBot";

			for (int i = 0; i < args.Length; i++) {
				if (args[i].Equals("-token") && args[i + 1] != null) {
					token = args[i + 1];
                }
			}
			  
			if (token == null) {
				Console.Write("Token: ");
				token = Console.ReadLine();
			}

			Console.Clear();

			client.MessageReceived += OnMessageReceived;
			client.LoggedIn += (s, e) => Log(String.Format("Logged in as {0}.", client.CurrentUser.Name));
			client.GatewaySocket.Connected += (s, e) => Log("Connected.");
			client.GatewaySocket.Disconnected += (s, e) => Log("Disconnected.");

			appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\PoyoBots\MarkovioBot\";

			servers = JsonConvert.DeserializeObject<List<Tuple<ulong, List<string>>>>(
				File.ReadAllText(appData + "serverData.json")
			);

			if (servers == null) {
				servers = new List<Tuple<ulong, List<string>>>();
			}

			synth = new SpeechSynthesizer();
			synth.Rate = 1;

			try {
				synth.SelectVoice("Microsoft Haruka Desktop");
			} catch (Exception) { }


			client.UsingAudio(x => {
				x.Mode = AudioMode.Outgoing;
				x.Bitrate = 64;
				x.BufferLength = 10000;
			});

			client.Connect(token);

			while (!shouldExit) { }
		}

		private static void OnMessageReceived(object sender, MessageEventArgs e) {
			List<ulong> serversInList = servers.Select(t => t.Item1).ToList();

			string rawMessage = Regex.Replace(e.Message.RawText, @" + ", " ");
			string formattedMessage = Regex.Replace(e.Message.Text, @" + ", " ");

			if (e.User.Id == client.CurrentUser.Id ||
				e.Channel.IsPrivate) {
				return;
			} else if (rawMessage.StartsWith("<@" + client.CurrentUser.Id + ">") &&
				   (e.Channel.Id == 151532383663947777 ||
				   e.Channel.Id == 153663183045787649)) {
				MarkovChain<string> chain = new MarkovChain<string>(1);
				foreach (var messageFromServer in servers[serversInList.IndexOf(e.Server.Id)].Item2) {
					chain.Add(messageFromServer.Split(' ', '\n', '\r'));
				}

				if (rawMessage.Split(' ', '\n', '\r').Length > 1) {
					try {
						List<string> words = new List<string>();
						words.Add(rawMessage.Split(' ', '\n', '\r').Skip(1).First());

						string chained = rawMessage.Split(' ', '\n', '\r').Skip(1).First() + " " + string.Join(" ", chain.Chain(words, new Random()));

						SendMessage(chained, e);
					} catch (ArgumentException) { }
				} else {
					try {
						string chained = string.Join(" ", chain.Chain(new Random()));

						SendMessage(chained, e);
					} catch (ArgumentException) { }
				}
			} else {
				if (!rawMessage.Contains("``") &&
					!rawMessage.StartsWith("<@") &&
					formattedMessage.Trim() != String.Empty &&
					e.Channel.Id != 151532383663947777 &&
					e.User.Id != 151545405614587904) {
					if (!serversInList.Contains(e.Server.Id)) {
						servers.Add(new Tuple<ulong, List<string>>(e.Server.Id, new List<string>()));
					}
					servers[serversInList.IndexOf(e.Server.Id)].Item2.Add(e.Message.Text);
				}
			}

			File.WriteAllText(
				appData + "serverData.json",
				JsonConvert.SerializeObject(servers)
			);
		}

		private static async void SendMessage(string message, MessageEventArgs e) {
			await e.Channel.SendMessage(message);

			if (e.User.VoiceChannel != null) {
				try {
					IAudioClient audioClient = await e.User.VoiceChannel.JoinAudio();

					MemoryStream memoryStream = new MemoryStream();

					synth.SetOutputToAudioStream(memoryStream,
						new SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo));

					synth.Speak(message);

					memoryStream.Position = 0;
					memoryStream.CopyTo(audioClient.OutputStream);
					memoryStream.Dispose();
				} catch { }
			}
		}

		private static void Log(object message, bool warning = false) {
			DateTime time = DateTime.Now;
			if (warning) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("[WARN ");
			} else {
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write("[INFO ");
			}

			Console.Write(time.ToString("HH:mm:ss dd/MM/yyyy"));
			Console.Write("] ");
			Console.ResetColor();

			Console.WriteLine(message.ToString());
		}
	}
}
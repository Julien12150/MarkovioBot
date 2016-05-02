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
	enum MessageType {
		Info,
		Warning,
		Error
	}

	class Program {
		public static DiscordClient client = new DiscordClient();

		public static string appData;

		public static string token;

		public static List<DiscordServer> servers;
		public static List<DiscordUser> users;

		public static SpeechSynthesizer synth;

		public static bool shouldExit;

		public static void Main(string[] args) {
			Console.Title = "MarkovioBot";

			appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\PoyoBots\MarkovioBot\";
			if (!Directory.Exists(appData)) {
				Directory.CreateDirectory(appData);
			}

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
			client.LoggedIn += (s, e) => {
				Log(String.Format("Logged in as {0}.", client.CurrentUser.Name));
				Log(String.Format("Current voice is {0}.", synth.Voice.Name));
			};
			client.GatewaySocket.Connected += (s, e) => Log("Connected.");
			client.GatewaySocket.Disconnected += (s, e) => Log("Disconnected.", MessageType.Warning);
			
			servers = new List<DiscordServer>();

			foreach (String file in Directory.GetFiles(appData)) {
				string fileText = File.ReadAllText(file);
				DiscordServer deserializedServer = JsonConvert.DeserializeObject<DiscordServer>(fileText);
				servers.Add(deserializedServer);
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
			if (e.User.Id == client.CurrentUser.Id) {
				return;
			}

			if (e.Channel.IsPrivate) {
				e.Channel.SendMessage("I'm MarkovioBot. I'm the digital manifestation of a dumpster. :put_litter_in_its_place:");
				return;
			} else {
				if (!servers.Where(x => x.Id == e.Server.Id).Any()) {
					servers.Add(new DiscordServer {
						Id = e.Server.Id,
						Inputs = new List<string>()
					});
				}
			}

			if (!e.Message.Channel.Topic.ToUpper().Contains("[MARKOVIONOREAD]")) {
				servers.Where(x => x.Id == e.Server.Id).First()
					.Inputs
					.Add(e.Message.Text);
			}

			if (e.Message.RawText.StartsWith("<@" + client.CurrentUser.Id + ">")) {
				if (!e.Message.Channel.Topic.ToUpper().Contains("[MARKOVIONOSPEAK]")) {
					MarkovChain<string> chain = new MarkovChain<string>(1);

					foreach (string input in servers.Where(x => x.Id == e.Server.Id).First().Inputs) {
						chain.Add(
							Regex.Replace(input, @"\s+", " ").Split(' ')
						);
					}

					SendMessage(String.Join(" ", chain.Chain()), e);
				}
			}

			foreach (DiscordServer server in servers) {
				File.WriteAllText(
					appData + "/" + server.Id + ".json",
					JsonConvert.SerializeObject(server)
				);
			}
		}

		private static async void SendMessage(string message, MessageEventArgs e) {
			await e.Channel.SendMessage(message);

			try {
				IAudioClient audioClient = await e.User.VoiceChannel.JoinAudio();

				MemoryStream memoryStream = new MemoryStream();

				synth.SetOutputToAudioStream(memoryStream,
					new SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo));

				synth.Speak(message);

				memoryStream.Position = 0;
				memoryStream.CopyTo(audioClient.OutputStream);
				memoryStream.Dispose();

			} catch (Exception error) {
				Log(error.Message, MessageType.Error);
			}
		}

		private static void Log(object message, MessageType type = MessageType.Info) {
			DateTime time = DateTime.Now;
			if (type == MessageType.Info) {
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write("[INFO ");
			} else if (type == MessageType.Warning) {
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("[WARN ");
			} else if (type == MessageType.Error) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("[ERRR ");
			}

			Console.Write(time.ToString("HH:mm:ss dd/MM/yyyy"));
			Console.Write("] ");
			Console.ResetColor();

			Console.WriteLine(message.ToString());
		}
	}
}
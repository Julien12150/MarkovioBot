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

			appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\PoyoBots\MarkovioBot";
			if (!Directory.Exists(appData + @"\Server Data")) {
				Directory.CreateDirectory(appData + @"\Server Data");
			}
			if (!Directory.Exists(appData + @"\User Data")) {
				Directory.CreateDirectory(appData + @"\User Data");
			}
			if (!Directory.Exists(appData + @"\Backups")) {
				Directory.CreateDirectory(appData + @"\Backups");
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
			users = new List<DiscordUser>();

			foreach (String file in Directory.GetFiles(appData + @"\Server Data")) {
				string fileText = File.ReadAllText(file);
				DiscordServer deserializedServer = JsonConvert.DeserializeObject<DiscordServer>(fileText);
				servers.Add(deserializedServer);
			}

			foreach (String file in Directory.GetFiles(appData + @"\User Data")) {
				string fileText = File.ReadAllText(file);
				DiscordUser deserializedUser = JsonConvert.DeserializeObject<DiscordUser>(fileText);
				users.Add(deserializedUser);
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

			while (!shouldExit) {
				DateTime time = DateTime.Now;
				bool hasCompleted = false;
				
                try {
					hasCompleted = new DirectoryInfo(appData + @"\Backups\")
						.GetDirectories()
						.OrderByDescending(x => x.CreationTime)
						.First()
						.CreationTime > DateTime.Now.AddDays(-1);
				} catch (InvalidOperationException) {}

				if (((time.DayOfWeek == DayOfWeek.Tuesday) ||
					(time.DayOfWeek == DayOfWeek.Saturday)) &&
					!hasCompleted) {
					Log("Backing up files...");

					string backupDirectory = appData + @"\Backups\" + time.ToString("dd-MM-yyyy");
					Directory.CreateDirectory(backupDirectory);
					Directory.CreateDirectory(backupDirectory + @"\Server Data");
					Directory.CreateDirectory(backupDirectory + @"\User Data");

					foreach (String file in Directory.GetFiles(appData + @"\Server Data")) {
						File.Copy(
							file,
							backupDirectory + @"\Server Data\" + Path.GetFileName(file)
						);
					}

					foreach (String file in Directory.GetFiles(appData + @"\User Data")) {
						File.Copy(
							file,
							backupDirectory + @"\User Data\" + Path.GetFileName(file)
						);

					}
					Log("Done.");
				}
			}
		}

		private static void OnMessageReceived(object sender, MessageEventArgs e) {
			if (e.User.Id == client.CurrentUser.Id) {
				return;
			}

			if (!users.Any(x => x.Id == e.User.Id)) {
				users.Add(new DiscordUser {
					Id = e.User.Id,
					LikesSpeech = true
				});
			}

			if (e.Channel.IsPrivate) {
				DiscordUser userItem = users.First(x => x.Id == e.User.Id);
				if (e.Message.Text.ToUpper().Contains("SHUT UP")) {
					if (userItem.LikesSpeech) {
						userItem.LikesSpeech = false;
						SendMessage(@"\*angrily becomes silent\*", e);
					} else {
						SendMessage(@"\*continues saying nothing angrily\*", e);
					}
				} else if (e.Message.Text.ToUpper().Contains("TALK TO ME")) {
					if (!userItem.LikesSpeech) {
						userItem.LikesSpeech = true;
						SendMessage(@"Freeeeeeedom!", e);
					} else {
						SendMessage(@"Ok.", e);
					}
				} else {
					SendMessage(
						"I'm MarkovioBot. I'm the digital manifestation of a dumpster. :put_litter_in_its_place:\n" +
						"If you'd like me to stop talking to you every single time you summon me, tell me to `shut up`.\n" +
						"If you want me to start talking again, say `talk to me`.",
					e);
				}
				return;
			} else {
				if (!servers.Any(x => x.Id == e.Server.Id)) {
					servers.Add(new DiscordServer {
						Id = e.Server.Id,
						Inputs = new List<string>()
					});
				}
			}

			if (!e.Message.Channel.Topic.ToUpper().Contains("[MARKOVIONOREAD]")) {
				servers.First(x => x.Id == e.Server.Id)
					.Inputs
					.Add(e.Message.Text);
			}

			if (e.Message.RawText.StartsWith("<@" + client.CurrentUser.Id + ">")) {
				if (!e.Message.Channel.Topic.ToUpper().Contains("[MARKOVIONOSPEAK]")) {
					MarkovChain<string> chain = new MarkovChain<string>(1);

					foreach (string input in servers.First(x => x.Id == e.Server.Id).Inputs) {
						chain.Add(
							Regex.Replace(input, @"\s+", " ").Split(' ')
						);
					}

					SendMessage(String.Join(" ", chain.Chain()), e);
				}
			}

			foreach (DiscordServer server in servers) {
				File.WriteAllText(
					appData + @"\Server Data\" + server.Id + ".json",
					JsonConvert.SerializeObject(server)
				);
			}

			foreach (DiscordUser user in users) {
				File.WriteAllText(
					appData + @"\User Data\" + user.Id + ".json",
					JsonConvert.SerializeObject(user)
				);
			}
		}

		private static async void SendMessage(string message, MessageEventArgs e) {
			try {
				await e.Channel.SendMessage(message);
			} catch (ArgumentException) {
				Log("Empty message...?", MessageType.Error);
			}

			if ((e.User.VoiceChannel != null) &&
				(!e.Channel.IsPrivate) &&
				(users.First(x => x.Id == e.User.Id).LikesSpeech)) {
				IAudioClient audioClient = await e.User.VoiceChannel.JoinAudio();

				MemoryStream memoryStream = new MemoryStream();

				synth.SetOutputToAudioStream(memoryStream,
					new SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo));

				synth.Speak(message);

				memoryStream.Position = 0;
				memoryStream.CopyTo(audioClient.OutputStream);
				memoryStream.Dispose();
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
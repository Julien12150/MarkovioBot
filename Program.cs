using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using Discord;
using Discord.Audio;
using Newtonsoft.Json;

//Version 3.3.0

namespace MarkovioBot {
	enum MessageType {
		Info,
		Warning,
		Error,
		Test
	}

	class Program {
		public static DiscordClient client = new DiscordClient();

		public static string appData;

		public static string token;

		public static List<DiscordServer> servers;
		public static List<DiscordUser> users;

		public static SpeechSynthesizer synth;

		public static bool shouldExit;

		public static string[] args;

		private static ulong currentUserId;
		private static ulong currentServerId;

		public static void Main(string[] cmdArgs) {
			args = cmdArgs;
			Console.Title = "MarkovioBot";

			appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\PoyoBots\MarkovioBot";

			for (int i = 0; i < args.Length; i++) {
				if (args[i].Equals("-token") && args[i + 1] != null) {
					token = args[i + 1];
				}

				if (args[i].Equals("-test") && args[i + 1] != null) {
					token = args[i + 1];
					appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\PoyoBots\MarkovioBotTest";
				}
			}

			if (token == null) {
				Console.Write("Token: ");
				token = Console.ReadLine();
			}

			Console.Clear();

			client.MessageReceived += OnMessageReceived;
			client.Ready += OnReady;
			client.GatewaySocket.Connected += OnConnected;

			client.GatewaySocket.Disconnected += OnDisconnected;

			client.JoinedServer += OnJoinedServer;

			servers = new List<DiscordServer>();
			users = new List<DiscordUser>();

			BackupHandler.Initialize();

			foreach (String file in Directory.GetFiles(appData + @"\Server Data")) {
				string fileText = File.ReadAllText(file);
				DiscordServer deserializedServer = JsonConvert.DeserializeObject<DiscordServer>(fileText);
				servers.Add(deserializedServer);
			}

			foreach (String file in Directory.GetFiles(appData + @"\User Data")) {
				string fileText = File.ReadAllText(file);
				DiscordUser deserializedUser = JsonConvert.DeserializeObject<DiscordUser>(fileText);
				if (deserializedUser != null) {
					users.Add(deserializedUser);
				} else {
					File.Delete(file);
				}
			}

			synth = new SpeechSynthesizer();
			synth.Rate = 1;

			try {
				synth.SelectVoice("Microsoft Haruka Desktop");
			} catch (Exception) { }

			AudioServiceConfigBuilder config = new AudioServiceConfigBuilder();

			config.Mode = AudioMode.Outgoing;
			config.Bitrate = 64;
			config.BufferLength = 10000;

			client.UsingAudio(config.Build());

			client.Connect(token);

			CurrentGameHandler.Initialize();

			while (!shouldExit) {
				bool hasBackedUp = false;

				try {
					hasBackedUp = new DirectoryInfo(appData + @"\Backups\")
						.GetDirectories()
						.OrderByDescending(SortFiles)
						.First()
						.CreationTime > DateTime.Now.AddDays(-2);
				} catch (InvalidOperationException) { }

				if (!hasBackedUp) {
					BackupHandler.Backup();
				}
			}
		}

		private static void OnMessageReceived(object sender, MessageEventArgs e) {
			currentUserId = e.User.Id;
			currentServerId = e.Server.Id;

			if (e.User.Id == client.CurrentUser.Id || e.User.IsBot) {
				return;
			}

			if (!users.Any(CompareIds)) {
				users.Add(new DiscordUser {
					Id = e.User.Id,
					LikesSpeech = true
				});
			}

			if (e.Channel.IsPrivate) {
				Log(String.Format(
					"Receieved DM from {0}: \"{1}\"",
					e.User.Name,
					e.Message.Text
				));

				DiscordUser userItem = users.First(CompareIds);
				string command = e.Message.Text.ToUpper();

				switch (CommandHandler.ParseCommand(e.Message.Text)) {
					case Command.NewGame:
						if (CurrentGameHandler.LastGameTime > DateTime.Now.AddMinutes(-1)) {
							SendMessage(@"...I just started this one.", e);
						} else {
							CurrentGameHandler.ChangeGame();
							CurrentGameHandler.GameTimer.Stop();
							CurrentGameHandler.GameTimer.Start();

							Log(String.Format("Game changed by {0} to '{1}'.", e.User.Name, client.CurrentGame.Name));
						}
						break;

					case Command.ShouldSpeak:
						if (!userItem.LikesSpeech) {
							userItem.LikesSpeech = true;
							SendMessage(@"Freeeeeeedom!", e);
						} else {
							SendMessage(@"Ok.", e);
						}
						break;

					case Command.ShouldntSpeak:
						if (userItem.LikesSpeech) {
							userItem.LikesSpeech = false;
							SendMessage(@"\*angrily says nothing\*", e);
						} else {
							SendMessage(@"\*continues saying nothing angrily\*", e);
						}
						break;

					case Command.None:
						SendMessage(
							"I'm Markovio. I'm the digital manifestation of a dumpster. :put_litter_in_its_place:\n" +
							"\n" +
							"If you'd like me to stop talking to you every single time you summon me, tell me to `shut up`.\n" +
							"If you want me to start talking again, say `talk to me`.\n" +
							"If you think I should, you can tell me to `play another game`.\n" +
							"\n" +
							"Do you want me in your server? Just click this link:\n" +
							"https://discordapp.com/oauth2/authorize?client_id=170089741130268672&scope=bot&permissions=36801536\n" +
							"Put `[MKS]` in a channel topic to allow me to speak, and `[MKR]` to allow me to read.\n" +
							"`[MKRS]` is a shorthand for both of these.\n" +
							"These are case-insensitive.\n" +
							"\n" +
							"For more info, you can join this Discord server dedicated to Markovio here:\n" +
							"https://discord.gg/014zZkPs5vVrrth0I",
						e);
						break;
				}
				return;
			}

			if (!servers.Any(CompareIds)) {
				servers.Add(new DiscordServer {
					Id = e.Server.Id,
					Inputs = new List<string>()
				});
			}

			DiscordServer currentServer = servers.First(CompareIds);

			foreach (var server in servers) {
				if (!currentServer.Initialized) {
					currentServer.Initialize();
				}
			}

			Permission markovioPermissions;

			if (e.Message.Channel.Topic == null) {
				return;
			} else {
				markovioPermissions = TopicHandler.ParseTopic(e.Message.Channel.Topic);
			}

			Match markovioMatch = Regex.Match(e.Message.Text, "`[^`]*`");

			MarkovArguments markovioArguments = MarkovArgumentHandler.ParseArguments(e.Message.Text.Substring(markovioMatch.Index, markovioMatch.Length));
			
			if (markovioPermissions.CanRead && !(e.Message.IsMentioningMe())) {
				if (e.Message.Text.Trim() != String.Empty) {
					servers.First(CompareIds)
						.Add(e.Message.Text.Trim());
				}
			}

			if (e.Message.RawText.StartsWith("<@" + client.CurrentUser.Id + ">") || e.Message.RawText.StartsWith("<@!" + client.CurrentUser.Id + ">")) {
				if (markovioPermissions.CanSpeak) {
					SendMessage(servers.First(CompareIds).Chain(markovioArguments), e);
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

		private static void OnReady(object sender, EventArgs e) {
			Log(String.Format("Logged in as {0}.", client.CurrentUser.Name));
			Log(String.Format("Current voice is {0}.", synth.Voice.Name));
			if (args.Contains("-voicelist")) {
				foreach (var voice in synth.GetInstalledVoices()) {
					Log(String.Format("Found possible voice {0}.", voice.VoiceInfo.Name));
				}
			}
		}

		private static void OnConnected(object sender, EventArgs e) {
			Log("Connected.");
		}

		private static void OnDisconnected(object sender, EventArgs e) {
			Log("Disconnected.", MessageType.Warning);
		}

		private static void OnJoinedServer(object sender, ServerEventArgs e) {
			e.Server.Owner.SendMessage(
					"I'm Markovio. I'm the digital manifestation of a dumpster. :put_litter_in_its_place:\n" +
					"It looks like I've been added to your server, " + e.Server.Name + "!\n" +
					"Thanks for having me.\n" +
					"\n" +
					"If you'd like me to stop talking to you every single time you summon me, tell me to `shut up`.\n" +
					"If you want me to start talking again, say `talk to me`.\n" +
					"If you think I should, you can tell me to `play another game`.\n" +
					"\n" +
					"Put `[MKS]` in a channel topic to allow me to speak, and `[MKR]` to allow me to read.\n" +
					"`[MKRS]` is a shorthand for both of these.\n" +
					"These are case-insensitive.\n" +
					"\n" +
					"For more info, you can join this Discord server dedicated to Markovio here:\n" +
					"https://discord.gg/014zZkPs5vVrrth0I"
				);
		}

		private static async void SendMessage(string message, MessageEventArgs e) {
			try {
				await e.Channel.SendMessage(message);
			} catch (ArgumentException) {
				Log(String.Format("Empty message from server {0}.", e.Server.Name), MessageType.Error);
			}

			try {
				if ((e.User.VoiceChannel != null) &&
					(!e.Channel.IsPrivate) &&
					(users.First(CompareIds).LikesSpeech)) {
					IAudioClient audioClient = await e.User.VoiceChannel.JoinAudio();

					MemoryStream memoryStream = new MemoryStream();

					synth.SetOutputToAudioStream(memoryStream,
						new SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo));

					synth.Speak(message);

					memoryStream.Position = 0;
					memoryStream.CopyTo(audioClient.OutputStream);
					memoryStream.Dispose();
				}
			} catch (TimeoutException) {
				Log("Timeout.", MessageType.Error);
			} catch (OperationCanceledException) {
				Log("Tried to speak while disconnected.", MessageType.Error);
			} catch (NullReferenceException) {
				Log("Null reference while speaking.", MessageType.Error);
			} catch (InvalidOperationException) {
				Log("Invalid operation while speaking.", MessageType.Error);
			}
		}

		public static void Log(object message, MessageType type = MessageType.Info) {
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
			} else if (type == MessageType.Test) {
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write("[TEST ");
			}

			Console.Write(time.ToString("HH:mm:ss dd/MM/yyyy"));
			Console.Write("] ");
			Console.ResetColor();
			try {
				Console.WriteLine(message.ToString());
			} catch (NullReferenceException) {
				Console.WriteLine("NULL");
			}
		}

		private static DateTime SortFiles(DirectoryInfo file) {
			return file.CreationTime;
		}

		private static bool CompareIds(DiscordUser user) {
			return user.Id == currentUserId;
		}

		private static bool CompareIds(DiscordServer server) {
			return server.Id == currentServerId;
		}
	}
}
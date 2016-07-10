using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using MarkovioBot.Markov;
using Newtonsoft.Json;

namespace MarkovioBot {
	static class CurrentGameHandler {
		public static DateTime LastGameTime = DateTime.MinValue;

		private static SteamAppList apps;
		private static MarkovChain<string> gameChain = new MarkovChain<string>(1);

		public static Timer GameTimer = new Timer(60 * 5 * 1000);

		public static void Initialize() {
			using (var webClient = new System.Net.WebClient()) {
				apps = JsonConvert.DeserializeObject<SteamAppList>(
					webClient.DownloadString("http://api.steampowered.com/ISteamApps/GetAppList/v0001/")
				);

				foreach (string app in apps.AppList.Apps.App.Select(ReturnName)) {
					gameChain.Add(
						Regex.Replace(app, @"\s+", " ").Split(' ')
					);
				}
			}

			GameTimer.Elapsed += ChangeGame;
			GameTimer.Start();
		}

		public static void ChangeGame(object source = null, ElapsedEventArgs e = null) {
			try {
				string game = String.Join(" ", gameChain.Chain());
				Program.client.SetGame(game);
			} catch (NullReferenceException) { }

			try {
				LastGameTime = e.SignalTime;
			} catch (NullReferenceException) { }
		}

		public static string ReturnName(App app) {
			return app.Name;
		}
	}
}

using System.Collections.Generic;
using Newtonsoft.Json;

namespace MarkovioBot {
	public class App {
		[JsonProperty("appid")]
		public int AppId { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }
	}

	public class Apps {
		[JsonProperty("app")]
		public List<App> App { get; set; }
	}

	public class AppList {
		[JsonProperty("apps")]
		public Apps Apps { get; set; }
	}

	public class SteamAppList {
		[JsonProperty("applist")]
		public AppList AppList { get; set; }
	}
}

using Newtonsoft.Json;

namespace MarkovioBot {
	class DiscordUser {
		[JsonProperty("id")]
		public ulong Id;

		[JsonProperty("likesSpeech")]
		public bool LikesSpeech = true;
	}
}
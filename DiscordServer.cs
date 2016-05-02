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
	class DiscordServer {
		[JsonProperty("id")]
		public ulong Id;

		[JsonProperty("inputs")]
		public List<String> Inputs;
	}
}

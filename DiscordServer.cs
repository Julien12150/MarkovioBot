using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MarkovioBot.Markov;
using Newtonsoft.Json;

namespace MarkovioBot {
	class DiscordServer {
		[JsonProperty("id")]
		public ulong Id;

		[JsonProperty("inputs")]
		public List<string> Inputs;

		[JsonIgnore]
		public bool Initializing = false;

		[JsonIgnore]
		public bool Initialized = false;

		[JsonIgnore]
		public MarkovChain<string> WordChain = new MarkovChain<string>(2);
		[JsonIgnore]
		public MarkovChain<string> LetterChain = new MarkovChain<string>(5);

		public void Initialize(bool filter = false) {
			Initializing = true;
			Initialized = true;

			try {
				Program.Log(String.Format("Initializing server {0} ({1}), with input count {2}", Program.client.GetServer(Id).Name, Id, Inputs.Count));
				if (Inputs == null) {
					Inputs = new List<string>();
				}

				foreach (string input in Inputs) {
					if (filter) {
						if (!String.IsNullOrWhiteSpace(input) && input.Contains("@Markovio")) {
							Add(input, true);
						}
					} else {
						Add(input, true);
					}
				}
			} catch (NullReferenceException) {
				Program.Log(String.Format("Error initializing server {0} ({1})", Program.client.GetServer(Id).Name, Id), MessageType.Error);
			}
			Initializing = false;
			Program.Log(String.Format("Finished initializing server {0} ({1})", Program.client.GetServer(Id).Name, Id, Inputs.Count));
		}

		public void Add(string text, bool initial = false) {
			if (Initializing && !initial) {
				return;
			}

			if (!initial) {
				Inputs.Add(text);
			}

			WordChain.Add(Regex.Replace(text, @"\s+", " ").Split(' '));
			LetterChain.Add(
			Regex.Replace(text, @"\s+", " ").ToCharArray().Select(Char.ToString));
		}

		public string Chain(MarkovArguments markovioArguments) {
			return Chain(markovioArguments.ChainLength, markovioArguments.UseLetters, markovioArguments.Seed);
		}

		public string Chain(int chainLength, bool useLetters, string seed = null) {
			if (useLetters) {
				LetterChain.Order = chainLength;
				return String.Join(String.Empty, LetterChain.Chain());
			} else {
				WordChain.Order = chainLength;
				if (seed == null) {
					return String.Join(" ", WordChain.Chain());
				} else {
					return seed + " " + String.Join(" ", WordChain.Chain(new List<string> { seed }));
				}
			}
		}
	}
}

using System;
using System.Linq;

namespace MarkovioBot {
	struct MarkovArguments {
		public int ChainLength;
		public bool UseLetters;
		public string Seed;
	}

	static class MarkovArgumentHandler {
		public static MarkovArguments ParseArguments(string text) {
			int chainLength = 2;
			bool useLetters = false;
			string seed = null;

			string parsedText = text.ToUpper();
			
			if (parsedText.Contains("L")) {
				useLetters = true;
			}

			if (parsedText.Contains("C")) {
				Int32.TryParse(parsedText.ElementAt(parsedText.IndexOf("C") + 1).ToString(), out chainLength);
			} else {
				if (useLetters) {
					chainLength = 5;
				}
			}

			if (parsedText.Contains("WW")) {
				seed = "CryWolf";
			}

			return new MarkovArguments {
				ChainLength = chainLength,
				UseLetters = useLetters,
				Seed = seed
			};
		}
	}
}

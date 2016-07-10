using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MarkovioBot {
	struct Permission {
		public bool CanRead;
		public bool CanSpeak;
	}

	static class TopicHandler {
		public static Permission ParseTopic(string text) {
			string parsedText = text.ToUpper();
			if (parsedText.Contains("[MKRS]") || (parsedText.Contains("[MKR]") && parsedText.Contains("[MKS]"))) {
				return new Permission() {
					CanRead = true,
					CanSpeak = true
				};
			} else {
				if (parsedText.Contains("[MKR]")) {
					return new Permission() {
						CanRead = true
					};
				}

				if (parsedText.Contains("[MKS]")) {
					return new Permission() {
						CanSpeak = true
					};
				}
			}

			return new Permission();
		}
	}
}

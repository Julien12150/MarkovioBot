using System;
using System.Text.RegularExpressions;

namespace MarkovioBot {
    enum Command {
        NewGame,
        ShouldSpeak,
        ShouldntSpeak,
        None
    }

	static class CommandHandler {
        public static Command ParseCommand(string text) {
            string parsedText = text.ToUpper();

            if (parsedText.Contains("PLAY") && parsedText.Contains("GAME")) {
                return Command.NewGame;
            } else if ((parsedText.Contains("TALK") || parsedText.Contains("SPEAK")) && !parsedText.Contains("DONT")) {
                return Command.ShouldntSpeak;
            } else if (((parsedText.Contains("TALK") || parsedText.Contains("SPEAK")) && parsedText.Contains("DONT")) || (parsedText.Contains("SHUT") && parsedText.Contains("UP"))) {
                return Command.ShouldSpeak;
            } else {
                return Command.None;
            }
        }
    }
}

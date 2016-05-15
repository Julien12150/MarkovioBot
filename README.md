# MarkovioBot

It's a Discord bot that uses Markov chains to emulate the users of a Discord server. Very shoddily written, it's due for another rewrite. Soon. Someday.

It supports command-line arguments:

```MarkovioBot.exe -token <token>```

It writes to `%appdata%/PoyoBots/MarkovioBot`.

To invite it to your server, click [this link](https://discordapp.com/oauth2/authorize?client_id=170089741130268672&scope=bot&permissions=36801536).

Put `[MKS]` in a channel topic to allow it to speak, and `[MKR]` to allow it to read. `[MKRS]` is a shorthand for both of these. These are case-insensitive.

It will speak to users who summon it if they are in a voice channel. DM it to see specific commands they may use to disable this. You can simply disable the permission to join voice channels if users abuse it.

I personally recommend instating a rule to disallow feeding the bot copypastas, memes, etc., but it's up to your discretion.

It also uses Steam's API to grab a list of every game in the Steam library, then Markov-chains that as well.

It will make backups every two days, in case of corruption. They are not used by the bot, so feel free to delete them or disable making them.

The bot is run on my home computer, so 100% uptime is not guaranteed. Feel free to run your own copy, or modify for your personal use.

# TODO:

* Better command-line support, more arguments.
* Better code organization, don't keep everything in one file.
* More elegant permission-handling.

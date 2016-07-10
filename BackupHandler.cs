using System;
using System.IO;

namespace MarkovioBot {
	static class BackupHandler {
		static public void Initialize() {
			if (!Directory.Exists(Program.appData + @"\Server Data")) {
				Directory.CreateDirectory(Program.appData + @"\Server Data");
			}
			if (!Directory.Exists(Program.appData + @"\User Data")) {
				Directory.CreateDirectory(Program.appData + @"\User Data");
			}
			if (!Directory.Exists(Program.appData + @"\Backups")) {
				Directory.CreateDirectory(Program.appData + @"\Backups");
			}
		}

		static public void Backup() {
			Program.Log("Backing up files...");

			string backupDirectory = Program.appData + @"\Backups\" + DateTime.Now.ToString("dd-MM-yyyy");
			Directory.CreateDirectory(backupDirectory);
			Directory.CreateDirectory(backupDirectory + @"\Server Data");
			Directory.CreateDirectory(backupDirectory + @"\User Data");

			foreach (string file in Directory.GetFiles(Program.appData + @"\Server Data")) {
				File.Copy(
					file,
					backupDirectory + @"\Server Data\" + Path.GetFileName(file)
				);
			}

			foreach (string file in Directory.GetFiles(Program.appData + @"\User Data")) {
				File.Copy(
					file,
					backupDirectory + @"\User Data\" + Path.GetFileName(file)
				);

			}

			Program.Log("Done.");
		}
	}
}
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using static discord_bot.Program;

namespace discord_bot
{
	static class JTalk
	{
		private static int counter = 0;
		private const double default_speed = 0.8;
		private static string TEMP_FOLDER_NAME = "jtalk_temp";
		private static string TEMP_FOLDER_PATH = Path.Combine(Environment.CurrentDirectory, $"{TEMP_FOLDER_NAME}");
		static JTalk()
		{
			if (!Directory.Exists(TEMP_FOLDER_PATH))
			{
				Directory.CreateDirectory(TEMP_FOLDER_PATH);
			}
		}
		public static async Task<string> Generate(string text ,double speed = default_speed)
		{
			int count = counter++;
			string wav_path = Path.Combine(TEMP_FOLDER_PATH, $"{count}.wav");
			string voice_path = Path.Combine(Config.Instance.JTALK.VOICEPATH,"mei_normal.htsvoice");
			var app = new ProcessStartInfo
			{
				FileName = Config.Instance.JTALK.PATH,
				RedirectStandardInput = true,
				ArgumentList = { "-m" , voice_path , "-r" , speed.ToString() , "-x" , Config.Instance.JTALK.DICPATH, "-ow" , wav_path },
			};
			var process = Process.Start(app);
			process.StandardInput.Write(text);
			process.StandardInput.Close();
			await process.WaitForExitAsync();
			return wav_path;

		}
	}
}

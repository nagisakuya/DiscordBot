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
		public static async Task<string> Generate(string text)
		{
			int current = counter++;

			string jtalk_path = $@"{Config.Instance.JTALK.BINPATH}\open_jtalk.exe";
			string wav_path = $@"{Config.Instance.JTALK.BINPATH}\{current}.wav";
			string voice_path = $@"{Config.Instance.JTALK.VOICEPATH}\mei_normal.htsvoice";
			var app = new ProcessStartInfo
			{
				WorkingDirectory = Config.Instance.JTALK.BINPATH,
				FileName = jtalk_path,
				UseShellExecute = false,
				RedirectStandardInput = true,
				CreateNoWindow = true,
				ArgumentList = { "-m" , voice_path , "-r" , "0.6" , "-x" , Config.Instance.JTALK.DICPATH, "-ow" , wav_path },
			};
			var process = Process.Start(app);
			process.StandardInput.Write(text);
			process.StandardInput.Close();
			await process.WaitForExitAsync();
			return wav_path;

		}
	}
}

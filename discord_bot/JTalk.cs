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

			string wav_path = Path.Combine( Environment.CurrentDirectory,$"{current}.wav");
			string voice_path = Path.Combine(Config.Instance.JTALK.VOICEPATH,"mei_normal.htsvoice");
			var app = new ProcessStartInfo
			{
				FileName = Config.Instance.JTALK.PATH,
				RedirectStandardInput = true,
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

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace discord_bot
{
	static class JTalk
	{
		private static int counter = 0;
		public static async Task<string> Generate(string text)
		{
			int current = counter++;

			string BINPATH = @"C:\open_jtalk\bin";
			string VOICEPATH = @"C:\open_jtalk\voice";
			string DICPATH = @"C:\open_jtalk\dic";

			string jtalk_path = $@"{BINPATH}\open_jtalk.exe";
			string voice_path = $@"{VOICEPATH}\mei_normal.htsvoice";
			string wav_path = $@"{BINPATH}\{current}.wav";
			var app = new ProcessStartInfo
			{
				WorkingDirectory = BINPATH,
				FileName = jtalk_path,
				UseShellExecute = false,
				RedirectStandardInput = true,
				CreateNoWindow = true,
				ArgumentList = { "-m" , voice_path , "-r" , "0.6" , "-x" , DICPATH , "-ow" , wav_path },
			};
			var process = Process.Start(app);
			process.StandardInput.Write(text);
			process.StandardInput.Close();
			await process.WaitForExitAsync();
			return wav_path;

		}
	}
}

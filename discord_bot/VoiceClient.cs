using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Audio;
using Discord.Rest;
using System.Diagnostics;
using static discord_bot.Utility;
using static discord_bot.Program;

namespace discord_bot
{
	class VoiceClient
	{
		static internal Dictionary<ulong, VoiceClient> active_voice_clients = new() { };
		internal IAudioClient audio_client;
		internal SocketGuild connected_guild;
		internal AudioOutStream audio_stream;
		protected VoiceClient() { }
		protected VoiceClient(IAudioClient client, SocketGuild guild) { audio_client = client; connected_guild = guild; audio_stream = audio_client.CreatePCMStream(AudioApplication.Music); }
		public static async Task<VoiceClient> Construct(SocketGuildUser caller, ISocketMessageChannel text_channel = null)
		{
			if (!caller.VoiceState.HasValue)
			{
				await SendError(text_channel, Error.FizzedOut);
				return null;
			}
			if (active_voice_clients.ContainsKey(caller.Guild.Id))
			{
				await SendError(text_channel, Error.FizzedOut);
				return null;
			}
			VoiceClient new_client = new(await caller.VoiceState.Value.VoiceChannel.ConnectAsync(), caller.Guild);
			active_voice_clients.Add(new_client.connected_guild.Id, new_client);
			//Console.CancelKeyPress += new ConsoleCancelEventHandler(new_client.Delete);
			return new_client;
		}
		public void Delete(object sender, ConsoleCancelEventArgs args)
		{
			_ = audio_client.StopAsync();
		}
		public async virtual Task Delete()
		{
			await audio_client.StopAsync();
			active_voice_clients.Remove(connected_guild.Id);
			//Console.CancelKeyPress -= new ConsoleCancelEventHandler(Delete);
		}
		protected Process CreateStream(string path)
		{
			return Process.Start(new ProcessStartInfo
			{
				FileName = "\"C:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe\"",
				Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
				UseShellExecute = false,
				RedirectStandardOutput = true,
			});
		}
	}

	class Reader : VoiceClient
	{
		protected IList<SocketUser> target_list = new List<SocketUser> { };
		protected Reader() { }
		protected Reader(VoiceClient from) { audio_client = from.audio_client; connected_guild = from.connected_guild; audio_stream = from.audio_stream; }
		public static new async Task<Reader> Construct(SocketGuildUser caller, ISocketMessageChannel text_channel = null)
		{
			var reader = new Reader(await VoiceClient.Construct(caller, text_channel));
			reader.target_list.Add(caller);
			client.MessageReceived += reader.CatchMessage;
			return reader;
		}
		public async override Task Delete()
		{
			client.MessageReceived -= CatchMessage;
			await base.Delete();
		}
		public async Task Read(IUserMessage message)
		{
			using var ffmpeg = CreateStream(await JTalk.Generate(message.Content));
			using var output = ffmpeg.StandardOutput.BaseStream;
			//using var discord = audio_client.CreatePCMStream(AudioApplication.Music);
			try { await output.CopyToAsync(audio_stream); }
			finally { await audio_stream.FlushAsync(); }
		}
		public Task CatchMessage(SocketMessage message)
		{
			if (message is SocketUserMessage user_message && target_list.Contains(user_message.Author) && !CommandModule.IsCommand(user_message))
			{
				Task.Run(() => Read(user_message));
			}
			return Task.CompletedTask;
		}
	}
}

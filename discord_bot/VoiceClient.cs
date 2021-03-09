using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Audio;
using Discord.Rest;
using Discord.Commands;
using System.Diagnostics;
using System.IO;
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
		protected VoiceClient(IAudioClient client, SocketGuild guild) { audio_client = client; connected_guild = guild; audio_stream = audio_client.CreatePCMStream(AudioApplication.Voice); }
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
		public static async Task Bye(SocketCommandContext context)
		{
			if (active_voice_clients.TryGetValue(context.Guild.Id, out var client))
			{
				await context.Channel.SendMessageAsync($"さようなら");
				await client.Delete();
			}
			else
			{
				await SendError(context.Channel, Error.FizzedOut);
			}
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
				FileName = Config.Instance.FFMPEG.PATH ,
				Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
				UseShellExecute = false,
				RedirectStandardOutput = true,
			});
		}
	}

	class Reader : VoiceClient
	{
		protected IList<IUser> target_list = new List<IUser> { };
		protected Queue<Stream> queue = new Queue<Stream> { };
		protected bool is_reading = false;
		protected Reader() { }
		protected Reader(VoiceClient from) { audio_client = from.audio_client; connected_guild = from.connected_guild; audio_stream = from.audio_stream; }
		public static new async Task<Reader> Construct(SocketGuildUser caller, ISocketMessageChannel text_channel = null)
		{
			var temp = await VoiceClient.Construct(caller, text_channel);
			if (temp == null)
			{
				if (active_voice_clients.TryGetValue(caller.Guild.Id,out var active_one) && active_one is Reader active_reader )
				{
					await active_reader.AddTarget(caller);
				}
				else
				{
					await SendError(text_channel, Error.FizzedOut);
				}
				return null;
			}
			var reader = new Reader(temp);
			await reader.AddTarget(caller);
			client.MessageReceived += reader.CatchMessage;
			return reader;
		}
		public async Task AddTarget(IUser add , ISocketMessageChannel text_channel = null)
		{
			target_list.Add(add);
			await text_channel.SendMessageAsync($"{add.Mention}の書いたことをしゃべります！");
		}
		public async override Task Delete()
		{
			client.MessageReceived -= CatchMessage;
			await base.Delete();
		}
		public async Task Read(IUserMessage message)
		{
			var wav_path = await JTalk.Generate(message.Content);
			var output = CreateStream(wav_path).StandardOutput.BaseStream;
			//File.Delete(wav_path);
			queue.Enqueue(output);
			if (is_reading == false)
			{
				is_reading = true;
				while (true)
				{
					var lead = queue.Dequeue();
					await lead.CopyToAsync(audio_stream);
					await audio_stream.FlushAsync();
					if (queue.Count == 0)
					{
						is_reading = false;
						break;
					}
				}
			}
			

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

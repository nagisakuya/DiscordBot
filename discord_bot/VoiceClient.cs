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
using System.Text.RegularExpressions;

namespace discord_bot
{
	class VoiceClient
	{
		static internal Dictionary<ulong, VoiceClient> active_voice_clients = new() { };
		internal IAudioClient audio_client;
		internal SocketGuild connected_guild;
		internal AudioOutStream audio_stream;
		protected Queue<Stream> queue = new() { };
		protected bool is_playing = false;
		internal int living_ffmpeg_counter = 0;
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
				await context.Channel.SendDisapperMessage($"さようなら");
				await client.Delete();
			}
			else
			{
				await SendError(context.Channel, Error.FizzedOut);
			}
		}
		public static async Task Reset(SocketCommandContext context, bool message_flag = true)
		{
			if (active_voice_clients.TryGetValue(context.Guild.Id, out var client))
			{
				if (message_flag)
					await context.Channel.SendDisapperMessage($"再起動します...\nデバッグ情報:queue={client.queue.Count} playing={client.is_playing} living_process={client.living_ffmpeg_counter}");
				client.queue.Clear();
				client.is_playing = false;
			}
			else
			{
				await SendError(context.Channel, Error.FizzedOut);
			}
		}
		public async Task Play(string wav_path)
		{
			var process = Process.Start(new ProcessStartInfo
			{
				FileName = Config.Instance.FFMPEG.PATH,
				Arguments = $"-hide_banner -loglevel panic -i \"{wav_path}\" -ac 2 -f s16le -ar 48000 pipe:1",
				UseShellExecute = false,
				RedirectStandardOutput = true,
			});
			queue.Enqueue(process.StandardOutput.BaseStream);
			if (is_playing == false)
			{
				is_playing = true;
				while (true)
				{
					var queue_first = queue.Dequeue();
					await queue_first.CopyToAsync(audio_stream);
					if (queue.Count == 0)
					{
						is_playing = false;
						break;
					}
				}
			}
			process.WaitForExit(10000);
			//process.Kill();
			if (!process.HasExited)
			{
				living_ffmpeg_counter++;
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
	}

	class Reader : VoiceClient
	{
		const int GRASS_LIMIT = 5;
		const double DEFAULT_READ_SPEED = 0.8;
		double read_speed = DEFAULT_READ_SPEED;
		const int TEXT_LENGTH_LIMIT = 30;
		const int DELETE_TIME = 60 * 1000;
		protected IList<IUser> target_list = new List<IUser> { };
		protected Reader() { }
		protected Reader(VoiceClient from) { audio_client = from.audio_client; connected_guild = from.connected_guild; audio_stream = from.audio_stream; }
		public static new async Task<Reader> Construct(SocketGuildUser caller, ISocketMessageChannel text_channel = null)
		{
			var temp = await VoiceClient.Construct(caller, text_channel);
			if (temp == null)
			{
				if (active_voice_clients.TryGetValue(caller.Guild.Id, out var active_one) && active_one is Reader active_reader)
				{
					await active_reader.AddTarget(caller, text_channel);
				}
				return null;
			}
			var reader = new Reader(temp);
			await reader.AddTarget(caller, text_channel);
			client.MessageReceived += reader.CatchMessage;
			return reader;
		}
		public static string Format(string str)
		{
			str = Regex.Replace(str, "(http://|https://)[^ ]+", "URL");
			str = Regex.Replace(str, "<@.?[0-9]{18}>", "");
			str = Regex.Replace(str, "[ｗ]{" + GRASS_LIMIT + ",}", string.Concat(Enumerable.Repeat("わら", GRASS_LIMIT)));
			str = str.Replace("ｗ", "わら");
			return str;
		}
		public async Task AddTarget(IUser add, ISocketMessageChannel text_channel = null)
		{
			target_list.Add(add);
			await text_channel?.SendDisapperMessage($"{add.Mention}の書いたことをしゃべります！");
		}
		public async override Task Delete()
		{
			client.MessageReceived -= CatchMessage;
			await base.Delete();
		}

		public Task CatchMessage(SocketMessage message)
		{
			if (message is SocketUserMessage user_message && target_list.Contains(user_message.Author) && !CommandModule.IsCommand(user_message) && user_message.MentionedUsers.Count == 0)
			{
				string text = Format(message.Content);
				double speed = text.Length > TEXT_LENGTH_LIMIT ? read_speed * text.Length / TEXT_LENGTH_LIMIT : read_speed;
				Task.Run(() =>
				{
					_ = Play(JTalk.Generate(text, speed).Result);
					Task.Delay(DELETE_TIME).Wait();
					message.DeleteAsync();
				});
			}
			return Task.CompletedTask;
		}
	}
}

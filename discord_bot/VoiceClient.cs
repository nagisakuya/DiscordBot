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
		static protected Dictionary<ulong, VoiceClient> active_voice_clients = new() { };
		private SocketGuild connected_guild;
		private IAudioClient audio_client;
		private AudioOutStream audio_stream;
		private Queue<Stream> queue = new() { };
		private bool playing = false;
		private static int living_ffmpeg_counter = 0;
		public (int queue_count, bool is_playing, int living_ffmpeg) debug_info
		{
			get
			{
				return (queue.Count, playing, living_ffmpeg_counter);
			}
		}
		public VoiceClient(SocketVoiceChannel channel, ISocketMessageChannel text_channel = null)
		{
			Task.Run(()=>{
				if (channel == null )
				{
					_ = text_channel.SendError(Error.FizzedOut);
				}
				if (active_voice_clients.ContainsKey(channel.Guild.Id))
				{
					_ = text_channel.SendError(Error.Obstacle);
				}
				else
				{
					audio_client = channel.ConnectAsync().Result;
					audio_stream = audio_client.CreatePCMStream(AudioApplication.Voice);
					connected_guild = channel.Guild;
					active_voice_clients.Add(connected_guild.Id, this);
				}
			});
		}
		public void Reset()
		{
			queue.Clear();
			playing = false;
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
			if (playing == false)
			{
				playing = true;
				while (true)
				{
					var queue_first = queue.Dequeue();
					await queue_first.CopyToAsync(audio_stream);
					if (queue.Count == 0)
					{
						playing = false;
						break;
					}
				}
			}
			process.WaitForExit(10000);
			process.Kill();
			if (!process.HasExited)
			{
				living_ffmpeg_counter++;
			}
		}
		public async virtual Task Disconnect()
		{
			await audio_client.StopAsync();
			active_voice_clients.Remove(connected_guild.Id);
		}
		public static VoiceClient Find(IGuild guild)
		{
			return active_voice_clients.TryGetValue(guild.Id, out var client) ?
				client : null;
		}
		public static bool TryFind(IGuild guild,out VoiceClient voice_client)
		{
			return active_voice_clients.TryGetValue(guild.Id, out voice_client);
		}
	}

	class Reader : VoiceClient
	{
		const int GRASS_LIMIT = 5;
		const double DEFAULT_READ_SPEED = 0.8;
		double read_speed = DEFAULT_READ_SPEED;
		const int TEXT_LENGTH_LIMIT = 30;
		const int DELETE_TIME = 60 * 1000;
		private IList<IUser> target_list = new List<IUser> { };
		public Reader(SocketGuildUser caller, ISocketMessageChannel text_channel = null) : base(caller.VoiceChannel, text_channel)
		{
			AddTarget(caller, text_channel);
			client.MessageReceived += CatchMessage;
		}
		public static string Format(string str)
		{
			str = Regex.Replace(str, "(http://|https://)[^ ]+", "URL");
			str = Regex.Replace(str, "<@.?[0-9]{18}>", "");
			str = Regex.Replace(str, "[ｗ]{" + GRASS_LIMIT + ",}", string.Concat(Enumerable.Repeat("わら", GRASS_LIMIT)));
			str = str.Replace("ｗ", "わら");
			return str;
		}
		public void AddTarget(IUser user, ISocketMessageChannel text_channel = null)
		{
			if (!target_list.Contains(user))
			{
				target_list.Add(user);
				text_channel?.SendDisapperMessage($"{user.Mention}の書いたことをしゃべります！");
			}
			else
			{
				text_channel?.SendError(Error.FizzedOut);
			}
		}
		public void RemoveTarget(IUser user, ISocketMessageChannel text_channel = null)
		{
			if (target_list.Remove(user))
			{
				text_channel?.SendDisapperMessage($"{user.Mention}の書いたことをしゃべるのをやめました🥺");
			}
			else
			{
				text_channel?.SendError(Error.FizzedOut);
			}
		}
		public async override Task Disconnect()
		{
			client.MessageReceived -= CatchMessage;
			await base.Disconnect();
		}
		public Task CatchMessage(SocketMessage message)
		{
			Task.Run(() =>
			{
				if (message is SocketUserMessage user_message && target_list.Contains(user_message.Author) && !CommandModule.IsCommand(user_message) && user_message.MentionedUsers.Count == 0)
				{
					Console.WriteLine("that message is caught by reader");
					string text = Format(message.Content);
					double speed = text.Length > TEXT_LENGTH_LIMIT ? read_speed * text.Length / TEXT_LENGTH_LIMIT : read_speed;
					string wav_path = JTalk.Generate(text, speed).Result;
					var task = Play(wav_path);
					Task.Delay(DELETE_TIME).Wait();
					task.Wait();
					message.DeleteAsync();
					File.Delete(wav_path);
				}
			});
			return Task.CompletedTask;
		}
		public static new Reader Find(IGuild guild)
		{
			active_voice_clients.TryGetValue(guild.Id, out var temp);
			return temp as Reader;
		}
		public static bool TryFind(IGuild guild, out Reader reader)
		{
			reader = Find(guild);
			return reader != null ? true : false;
		}
	}
}

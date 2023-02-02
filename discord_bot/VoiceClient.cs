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
		private Task last_play_task = Task.CompletedTask;
		private static int living_ffmpeg_counter = 0;
		public (int queue_count, bool is_playing, int living_ffmpeg) DebugInfo => (queue.Count, last_play_task.IsCompleted, living_ffmpeg_counter);
		public VoiceClient(SocketVoiceChannel channel, ISocketMessageChannel text_channel = null)
		{
			Task.Run(() =>
			{
				if (channel == null)
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
			last_play_task = Task.CompletedTask;
		}
		public Task Play(string wav_path)
		{
			var process = Process.Start(new ProcessStartInfo
			{
				FileName = Config.Instance.FFMPEG.PATH,
				Arguments = $"-hide_banner -loglevel panic -i \"{wav_path}\" -ac 2 -f s16le -ar 48000 pipe:1",
				UseShellExecute = false,
				RedirectStandardOutput = true,
			});
			var stream = process.StandardOutput.BaseStream;
			var task_for_wait = last_play_task;
			return last_play_task = Task.Run(async () =>
			{
				task_for_wait.Wait();
				await stream.CopyToAsync(audio_stream);
				process.Kill();
			});
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
		public static bool TryFind(IGuild guild, out VoiceClient voice_client)
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
		const int DELETE_TIME = 30 * 60 * 1000;
		readonly static Regex URL_PATTERN = new("(http://|https://)[^ ]+");
		readonly static Regex MENTION_PATTERN = new("<@.?[0-9]{18}>");
		bool DELETE_FLAG = true;
		private IList<IUser> target_list = new List<IUser> { };
		public Reader(SocketGuildUser caller, ISocketMessageChannel text_channel = null) : base(caller.VoiceChannel, text_channel)
		{
			AddTarget(caller, text_channel);
			client.MessageReceived += CatchMessage;
		}
		public static string Format(SocketMessage message)
		{
			var str = message.Content;
			foreach (var user in message.MentionedUsers)
			{
				str = MENTION_PATTERN.Replace(str, "@" + user.Username, 1);
			}
			str = URL_PATTERN.Replace(str, "");
			str = Regex.Replace(str, "[ｗ]{" + GRASS_LIMIT + ",}", string.Concat(Enumerable.Repeat("わら", GRASS_LIMIT)));
			str = str.Replace("ｗ", "わら");
			str = str.Replace("～", "ー");
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
		public void DisableDelete(ISocketMessageChannel text_channel = null)
		{
			if (DELETE_FLAG)
			{
				DELETE_FLAG = false;
				text_channel?.SendDisapperMessage($"メッセージが消えなくなりました");
			}
			else
			{
				text_channel?.SendError(Error.FizzedOut);
			}
		}
		public void EnableDelete(ISocketMessageChannel text_channel = null)
		{
			if (!DELETE_FLAG)
			{
				DELETE_FLAG = true;
				text_channel?.SendDisapperMessage($"メッセージが消えるようになりました");
			}
			else
			{
				text_channel?.SendError(Error.FizzedOut);
			}
		}
		private static bool HasNoMention(IMessage message)
		{
			return message.MentionedUserIds.Count == 0
				&& message.MentionedRoleIds.Count == 0
				&& !message.MentionedEveryone
				&& message.MentionedChannelIds.Count == 0;
		}
		private static int URLCount(IMessage message)
		{
			return URL_PATTERN.Match(message.Content).Captures.Count;
		}
		public Task CatchMessage(SocketMessage message)
		{
			Task.Run(async () =>
			{
				if (message is SocketUserMessage user_message && target_list.Contains(user_message.Author) && !CommandModule.IsCommand(user_message))
				{
					Console.WriteLine("that message is caught by reader");
					string text = Format(message);
					if (text.Length == 0) return;
					double speed = text.Length > TEXT_LENGTH_LIMIT ? read_speed * text.Length / TEXT_LENGTH_LIMIT : read_speed;
					string wav_path = await JTalk.Generate(text, speed);
					await Play(wav_path);
					if (DELETE_FLAG && URLCount(message) == 0 && message.Attachments.Count == 0 && HasNoMention(message))
					{
						Console.WriteLine("that message will delete by reader");
						await Task.Delay(DELETE_TIME);
						_ = message.DeleteAsync();
						File.Delete(wav_path);
					}
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
			return reader != null;
		}
	}
}

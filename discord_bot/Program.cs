using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Audio;
using Discord.Net;
using Discord.Commands;
using discord_bot;
using Microsoft.Extensions.Configuration;

namespace discord_bot
{
	class Program
	{
		public class Config
		{
			public static Config Instance;
			public class Discord
			{
				public string TOKEN { get; set; }
			}
			public Discord DISCORD { get; set; }
			public class JTalk
			{
				public string PATH { get; set; }
				public string DICPATH { get; set; }
				public string VOICEPATH { get; set; }
			}
			public JTalk JTALK { get; set; }
			public class ffmpeg
			{
				public string PATH { get; set; }
			}
			public ffmpeg FFMPEG { get; set; }
			public static Config Get()
			{
				if (Instance != null) return Instance;

				Instance = new ConfigurationBuilder()
					.AddIniFile(@"./config.ini")
					.Build()
					.Get<Config>();
				return Instance;
			}
		}

		public static readonly Config CONFIG = Config.Get();

		public static DiscordSocketClient client;
		public static CommandService commands;

		static void Main(string[] args)
			=> new Program().MainAsync().GetAwaiter().GetResult();

		public async Task MainAsync()
		{
			commands = new();
			client = new DiscordSocketClient(new DiscordSocketConfig
			{
				LogLevel = LogSeverity.Info
			});
			client.Log += Log;
			client.MessageReceived += LogMessage;
			//client.MessageReceived += ReplaceToSummon;
			client.MessageReceived += DeleteMention;
			CommandModule command_module = new();
			await command_module.InstallCommandsAsync();
			await client.LoginAsync(TokenType.Bot, Config.Instance.DISCORD.TOKEN);
			await client.StartAsync();
			await Task.Delay(-1);
		}
		private static Task LogMessage(SocketMessage message)
		{
			Console.WriteLine("{0} {1} {2}:{3}", DateTime.Now.ToLongTimeString(), message.Channel.Name, message.Author.Username, message.Content);
			return Task.CompletedTask;
		}
		private static async Task ReplaceToSummon(SocketMessage messageParam)
		{
			if (messageParam is SocketUserMessage message && MentionUtils.TryParseUser(message.Content, out var target_id))
			{
				//message.ModifyAsync(m=>m.Content = $"༽୧༺ ‡۞卍✞༒ {message.Content} ༒✞卍۞‡༻୨༼");
				await message.Channel.SendDisapperMessage($"༽୧༺ ‡۞卍✞༒ {client.GetUser(target_id).Username} ༒✞卍۞‡༻୨༼");
				//message.DeleteAsync();
			}
		}
		private static async Task DeleteMention(SocketMessage messageParam)
		{
			if (messageParam is SocketUserMessage message && message.MentionedUsers.Count == 1)
			{
				client.UserVoiceStateUpdated += (user,before,after) =>
				{
					Task.Run(() =>
					{
						if (user.Id == message.MentionedUsers.First().Id)
						{
							message.DeleteAsync();
						}
					});
					return Task.CompletedTask;
				};
			}
		}
		private static Task Log(LogMessage message)
		{
			Console.WriteLine(message.ToString());
			return Task.CompletedTask;
		}

	}
}
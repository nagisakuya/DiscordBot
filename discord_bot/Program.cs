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
		private static Task Log(LogMessage message)
		{
			Console.WriteLine(message.ToString());
			return Task.CompletedTask;
		}

	}
}
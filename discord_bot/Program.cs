using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace discord_bot
{
	class Program
	{
		public static DiscordSocketClient client;

		static void Main(string[] args)
			=> new Program().MainAsync().GetAwaiter().GetResult();

		public async Task MainAsync()
		{
			client = new DiscordSocketClient(new DiscordSocketConfig
			{
				LogLevel = LogSeverity.Info
			});
			client.Log += Log;
			client.MessageReceived += WriteDownMessage;
			client.MessageReceived += Commands.CheckCommand;
			client.MessageReceived += CheckByeBye;
			string token = "ODE2NTI4ODg1NTU0NDc5MTI0.YD8RyA.souS_C6w8y29EEKh1kU7QKn0YNI";
			await client.LoginAsync(TokenType.Bot, token);
			await client.StartAsync();
			await Task.Delay(-1);
		}
		private static async Task<object> WriteDownMessage(SocketMessage message)
		{
			Console.WriteLine("{0} {1} {2}:{3}", DateTime.Now.ToLongTimeString(), message.Channel.Name, message.Author.Username, message.Content);
			return await Task.FromResult<object>(null);
		}
		private static async Task<object> CheckByeBye(SocketMessage message)
		{
			if (message.Content == "byebye")
			{
				await client.StopAsync();
			}
			return await Task.FromResult<object>(null);
		}
		private static Task Log(LogMessage message)
		{
			Console.WriteLine(message.ToString());
			return Task.CompletedTask;
		}

	}
}
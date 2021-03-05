using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace TestHoge
{
    class Program
    {
        private DiscordSocketClient _client;
        public static CommandService _commands;
        public static IServiceProvider _services;

        static void Main(string[] args)
                    => new Program().MainAsync().GetAwaiter().GetResult();
        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });
            _client.Log += Log;
            _commands = new CommandService();
            _services = new ServiceCollection().BuildServiceProvider();
            _client.MessageReceived += CommandRecieved;

            //次の行に書かれているstring token = "hoge"に先程取得したDiscordTokenを指定する。
            string token = "hogehogehogehoge";
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }
}
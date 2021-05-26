using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using System.Reflection;
using System.Linq;
using Discord;
using Discord.Audio;
using Discord.Net;
using static discord_bot.Utility;
using static discord_bot.Program;

namespace discord_bot
{
	
	public class CommandModule : ModuleBase<SocketCommandContext>
	{
		private static readonly char PREFIX = '!';
		private static readonly ulong SARYO_ID = 353199430687653898;
		private static readonly ulong WATCHING_ID = 241192743345455105;
		public async Task InstallCommandsAsync()
		{
			client.MessageReceived += HandleCommandAsync;
			await commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),services: null);
		}
		public static bool IsCommand(IUserMessage message)
		{
			int argPos = 0;
			return message.HasCharPrefix(PREFIX, ref argPos) ||
					message.HasMentionPrefix(client.CurrentUser, ref argPos);
		}
		private async Task HandleCommandAsync(SocketMessage messageParam)
		{
			if (messageParam is not SocketUserMessage message) return;

			int argPos = 0;

			if (!(message.HasCharPrefix(PREFIX, ref argPos) ||
				message.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
				message.Author.IsBot)
				return;

			var context = new SocketCommandContext(client, message);

			if ((await commands.ExecuteAsync(
				context: context,
				argPos: argPos,
				services: null)).IsSuccess
			)
			{
				await context.Message.DeleteAsync();
			}
			else
			{
				await SendError(messageParam.Channel, Error.UnknownCommand);
			}
		}

		[Command("hello", RunMode = RunMode.Async)]
		[Summary("挨拶します")]
		public async Task SayHello()
		{
			await Context.Channel.SendMessageAsync($"こんにちは！🦀 {client.CurrentUser.Username}です！\"{PREFIX}help\" で使い方を確認できます！");
		}

		[Command("help", RunMode = RunMode.Async)]
		[Summary("ヘルプを表示します")]
		public async Task ShowHelp()
		{
			EmbedBuilder embedBuilder = new();	
			foreach (CommandInfo command in commands.Commands)
			{
				string embedFieldText = command.Summary ?? "説明不要！\n";
				string Header = $"{PREFIX}{command.Name}";
				foreach( var param in command.Parameters)
				{
					Header += $" [{param.Summary}]";
				}
				embedBuilder.AddField(Header, embedFieldText);
			}
			embedBuilder.WithColor(Color.Green);
			await ReplyAsync($"\"{PREFIX}\"の代わりに{client.CurrentUser.Mention}でも呼び出せます！", false, embedBuilder.Build());
		}

		[Command("summon", RunMode = RunMode.Async)]
		[Summary("茶寮を召喚")]
		public async Task Summon()
		{
			if (client.GetUser(SARYO_ID) is SocketUser saryo)
			{
				await Context.Channel.SendMessageAsync($"༽୧༺ ‡۞卍✞༒ {saryo.Mention} ༒✞卍۞‡༻୨༼");
			}
			else
			{
				await SendError(Context.Channel, Error.UserNotFound);
			}

		}

		[Command("summon", RunMode = RunMode.Async)]
		[Summary("ターゲットを召喚！")]
		public async Task Summon([Summary("target")]string mention)
		{
			if (MentionUtils.TryParseUser(mention,out var _))
			{
				await Context.Channel.SendMessageAsync($"༽୧༺ ‡۞卍✞༒ {mention} ༒✞卍۞‡༻୨༼");
			}
			else
			{
				await SendError(Context.Channel, Error.UserNotFound);
			}
		}

		private static readonly int ROLL_MIN_DEFAULT = 1;
		private static readonly int ROLL_MAX_DEFAULT = 100;
		[Command("roll", RunMode = RunMode.Async)]
		[Summary("100面ダイスを振ります")]
		public async Task Roll()
		{
			await Context.Channel.SendMessageAsync($"{Context.User.Mention}が{ROLL_MAX_DEFAULT}面ダイスを振った...{new Random().Next(ROLL_MIN_DEFAULT, ROLL_MAX_DEFAULT + 1)}！");
		}
		[Command("roll", RunMode = RunMode.Async)]
		[Summary("X面ダイスを振ります")]
		public async Task Roll([Summary("X")] int max)
		{
			if (max <= 0)
			{
				await SendError(Context.Channel, Error.SomethingIsWrong);
				return;
			}
			await Context.Channel.SendMessageAsync($"{Context.User.Mention}が{max}面ダイスを振った...{new Random().Next(ROLL_MIN_DEFAULT, max + 1)}！");
		}
		[Command("roll", RunMode = RunMode.Async)]
		[Summary("指定された範囲の数字を一つ選びます")]
		public async Task Roll([Summary("min")] int min ,[Summary("max")] int max)
		{
			if (min > max)
			{
				Swap(ref min,ref max);
			}
			await Context.Channel.SendMessageAsync($"{Context.User.Mention}の為に{min}から{max}までの数字を一つ選んだ...{new Random().Next(min, max + 1)}！");
		}

		[Command("flip", RunMode = RunMode.Async)]
		[Summary("コインを投げます")]
		public async Task Flip()
		{
			await Context.Channel.SendMessageAsync($"{Context.User.Mention}がコインを投げた...{(new Random().Next(0, 2) == 0 ? "表" : "裏")}！");
		}

		[Command("mute", RunMode = RunMode.Async)]
		[Summary("watchingを強制ミュートします")]
		public async Task Mute()
		{
			if (await Context.Channel.GetUserAsync(WATCHING_ID) is not SocketGuildUser watching || !watching.VoiceState.HasValue)
			{
				await SendError(Context.Channel, Error.UserNotFound);
				return;
			}
			await watching.ModifyAsync((target) => target.Mute = true);
			await Context.Channel.SendMessageAsync("(ファミチキください)");
		}

		[Command("unmute", RunMode = RunMode.Async)]
		[Summary("ミュート解除")]
		public async Task Unmute()
		{
			if (await Context.Channel.GetUserAsync(WATCHING_ID) is not SocketGuildUser watching || !watching.VoiceState.HasValue)
			{
				await SendError(Context.Channel, Error.UserNotFound);
				return;
			}
			await watching.ModifyAsync((target) => target.Mute = false);
			await Context.Channel.SendMessageAsync("封印解除！");
		}

		[Command("sex", RunMode = RunMode.Async)]
		[Summary("セックスしないと出られない部屋を作成します")]
		public async Task CreateSexroom()
		{
			if (Context.Channel is SocketGuildChannel channel)
			{
				await SexRoom.Construct(Context.Channel, channel.Guild);
			}
		}

		[Command("blackhole", RunMode = RunMode.Async)]
		[Summary("ブラックホールを呼び出します")]
		public async Task CreateBlackhole()
		{
			if (Context.Channel is SocketGuildChannel channel)
			{
				await Blackhole.Construct(Context.Channel, channel.Guild, Context.User as SocketGuildUser);
			}
		}

		[Command("whitehole", RunMode = RunMode.Async)]
		[Summary("最後の手段->ブラックホールに突入する")]
		public async Task CreateWhitehole()
		{
			if (Context.Channel is SocketGuildChannel channel)
			{
				await Whitehole.Construct(Context.Channel, channel.Guild, Context.User as SocketGuildUser);
			}
		}

		[Command("speak", RunMode = RunMode.Async)]
		[Summary("チャット欄に打ち込んだ内容を喋ってくれます")]
		public async Task SpeakingClientConnect() {
			if (Context.User is SocketGuildUser caller)
			{
				await Reader.Construct(caller, Context.Channel);
			}
		}

		[Command("bye", RunMode = RunMode.Async)]
		[Summary("ボイスチャンネルのbotに別れを告げます")]
		public async Task SpeakingClientDisconnect()
		{
			await VoiceClient.Bye(Context);
		}
		[Command("reset", RunMode = RunMode.Async)]
		[Summary("Watchinpoが喋らなくなった時に押してください")]
		public async Task SpeakerReset()
		{
			await VoiceClient.Reset(Context);
		}
		[Command("enigma", RunMode = RunMode.Async)]
		[Summary("saryo氏")]
		public async Task SaryoLeadingTeamtoWIn()
		{
			await Context.Channel.SendMessageAsync($"https://www.youtube.com/watch?v=ZYKn9C25oQ4");
		}
	}
}

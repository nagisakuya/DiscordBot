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
			await commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
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
				_ = messageParam.Channel.SendError(Error.UnknownCommand);
			}
		}

		[Command("hello", RunMode = RunMode.Async)]
		[Summary("挨拶します")]
		public Task SayHello()
		{
			_ = Context.Channel.SendDisapperMessage($"こんにちは！{client.CurrentUser.Username}です！\"{PREFIX}help\" で使い方を確認できます！");
			return Task.CompletedTask;
		}

		[Command("help", RunMode = RunMode.Async)]
		[Summary("ヘルプを表示します")]
		public Task ShowHelp()
		{
			EmbedBuilder embedBuilder = new();
			foreach (CommandInfo command in commands.Commands)
			{
				string embedFieldText = command.Summary ?? "説明不要！\n";
				string Header = $"{PREFIX}{command.Name}";
				foreach (var param in command.Parameters)
				{
					Header += $" [{param.Summary}]";
				}
				embedBuilder.AddField(Header, embedFieldText);
			}
			embedBuilder.WithColor(Color.Green);
			_ = Context.Channel.SendDisapperMessage(text: $"\"{PREFIX}\"の代わりに{client.CurrentUser.Mention}でも呼び出せます！", embed: embedBuilder.Build());
			return Task.CompletedTask;
		}

		[Command("summon", RunMode = RunMode.Async)]
		[Summary("茶寮を召喚")]
		public Task Summon()
		{
			if (client.GetUser(SARYO_ID) is SocketUser saryo)
			{
				_ = Context.Channel.SendDisapperMessage($"༽୧༺ ‡۞卍✞༒ {saryo.Mention} ༒✞卍۞‡༻୨༼");
			}
			else
			{
				_ = Context.Channel.SendError(Error.UserNotFound);
			}
			return Task.CompletedTask;
		}

		[Command("summon", RunMode = RunMode.Async)]
		[Summary("ターゲットを召喚！")]
		public Task Summon([Summary("target")] string mention)
		{
			if (MentionUtils.TryParseUser(mention, out var _))
			{
				_ = Context.Channel.SendDisapperMessage($"༽୧༺ ‡۞卍✞༒ {mention} ༒✞卍۞‡༻୨༼");
			}
			else
			{
				_ = Context.Channel.SendError(Error.UserNotFound);
			}
			return Task.CompletedTask;
		}

		private static readonly int ROLL_MIN_DEFAULT = 1;
		private static readonly int ROLL_MAX_DEFAULT = 100;
		[Command("roll", RunMode = RunMode.Async)]
		[Summary("100面ダイスを振ります")]
		public Task Roll()
		{
			_ = Context.Channel.SendDisapperMessage($"{Context.User.Mention}が{ROLL_MAX_DEFAULT}面ダイスを振った...{new Random().Next(ROLL_MIN_DEFAULT, ROLL_MAX_DEFAULT + 1)}！");
			return Task.CompletedTask;
		}
		[Command("roll", RunMode = RunMode.Async)]
		[Summary("X面ダイスを振ります")]
		public Task Roll([Summary("X")] int max)
		{
			if (max <= 0)
			{
				_ = Context.Channel.SendError(Error.SomethingIsWrong);
			}
			else
			{
				_ = Context.Channel.SendDisapperMessage($"{Context.User.Mention}が{max}面ダイスを振った...{new Random().Next(ROLL_MIN_DEFAULT, max + 1)}！");
			}
			return Task.CompletedTask;
		}
		[Command("roll", RunMode = RunMode.Async)]
		[Summary("指定された範囲の数字を一つ選びます")]
		public Task Roll([Summary("min")] int min, [Summary("max")] int max)
		{
			if (min > max)
			{
				Swap(ref min, ref max);
			}
			_ = Context.Channel.SendDisapperMessage($"{Context.User.Mention}の為に{min}から{max}までの数字を一つ選んだ...{new Random().Next(min, max + 1)}！");
			return Task.CompletedTask;
		}

		[Command("flip", RunMode = RunMode.Async)]
		[Summary("コインを投げます")]
		public Task Flip()
		{
			_ = Context.Channel.SendDisapperMessage($"{Context.User.Mention}がコインを投げた...{(new Random().Next(0, 2) == 0 ? "表" : "裏")}！");
			return Task.CompletedTask;
		}

		[Command("mute", RunMode = RunMode.Async)]
		[Summary("watchingを強制ミュートします")]
		public Task Mute()
		{
			Task.Run(() =>
			{
				if (Context.Channel.GetUserAsync(WATCHING_ID).Result is not SocketGuildUser watching || !watching.VoiceState.HasValue)
				{
					_ = Context.Channel.SendError(Error.UserNotFound);
				}
				else
				{
					_ = watching.ModifyAsync((target) => target.Mute = true);
					_ = Context.Channel.SendDisapperMessage("(ファミチキください)");
				}
			});
			return Task.CompletedTask;
		}

		[Command("unmute", RunMode = RunMode.Async)]
		[Summary("ミュート解除")]
		public Task Unmute()
		{
			Task.Run(() =>
			{
				if (Context.Channel.GetUserAsync(WATCHING_ID).Result is not SocketGuildUser watching || !watching.VoiceState.HasValue)
				{
					_ = Context.Channel.SendError(Error.UserNotFound);
					return;
				}
				_ = watching.ModifyAsync((target) => target.Mute = false);
				_ = Context.Channel.SendDisapperMessage("封印解除！");
			});
			return Task.CompletedTask;
		}

		[Command("sex", RunMode = RunMode.Async)]
		[Summary("セックスしないと出られない部屋を作成します")]
		public Task CreateSexroom()
		{
			if (Context.Channel is SocketGuildChannel channel)
			{
				_ = SexRoom.Construct(Context.Channel, channel.Guild);
			}
			return Task.CompletedTask;
		}

		[Command("blackhole", RunMode = RunMode.Async)]
		[Summary("ブラックホールを呼び出します")]
		public Task CreateBlackhole()
		{
			if (Context.Channel is SocketGuildChannel channel)
			{
				_ = Blackhole.Construct(Context.Channel, channel.Guild, Context.User as SocketGuildUser);
			}
			return Task.CompletedTask;
		}

		[Command("whitehole", RunMode = RunMode.Async)]
		[Summary("最後の手段->ブラックホールに突入する")]
		public Task CreateWhitehole()
		{
			if (Context.Channel is SocketGuildChannel channel)
			{
				_ = Whitehole.Construct(Context.Channel, channel.Guild, Context.User as SocketGuildUser);
			}
			return Task.CompletedTask;
		}

		[Command("speak", RunMode = RunMode.Async)]
		[Summary("チャット欄に打ち込んだ内容を喋ってくれます")]
		public Task SpeakingClientConnect()
		{
			if (Context.User is SocketGuildUser caller)
			{
				_ = new Reader(caller, Context.Channel);
			}
			return Task.CompletedTask;
		}

		[Command("bye", RunMode = RunMode.Async)]
		[Summary("ボイスチャンネルのbotに別れを告げます")]
		public Task SpeakingClientDisconnect()
		{
			if (VoiceClient.TryFind(Context.Guild, out var voice_client))
			{
				_ = voice_client.Disconnect();
			}
			else
			{
				_ = Context.Channel.SendError(Error.FizzedOut);
			}
			return Task.CompletedTask;
		}
		[Command("reset", RunMode = RunMode.Async)]
		[Summary("Watchinpoが喋らなくなった時に押してください")]
		public Task SpeakerReset()
		{
			if (VoiceClient.TryFind(Context.Guild, out var voice_client))
			{
				var info = voice_client.debug_info;
				_ = Context.Channel.SendDisapperMessage("再起動します…");
				voice_client.Reset();
				_ = Context.Channel.SendDisapperMessage($"デバッグ情報:queue={info.queue_count} playing={info.is_playing} living_process={info.living_ffmpeg}");
			}
			else
			{
				_ = Context.Channel.SendError(Error.FizzedOut);
			}
			return Task.CompletedTask;
		}
		[Command("enigma", RunMode = RunMode.Async)]
		[Summary("saryo氏")]
		public Task SaryoLeadsTeamtoWIn()
		{
			_ = Context.Channel.SendDisapperMessage($"https://www.youtube.com/watch?v=ZYKn9C25oQ4");
			return Task.CompletedTask;
		}
	}
}

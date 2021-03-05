using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace discord_bot
{
	class Commands
	{
		private static readonly char PREFIX = '!';
		private static readonly ulong SARYO_ID = 353199430687653898;
		private static readonly ulong WATCHING_ID = 241192743345455105;
		enum Error
		{
			UserNotFound,
			CommandUndefined,
			UnknownCommand,
		}
		private static readonly Dictionary<Error, string> ErrorMessage = new()
		{
			{ Error.UserNotFound, "そんな人しらなーいヽ(`Д´)ﾉ" },
			{ Error.CommandUndefined, $"🚧工事中🚧" },
			{ Error.UnknownCommand, $"すみません、上手く聞き取れませんでした" },
		};
		enum Type
		{
			hello,
			help,
			detailedhelp,
			summon,
			roll,
			flip,
			mute,
			unmute,
			sex,
			blackhole,
			whitehole,
			speak,
			bye,
		}
		private static readonly Dictionary<Type, (Func<SocketMessage, List<string>, Task> func, List<string> description)> CommandDict = new()
		{
			//{ Type., (,  new(){""}) },
			{ Type.hello, (SayHello, new() { "挨拶します" }) },
			{ Type.help, (ShowHelp, new() { "ヘルプを表示します" }) },
			{ Type.detailedhelp, (ShowDetailedHelp, new() { "詳細なヘルプを表示します" }) },
			{ Type.summon, (Summon, new() { "茶寮を召喚！" }) },
			{ Type.roll, (Roll, new() { "100面ダイスを振ります" }) },
			{ Type.flip, (Flip, new() { "コインを投げます" }) },
			{ Type.mute, (Mute, new() { "watchingを強制ミュートします" }) },
			{ Type.unmute, (Unmute, new() { "ミュート解除" }) },
			{ Type.sex, (CreateSexroom, new() { "セックスしないと出られない部屋を作成します" }) },
			{ Type.blackhole, (CreateBlackhole, new() { "ブラックホールを呼び出します" }) },
			{ Type.whitehole, (CreateWhitehole, new() { "本棚の裏" }) },
			{ Type.speak, (SpeakingClientConnect, new() { "チャット欄に打ち込んだ内容を喋ってくれます" }) },
			{ Type.bye, (SpeakingClientDisconnect, new() { "ボイスチャンネルのbotに別れを告げます" }) },
		};
		public static async Task CheckCommand(SocketMessage message)
		{
			//var message = messageParam as SocketUserMessage;
			if (message == null || message.Author.IsBot)
			{
				return;
			}
			if (message.Content[0] != PREFIX)
			{
				return;
			}
			var param_list = new List<string>(message.Content.Split(' '));
			param_list[0] = param_list[0].Substring(1);
			if (Enum.TryParse(param_list[0], out Type type))
			{
				await CommandDict[type].func(message, param_list);
			}
			else
			{
				await message.Channel.SendMessageAsync($"{ErrorMessage[Error.UnknownCommand]}");
			}
		}
		private static string get_command(Type type)
		{
			return $"{PREFIX}{type}";
		}
		private static async Task SayHello(SocketMessage message, List<string> param)
		{
			await message.Channel.SendMessageAsync($"こんにちは！🦀 watchinpoです！\n{get_command(Type.help)} で使い方を確認できます！");
		}
		private static async Task ShowHelp(SocketMessage message, List<string> param)
		{
			string temp = "";
			foreach (Type type in Enum.GetValues(typeof(Type)))
			{
				temp += $"{get_command(type)}:{CommandDict[type].description[0]}\n";
			}
			await message.Channel.SendMessageAsync($"{temp}");
		}
		private static async Task ShowDetailedHelp(SocketMessage message, List<string> param)
		{
			await message.Channel.SendMessageAsync($"{ErrorMessage[Error.CommandUndefined]}");
		}
		private static async Task Summon(SocketMessage message, List<string> param)
		{
			if (Program.client.GetUser(SARYO_ID) is SocketUser saryo)
			{
				await message.Channel.SendMessageAsync($"༽୧༺ ‡۞卍✞༒ {saryo.Mention} ༒✞卍۞‡༻୨༼");
			}
			else
			{
				await message.Channel.SendMessageAsync($"{ErrorMessage[Error.UserNotFound]}");
			}

		}
		private static readonly int ROLL_MIN_DEFAULT = 1;
		private static readonly int ROLL_MAX_DEFAULT = 100;
		private static async Task Roll(SocketMessage message, List<string> param)
		{
			await message.Channel.SendMessageAsync($"{message.Author.Mention} が{ROLL_MAX_DEFAULT}面ダイスを振った...{new Random().Next(ROLL_MIN_DEFAULT, ROLL_MAX_DEFAULT + 1)}！");
		}
		private static async Task Flip(SocketMessage message, List<string> param)
		{
			await message.Channel.SendMessageAsync($"{message.Author.Mention} がコインを投げた...{(new Random().Next(0, 2) == 0 ? "表" : "裏")}！");
		}
		private static async Task Mute(SocketMessage message, List<string> param)
		{
			if (await message.Channel.GetUserAsync(WATCHING_ID) is not SocketGuildUser watching || !watching.VoiceState.HasValue)
			{
				await message.Channel.SendMessageAsync($"{ErrorMessage[Error.UserNotFound]}");
				return;
			}
			await watching.ModifyAsync((target) => target.Mute = true);
			await message.Channel.SendMessageAsync("(ファミチキください)");
		}
		private static async Task Unmute(SocketMessage message, List<string> param)
		{
			if (await message.Channel.GetUserAsync(WATCHING_ID) is not SocketGuildUser watching || !watching.VoiceState.HasValue)
			{
				await message.Channel.SendMessageAsync($"{ErrorMessage[Error.UserNotFound]}");
				return;
			}
			await watching.ModifyAsync((target) => target.Mute = false);
			await message.Channel.SendMessageAsync("封印解除！");
		}
		private static async Task CreateSexroom(SocketMessage message, List<string> param)
		{
			if (message.Channel is SocketGuildChannel channel)
			{
				VoiceChannel voicechannel = await VoiceChannel.Construct(channel.Guild);
			}
		}
		private static async Task CreateBlackhole(SocketMessage message, List<string> param)
		{
			await message.Channel.SendMessageAsync($"{ErrorMessage[Error.CommandUndefined]}");
		}
		private static async Task CreateWhitehole(SocketMessage message, List<string> param)
		{
			await message.Channel.SendMessageAsync($"{ErrorMessage[Error.CommandUndefined]}");
		}
		private static async Task SpeakingClientConnect(SocketMessage message, List<string> param)
		{
			await message.Channel.SendMessageAsync($"{ErrorMessage[Error.CommandUndefined]}");
		}
		private static async Task SpeakingClientDisconnect(SocketMessage message, List<string> param)
		{
			await message.Channel.SendMessageAsync($"{ErrorMessage[Error.CommandUndefined]}");
		}
	}
}

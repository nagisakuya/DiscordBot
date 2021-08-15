using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace discord_bot
{
	static class Utility
	{
		public enum Error
		{
			UserNotFound,
			CommandUndefined,
			UnknownCommand,
			NotEnoughUsers,
			FizzedOut,
			SomethingIsWrong,
		}
		public static readonly Dictionary<Error, string> ErrorMessage = new()
		{
			{ Error.UserNotFound, "そんな人しらなーいヽ(`Д´)ﾉ" },
			{ Error.CommandUndefined, $"🚧工事中🚧" },
			{ Error.UnknownCommand, $"すみません、上手く聞き取れませんでした" },
			{ Error.NotEnoughUsers, $"*しかし誰も来なかった" },
			{ Error.FizzedOut, $"しかし なにも おこらなかった！" },
			{ Error.SomethingIsWrong, $"先生、何もしていないのに壊れました！" },
		};
		public static async Task SendError(ISocketMessageChannel channel, Error error)
		{
			await channel?.SendMessageAsync($"{ ErrorMessage[error] }");
		}
		public static Task SendDisapperMessage(this ISocketMessageChannel channel, string text)
		{
			int DISAPPER_TIME = 60 * 1000;
			//awaitしちゃうとなぜか動かない
			_ = Task.Run(()=>
			{
				if (channel != null)
				{
					var mes = channel.SendMessageAsync(text);
					mes.Wait();
					Task.Delay(DISAPPER_TIME).Wait();
					mes.Result.DeleteAsync();
				}
			});
			return Task.CompletedTask;
		}
		public static IList<Type> ChooseRandom<Type>(IList<Type> ronly, uint number = 1)
		{
			/*if (from.Count > number)
			{
				throw new OverflowException("そんなに選べないよ～");
			}*/
			IList<Type> from = ronly.ToList();
			IList<Type> to = new List<Type>() { };
			for (uint i = 0; i < number; i++)
			{
				int choice = new Random().Next(0, (int)(number - i));
				to.Add(from[choice]);
				from.RemoveAt(choice);
			}
			return to;
		}
		public static IList<SocketGuildUser> SpeakingUsersInTheGuild(SocketGuild guild)
		{
			return guild.Users.Where(user => user.VoiceState.HasValue).ToList();
		}
		public static void Swap<Type>(ref Type a, ref Type b)
		{
			(a, b) = (b, a);
		}
	}
}

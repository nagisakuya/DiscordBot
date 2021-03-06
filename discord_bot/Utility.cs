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
		}
		public static readonly Dictionary<Error, string> ErrorMessage = new()
		{
			{ Error.UserNotFound, "そんな人しらなーいヽ(`Д´)ﾉ" },
			{ Error.CommandUndefined, $"🚧工事中🚧" },
			{ Error.UnknownCommand, $"すみません、上手く聞き取れませんでした" },
			{ Error.NotEnoughUsers, $"*しかし誰も来なかった" },
		};
		public static IList<Type> ChooseRandom<Type>(IList<Type> ronly , uint number = 1)
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
	}
}

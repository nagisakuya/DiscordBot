using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Rest;
using Discord.Commands;
using static discord_bot.Utility;
using static discord_bot.Program;


namespace discord_bot
{
	class VoiceChannel
	{
		protected internal RestVoiceChannel voice_channel;

		//this constructor is heavy
		protected VoiceChannel(SocketGuild guild, Action<VoiceChannelProperties> func = null, string name = "VoiceChannel")
		{
			voice_channel = guild.CreateVoiceChannelAsync(name, func).Result;
			Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelEvent);
		}
		public void CancelEvent(object sender, ConsoleCancelEventArgs args)
		{
			Delete().Wait(100);
		}
		public async virtual Task Delete()
		{
			await voice_channel?.DeleteAsync();
			Console.CancelKeyPress -= new ConsoleCancelEventHandler(CancelEvent);
		}
		public IList<SocketGuildUser> SpeakingUsersInGuild()
		{
			return SpeakingUsersInTheGuild(client.GetGuild(voice_channel.GuildId));
		}
		protected async Task Attract(SocketGuildUser target)
		{
			if (target.VoiceState.HasValue && target.Guild.Id == voice_channel.GuildId && target.VoiceState.Value.VoiceSessionId != voice_channel.Id.ToString())
			{
				await target.ModifyAsync((t) => t.Channel = voice_channel);
			}
		}
		protected async Task Attract(IList<SocketGuildUser> targets)
		{
			for (int i = 0; i < targets.Count; i++)
			{
				await Attract(targets[i]);
			}
		}
		protected async Task Attract(SocketUser user, SocketVoiceState before, SocketVoiceState after)
		{
			if (after.VoiceChannel.Id != voice_channel.Id)
			{
				await Attract(user as SocketGuildUser);
			}
		}
		protected async Task DeleteIfEmpty(SocketUser user, SocketVoiceState before, SocketVoiceState after)
		{
			if (before.VoiceChannel.Id == voice_channel.Id && after.VoiceChannel.Id != voice_channel.Id)
			{
				if (await (voice_channel as IGuildChannel).GetUsersAsync().CountAsync() == 0)
				{
					await Delete();
				}
			}

		}
	}
	class SexRoom : VoiceChannel
	{
		private const string NAME = "〇ックスしないと出られない部屋";
		protected SexRoom(SocketGuild guild, Action<VoiceChannelProperties> func = null) : base(guild, func, NAME)
		{
			client.UserVoiceStateUpdated += DeleteIfEmpty;
		}
		public static async Task<SexRoom> Construct(ISocketMessageChannel text_channel, SocketGuild guild)
		{
			var speaking_users = SpeakingUsersInTheGuild(guild);
			if (speaking_users.Count <= 0)
			{
				_ = text_channel.SendError(Error.NotEnoughUsers);
				return null;
			}
			var targets = speaking_users.Count == 1
				? ChooseRandom(speaking_users)
				: ChooseRandom(speaking_users, 2);

			var new_channel = await Task.Run(()=>{
			return new SexRoom(guild, channel => { channel.CategoryId = targets[0].VoiceChannel.CategoryId; channel.UserLimit = 2; });
			});
			await new_channel.Attract(targets);
			return new_channel;
		}
		public async override Task Delete()
		{
			client.UserVoiceStateUpdated -= DeleteIfEmpty;
			await base.Delete();
		}
	}
	class Blackhole : VoiceChannel
	{
		static internal Dictionary<ulong, Blackhole> active_blackholes = new() { };
		private const string NAME = "ブラックホール";
		protected Blackhole(SocketGuild guild, Action<VoiceChannelProperties> func = null) : base(guild, func, NAME)
		{
			client.UserVoiceStateUpdated += Attract;
			active_blackholes.Add(guild.Id, this);
		}
		public static async Task<Blackhole> Construct(ISocketMessageChannel text_channel, SocketGuild guild, SocketGuildUser caller)
		{
			if (active_blackholes.ContainsKey(guild.Id))
			{
				_ = text_channel.SendError(Error.FizzedOut);
				return null;
			}
			var category = caller.VoiceChannel?.Category?.Id;
			var new_channel = await Task.Run(() => {
				return new Blackhole(guild, channel => { channel.CategoryId = category;});
			});
			await new_channel.Attract(guild.Users.ToList());
			return new_channel;
		}
		public void Disable()
		{
			client.UserVoiceStateUpdated -= Attract;
			client.UserVoiceStateUpdated += DeleteIfEmpty;
		}
		public async override Task Delete()
		{
			active_blackholes.Remove(voice_channel.GuildId);
			client.UserVoiceStateUpdated -= Attract;
			client.UserVoiceStateUpdated -= DeleteIfEmpty;
			await base.Delete();
		}
	}
	class Whitehole : VoiceChannel
	{
		private const string NAME = "本棚の裏";
		protected Whitehole(SocketGuild guild, Action<VoiceChannelProperties> func = null) : base(guild, func, NAME)
		{
			client.UserVoiceStateUpdated += DeleteIfEmpty;
		}
		public static async Task<Whitehole> Construct(ISocketMessageChannel text_channel, SocketGuild guild, SocketGuildUser caller)
		{
			if (!Blackhole.active_blackholes.TryGetValue(guild.Id, out Blackhole blackhole))
			{
				_ = text_channel.SendError(Error.FizzedOut);
				return null;
			}
			var new_channel = await Task.Run(() => {
				return new Whitehole(guild, channel => { channel.CategoryId = blackhole.voice_channel.CategoryId; });
			});
			blackhole.Disable();
			await new_channel.Attract(guild.Users.ToList());
			return new_channel;
		}
		public async override Task Delete()
		{
			client.UserVoiceStateUpdated -= DeleteIfEmpty;
			await base.Delete();
		}
	}
}

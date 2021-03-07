using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Rest;
using static discord_bot.Utility;


namespace discord_bot
{
	class VoiceChannel
	{
		protected internal RestVoiceChannel voice_channel;
		protected VoiceChannel() { }
		public static async Task<VoiceChannel> Construct(SocketGuild guild,Action<VoiceChannelProperties> func = null, string name = "VoiceChannel")
		{
			VoiceChannel @new = new();
			@new.voice_channel = await guild.CreateVoiceChannelAsync(name, func);
			Console.CancelKeyPress += new ConsoleCancelEventHandler(@new.Delete);
			return @new;
		}
		public void Delete(object sender, ConsoleCancelEventArgs args)
		{
			_ = Delete();
		}
		public async virtual Task Delete()
		{
			await voice_channel.DeleteAsync();
			Console.CancelKeyPress -= new ConsoleCancelEventHandler(Delete);
		}
		public IList<SocketGuildUser> SpeakingUsersInGuild()
		{
			return SpeakingUsersInTheGuild(Program.client.GetGuild(voice_channel.GuildId));
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
		protected SexRoom() { }
		protected SexRoom(VoiceChannel from) { voice_channel = from.voice_channel; }
		public static async Task<SexRoom> Construct(ISocketMessageChannel text_channel, SocketGuild guild)
		{
			var speaking_users = SpeakingUsersInTheGuild(guild);
			if (speaking_users.Count <= 0)
			{
				await SendError(text_channel, Error.NotEnoughUsers);
				return null;
			}
			var targets = speaking_users.Count == 1
				? ChooseRandom(speaking_users)
				: ChooseRandom(speaking_users, 2);
			var new_channel = new SexRoom(await VoiceChannel.Construct(guild, prop => {prop.CategoryId = targets[0].VoiceChannel.CategoryId;prop.UserLimit = 2; }, NAME)) ;
			await new_channel.Attract(targets);
			Program.client.UserVoiceStateUpdated += new_channel.DeleteIfEmpty;
			return new_channel;
		}
		public async override Task Delete()
		{
			Program.client.UserVoiceStateUpdated -= DeleteIfEmpty;
			await base.Delete();
		}
	}
	class Blackhole : VoiceChannel
	{
		static internal Dictionary<ulong, Blackhole> active_blackholes = new() { };
		private const string NAME = "ブラックホール";
		protected Blackhole() { }
		protected Blackhole(VoiceChannel from) { voice_channel = from.voice_channel; }
		public static async Task<Blackhole> Construct(ISocketMessageChannel text_channel, SocketGuild guild ,SocketGuildUser caller)
		{
			if (active_blackholes.ContainsKey(guild.Id))
			{
				await SendError(text_channel, Error.FizzedOut);
				return null;
			}
			var category = caller?.VoiceChannel?.Category?.Id;
			var new_channel = new Blackhole(await VoiceChannel.Construct(guild, prop => { prop.CategoryId = category; }, NAME));
			active_blackholes.Add(guild.Id, new_channel);
			await new_channel.Attract(guild.Users.ToList());
			Program.client.UserVoiceStateUpdated += new_channel.Attract;
			return new_channel;
		}
		public void Disable()
		{
			Program.client.UserVoiceStateUpdated -= Attract;
			Program.client.UserVoiceStateUpdated += DeleteIfEmpty;
		}
	public async override Task Delete()
		{
			active_blackholes.Remove(voice_channel.GuildId);
			Program.client.UserVoiceStateUpdated -= Attract;
			Program.client.UserVoiceStateUpdated -= DeleteIfEmpty;
			await base.Delete();
		}
	}
	class Whitehole : VoiceChannel
	{
		private const string NAME = "本棚の裏";
		protected Whitehole() { }
		protected Whitehole(VoiceChannel from) { voice_channel = from.voice_channel; }
		public static async Task<Whitehole> Construct(ISocketMessageChannel text_channel, SocketGuild guild, SocketGuildUser caller)
		{
			if (!Blackhole.active_blackholes.TryGetValue(guild.Id,out Blackhole blackhole))
			{
				await SendError(text_channel, Error.FizzedOut);
				return null;
			}
			var new_whitehole = new Whitehole(await VoiceChannel.Construct(guild, prop => { prop.CategoryId = blackhole.voice_channel.CategoryId; }, NAME));
			blackhole.Disable();
			await new_whitehole.Attract(guild.Users.ToList());
			Program.client.UserVoiceStateUpdated += new_whitehole.DeleteIfEmpty;
			return new_whitehole;
		}
		public async override Task Delete()
		{
			Program.client.UserVoiceStateUpdated -= DeleteIfEmpty;
			await base.Delete();
		}
	}
}

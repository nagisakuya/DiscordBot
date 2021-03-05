using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Rest;


namespace discord_bot
{
	class VoiceChannel
	{
		private RestVoiceChannel channel;
		private VoiceChannel() { }
		public static async Task<VoiceChannel> Construct(SocketGuild guild, SocketCategoryChannel category = null, string name = "VoiceChannel")
		{
			VoiceChannel @new = new();
			@new.channel = await guild.CreateVoiceChannelAsync(name,(prop)=> {
				prop.CategoryId = category?.Id;
			});
			return @new;
		}
		public async void Delete()
		{
			await channel.DeleteAsync();
		}
		~VoiceChannel()
		{
			Delete();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Audio;
using Discord.Rest;
using static discord_bot.Utility;

namespace discord_bot
{
	class VoiceClient
	{
		protected static internal IAudioClient voice_client;
		protected VoiceClient(IAudioClient new__client) { voice_client = new__client; }
		public static async Task Construct(SocketGuildUser caller, string name = "watchinpo")
		{
			if (caller.VoiceState.HasValue)
			{
				VoiceClient new_client = new(await caller.VoiceState.Value.VoiceChannel.ConnectAsync()); ;
			}
		}
		public void Delete(object sender, ConsoleCancelEventArgs args)
		{
			_ = Delete();
		}
		public async virtual Task Delete()
		{
		}
	}
}

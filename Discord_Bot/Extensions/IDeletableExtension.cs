using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Discord_Bot.Extensions
{
	public static class IDeletableExtensions
	{
		public static async Task<bool> TryDeleteAsync(this IDeletable msg, IMessageChannel channel, BaseSocketClient discord)
		{
			if (channel is not SocketGuildChannel guildChannel || !guildChannel.Guild.GetUser(discord.CurrentUser.Id).GuildPermissions.ManageMessages)
				return false;

			await msg.DeleteAsync();
			return true;
		}
	}
}

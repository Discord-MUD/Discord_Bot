using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Bot.Extensions;
using Discord_Bot.Bot.Services.Interaction;
using System.Threading.Tasks;

namespace Discord_Bot.Bot
{
	public class MainModule<T> : InteractiveBase<T>
		where T : SocketCommandContext
	{
		public DiscordSocketClient Discord { get; set; }

		protected static void DeleteMessageDelay(int timeout, params IMessage[] messages)
		{
			_ = Task.Delay(timeout).ContinueWith(async _ =>
			{
				foreach (IMessage msg in messages)
					await msg.DeleteAsync();
			});
			return;
		}
		protected void TryDeleteMessageDelay(int timeout, params IMessage[] messages)
		{
			_ = Task.Delay(timeout).ContinueWith(async _ =>
			{
				foreach (IMessage msg in messages)
					await msg.TryDeleteAsync(msg.Channel, Discord);
			});
			return;
		}
		protected static void TryDeleteMessageDelayStatic(BaseSocketClient discord, int timeout, params IMessage[] messages)
		{
			_ = Task.Delay(timeout).ContinueWith(async _ =>
			{
				foreach (IMessage msg in messages)
					await msg.TryDeleteAsync(msg.Channel, discord);
			});
			return;
		}
		protected async Task SimpleReply(IUserMessage msgToReply, string msgText)
		{
			IMessage msg = await msgToReply.ReplyAsync(msgText);
			TryDeleteMessageDelay(10000, msg, msgToReply);
		}
	}
}

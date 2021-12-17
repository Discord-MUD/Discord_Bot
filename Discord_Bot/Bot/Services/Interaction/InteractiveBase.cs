using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Discord_Bot.Bot.Services.Interaction
{
	public abstract class InteractiveBase : InteractiveBase<SocketCommandContext>
	{ }

	public abstract class InteractiveBase<T> : ModuleBase<T>
		where T : SocketCommandContext
	{
		private static InteractiveBase<T> Instance;
		public InteractionService Interaction { get; set; }

		private static bool init = false;
		public InteractiveBase()
		{
			if (init)
				return;
			init = true;
			Instance = this;
		}

		protected async Task<SocketMessage> GetReply(bool fromSourceUser = true,
			bool inSourceChannel = true,
			TimeSpan? timeout = null,
			CancellationTokenSource tokenSrc = null)
			=> await Interaction.GetReply(Context, fromSourceUser, inSourceChannel, timeout, tokenSrc);

		protected static async Task<SocketMessage> GetReply(Func<SocketMessage, Task<bool>> validateCallback,
			TimeSpan? timeout = null,
			CancellationTokenSource tokenSrc = null)
			=> await Instance.Interaction.GetReply(validateCallback, timeout, tokenSrc);
	}
}

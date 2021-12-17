using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Discord_Bot.Bot.Services.Interaction
{
	public class InteractionService : IDisposable
	{
		public BaseSocketClient Discord { get; }

		private readonly TimeSpan defaultTimeout;


		public InteractionService(DiscordSocketClient discord, TimeSpan? defaultTimespan = null)
			: this((BaseSocketClient)discord, defaultTimespan) { }
		public InteractionService(DiscordShardedClient discord, TimeSpan? defaultTimespan = null)
			: this((BaseSocketClient)discord, defaultTimespan) { }

		public InteractionService(BaseSocketClient discord, TimeSpan? defaultTimespan = null)
		{
			Discord = discord;
			defaultTimeout = defaultTimespan ?? new TimeSpan(0, 5, 0);
		}

		public async Task<SocketMessage> GetReply(SocketCommandContext context,
			bool fromSourceUser = true,
			bool inSourceChannel = true,
			TimeSpan? timeout = null,
			CancellationTokenSource tokenSrc = null)
		{
			Task<bool> Validator(SocketMessage message)
			{
				if (fromSourceUser)
					if (message.Author.Id != context.User.Id) return Task.FromResult(false);
				if (inSourceChannel)
					if (message.Channel.Id != context.Channel.Id) return Task.FromResult(false);

				return Task.FromResult(true);
			}

			return await GetReply(Validator, timeout, tokenSrc);
		}

		public async Task<SocketMessage> GetReply(Func<SocketMessage, Task<bool>> validateCallback,
		TimeSpan? timeout = null,
		CancellationTokenSource tokenSrc = null)
		{
			timeout ??= defaultTimeout;

			TaskCompletionSource<SocketMessage> eventTrigger = new();
			TaskCompletionSource<bool> cancelTrigger = new();

			if (tokenSrc != null)
				tokenSrc.Token.Register(() => cancelTrigger.SetResult(true));

			async Task Handler(SocketMessage message)
			{
				if (await validateCallback(message)) // validateCallback is a function provided that will validate whether the reply is valid
					eventTrigger.SetResult(message);
			}

			Discord.MessageReceived += Handler;

			Task task = await Task.WhenAny(eventTrigger.Task, cancelTrigger.Task, Task.Delay(timeout.Value)); // wait for either a valid reply, a cancel trigger or a timeout

			Discord.MessageReceived -= Handler;

			if (task == eventTrigger.Task)
				return await eventTrigger.Task;
			return null;
		}

		
		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}
	}
}

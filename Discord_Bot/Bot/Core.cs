using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Discord_Bot.Extensions;

namespace Discord_Bot.Bot
{
	public class Core
	{
		private readonly ServiceProvider services;

		public Core()
		{
			DiscordSocketConfig socketConfig = new()
			{
#if DEBUG
				LogLevel = LogSeverity.Verbose
#else
				LogLevel = LogSeverity.Warning
#endif
			};

			services = new ServiceCollection()
				.AddSingleton(new DiscordSocketClient(socketConfig))
				.AddSingleton<CommandService>()
				.BuildServiceProvider();
		}

		public async Task StartBot(Settings settings)
		{
			GlobalSettings.prefix = settings.botPrefix;

			DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();
			CommandService command = services.GetRequiredService<CommandService>();

			client.Log += LogMsg; // Add logging callbacks
			command.Log += LogMsg;

			client.MessageReceived += OnMessageRecievedEvent;
			command.CommandExecuted += OnCommandExecutedEvent; // add message and command executed callbacks

#if DEBUG
			if (settings.botToken_dev != null)
				await client.LoginAsync(TokenType.Bot, settings.botToken_dev); // use the dev token if available, intended for having dev-instances. This is only compiled in debug mode
			else
				await client.LoginAsync(TokenType.Bot, settings.botToken);
#else
            await client.LoginAsync(TokenType.Bot, settings.botToken);
#endif
			await client.StartAsync();

			static Task ReadyHandler()
			{
				GlobalSettings.ready = true;
				return Task.CompletedTask;
			}
			client.Ready += ReadyHandler;
			while (!GlobalSettings.ready)
				await Task.Delay(5);
			client.Ready -= ReadyHandler;

			await command.AddModulesAsync(Assembly.GetEntryAssembly(), services); // this is a function that gathers all command modules. It is a part of Discord.NET

			await Task.Delay(Timeout.Infinite); // just wait forever since the bot is now running
		}

		private Task LogMsg(LogMessage msg)
		{
			Console.WriteLine(msg);
			return Task.CompletedTask;
		}

		private async Task OnMessageRecievedEvent(SocketMessage msg)
		{
			if (msg is not SocketUserMessage uMsg) return;
			if (uMsg.Source != MessageSource.User) return;

			int argPos = 0;

			if (!uMsg.HasStringPrefix(GlobalSettings.prefix, ref argPos)) return;

			SocketCommandContext context = new(services.GetRequiredService<DiscordSocketClient>(), uMsg);

			await services.GetRequiredService<CommandService>().ExecuteAsync(context, argPos, services);
		}


		private async Task OnCommandExecutedEvent(Optional<CommandInfo> command, ICommandContext context, IResult result)
		{
			switch (result)
			{
				case CommandResult commandResult:
					if (commandResult.errorType != CommandResult.ErrorType.Success)
						await HandleCommandResultError(command, context, commandResult);
					break;

				default:
					if (!result.IsSuccess)
						await HandleDefaultCommandError(command, context, result);
					break;
			}
		}

		private async Task HandleCommandResultError(Optional<CommandInfo> command, ICommandContext context, CommandResult result)
		{
			long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

			IMessage msg = null;
			switch (result.errorType)
			{
				case CommandResult.ErrorType.Generic:
					msg = await context.Message.ReplyAsync($"Something went wrong! Error (Code: {timestamp}). Please report me to the owner!");
					Console.WriteLine($"({timestamp})\n\t" +
						$"Message: {context.Message.Content.Replace("\t", "\t\t").Replace("\n", "\n\t")}\n\t" +
						$"Command Result: {result.errorType}\n\t" +
						$"Error Reason: {result.Reason.Replace("\t", "\t\t").Replace("\n", "\n\t")}");
					break;

				case CommandResult.ErrorType.NotEnoughArgs:
					msg = await context.Message.ReplyAsync(result.Reason);
					break;

				case CommandResult.ErrorType.BadArgs:
					msg = await context.Message.ReplyAsync(result.Reason);
					break;
			}
			_ = Task.Delay(10000).ContinueWith(async (Task _) =>
			{
				await msg.DeleteAsync();
				await context.Message.TryDeleteAsync(context.Message.Channel, services.GetRequiredService<DiscordSocketClient>());
			});
		}

		private async Task HandleDefaultCommandError(Optional<CommandInfo> command, ICommandContext context, IResult result)
		{
			long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

			IMessage msg = null;
			bool rmv = true;
			switch (result.Error)
			{
				case CommandError.UnmetPrecondition:
					msg = await context.Message.ReplyAsync(result.ErrorReason);
					break;

				case CommandError.UnknownCommand:
					msg = await context.Message.ReplyAsync("I don't know that one! Use " + GlobalSettings.prefix + "help for a list of commands!");
					break;

				case CommandError.BadArgCount:
					break;

				case CommandError.Exception:
					msg = await context.Message.ReplyAsync($"Error (Code: {timestamp}), Please report me to the owner!");
					Logger.WriteLineErr($"({timestamp})\n\t" +
						$"Message: {context.Message.Content.Replace("\t", "\t\t").Replace("\n", "\n\t")}\n\t" +
						$"Error: {result.Error.ToString().Replace("\t", "\t\t").Replace("\n", "\n\t")}\n\t" +
						$"Error Reason: {result.ErrorReason.Replace("\t", "\t\t").Replace("\n", "\n\t")}");
					rmv = false;
					break;

				default:
					msg = await context.Message.ReplyAsync($"Error (Code: {timestamp}): {result.Error}");
					Logger.WriteLineErr($"({timestamp})\n\t" +
						$"Message: {context.Message.Content.Replace("\t", "\t\t").Replace("\n", "\n\t")}\n\t" +
						$"Error: {result.Error.ToString().Replace("\t", "\t\t").Replace("\n", "\n\t")}\n\t" +
						$"Error Reason: {result.ErrorReason.Replace("\t", "\t\t").Replace("\n", "\n\t")}");
					rmv = false;
					break;
			}
			if (rmv)
			{
				_ = Task.Delay(10000).ContinueWith(async (Task _) =>
				{
					await msg.DeleteAsync();
					await context.Message.TryDeleteAsync(context.Message.Channel, services.GetRequiredService<DiscordSocketClient>());
				});
			}
		}
	}
}

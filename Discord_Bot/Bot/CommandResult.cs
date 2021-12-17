using Discord.Commands;

namespace Discord_Bot.Bot
{
	public class CommandResult : RuntimeResult
	{
		public enum ErrorType
		{
			Success,
			Generic,
			NotEnoughArgs,
			BadArgs
		}

		public ErrorType errorType;

		public CommandResult(CommandError? error, ErrorType errorType, string reason) : base(error, reason)
		{
			this.errorType = errorType;
		}
		public static CommandResult Success(string reason = null) => new(null, ErrorType.Success, reason);
		public static CommandResult Generic(string reason) => new(CommandError.Unsuccessful, ErrorType.Generic, reason);
		public static CommandResult NotEnoughArgs(string msg) => new(CommandError.Unsuccessful, ErrorType.NotEnoughArgs, msg);
		public static CommandResult BadArgs(string msg) => new(CommandError.Unsuccessful, ErrorType.BadArgs, msg);
	}
}

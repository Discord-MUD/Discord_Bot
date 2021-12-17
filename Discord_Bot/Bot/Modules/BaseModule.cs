using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Discord_Bot.Extensions;

namespace Discord_Bot.Bot.Modules
{
	[ModuleInfo("📋 Core", moduleDescription = "These are the core commands to the bot.")]
	public class BaseModule : MainModule<SocketCommandContext>
	{
		public struct ModuleInfo
		{
			public string moduleName;
			public string moduleDescription;
			public string[] moduleCommands;
		}

		public readonly Dictionary<string, string[]> AllCommands = new();
		public readonly Dictionary<string, ModuleInfo> ModuleInfos = new();

		public BaseModule()
		{
			Type[] modules = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(MainModule<SocketCommandContext>).IsAssignableFrom(t)).ToArray(); // get all modules in the assembly

			var tmp = new Dictionary<string, List<string>>(); // stores all commands per module
			var tmp2 = new Dictionary<string, string>(); // stores descriptions per module

			foreach (Type module in modules)
			{
				// this block is to fill out AllCommands
				var modInfo = module.GetCustomAttribute(typeof(ModuleInfoAttribute)) as ModuleInfoAttribute; // get the module info. This should be specified for each module and if it isn't, this will cause errors on startup. This is by design.
				if (!tmp.ContainsKey(modInfo.moduleName))
					tmp[modInfo.moduleName] = new(); // if the name wasn't encountered before, make a new list in the dictionary. This is to handle a module being split into different classes/files. If they share names, they are the same module.
				tmp[modInfo.moduleName].AddRange(module.GetMethods()
					.Where(m => m.GetCustomAttribute(typeof(CommandAttribute), false) != null) // select all methods with a command attribute
					.Select(m => ((CommandAttribute)m.GetCustomAttribute(typeof(CommandAttribute), false)).Text) // select the text part of the commmand attribute (The command name)
					.Distinct()); // make sure they are distinct since you can have copies of commands with different args

				// this is to fill out ModuleReferences
				if (modInfo.moduleDescription != null)
					tmp2[modInfo.moduleName] = modInfo.moduleDescription;
			}

			foreach (string key in tmp.Keys)
			{
				AllCommands[key] = tmp[key].ToArray();
			}

			foreach (string key in tmp2.Keys)
			{
				ModuleInfo info;

				info.moduleName = key;
				info.moduleDescription = tmp2[key];
				info.moduleCommands = tmp[key].ToArray();

				ModuleInfos[key] = info;
			}
		}


		[Command("Ping")]
		public async Task Ping()
		{
			await SimpleReply(Context.Message, "Pong!");
		}

		[Command("Help", RunMode = RunMode.Async)]
		public async Task Help()
		{
			Type[] modules = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(MainModule<SocketCommandContext>).IsAssignableFrom(t)).ToArray();

			EmbedBuilder embed = new()
			{
				Title = "NekoMon Commands",
				Description = "This is all dynamic!",
				ThumbnailUrl = Discord.CurrentUser.GetAvatarUrl(),
				Footer = new EmbedFooterBuilder()
				{
					Text = $"Unsure how to use the interface? Do {GlobalSettings.prefix}help help for more info"
				}
			};

			ModuleInfo[] modInfoArr = new ModuleInfo[ModuleInfos.Count];
			{ // we need to turn the module infos into an array so a selection index can be used
				int i = 0;
				foreach (ModuleInfo info in ModuleInfos.Values)
				{
					modInfoArr[i++] = info;
				}
			}

			EmbedFieldBuilder[] embedListFields = new EmbedFieldBuilder[ModuleInfos.Count];
			for (int i = 0; i < ModuleInfos.Count; i++)
			{
				embedListFields[i] = new EmbedFieldBuilder
				{
					Name = modInfoArr[i].moduleName,
					Value = $"[Hover for Info](https://www.google.com \"{modInfoArr[i].moduleCommands.Length} commands\")"
					// slightly hacky but for hover messages to work, we need a link. We could put a garbage link but that risks linking to a problematic website so we just use google
				};
			}

			const string indctr = " 🔸";
			embedListFields[0].Name += indctr; // put the indicator on the first element as this is selected by default

			embed.WithFields(embedListFields);

			IUserMessage msg = await Context.Message.ReplyAsync(embed: embed.Build()); // send the embed


			int selected = 0;
			bool zoomed = false;


			/*Dictionary<string, IEmote> choices = new()
			{
				["up"] = new Emoji("⬆️"),
				["down"] = new Emoji("⬇️"),
				["zoom"] = new Emoji("⏺️"),
				["stop"] = new Emoji("⏹️"),
				["top"] = new Emoji("⏫"),
				["bottom"] = new Emoji("⏬")
			}; // the order of this list matters as its how they appear on the message. Emojis also take a while to add so the most important controls should be placed first.
			await AddReactionCallback(msg, new ReactionCallback()
			{
				runMode = RunMode.Async,
				timeout = new TimeSpan(0, 5, 0),
				reactionChoices = choices.Values.ToArray(),

				HandleTimeout = HandleTimeout,
				ValidateReaction = ValidateReaction,
				HandleCallback = HandleCallback
			});

			async Task HandleTimeout()
			{
				await msg.DeleteAsync();
				await Context.Message.TryDeleteAsync(Context.Channel, Discord);
			}
			Task<bool> ValidateReaction(SocketReaction reaction)
			{
				if (reaction.MessageId != msg.Id) return Task.FromResult(false); // the validation is simply "Is it the same user as the command initiator on the right message"
				if (reaction.UserId != Context.User.Id) return Task.FromResult(false);

				return Task.FromResult(true);
			}
			async Task HandleCallback(SocketReaction reaction)
			{
				IEmote emote = reaction.Emote;

				if (!zoomed)
				{
					if (emote.Equals(choices["up"]) && selected > 0)
					{
						embedListFields[selected].Name = embedListFields[selected].Name.Replace(indctr, "");
						selected--; // change selection up the list
						embedListFields[selected].Name += indctr;
					}
					else if (emote.Equals(choices["top"]))
					{
						embedListFields[selected].Name = embedListFields[selected].Name.Replace(indctr, "");
						selected = 0; // change selection to the first element
						embedListFields[selected].Name += indctr;
					}
					else if (emote.Equals(choices["down"]) && selected < embedListFields.Length - 1)
					{
						embedListFields[selected].Name = embedListFields[selected].Name.Replace(indctr, "");
						selected++; // change selection down the list
						embedListFields[selected].Name += indctr;
					}
					else if (emote.Equals(choices["bottom"]))
					{
						embedListFields[selected].Name = embedListFields[selected].Name.Replace(indctr, "");
						selected = embedListFields.Length - 1; // change selection to the last element
						embedListFields[selected].Name += indctr;
					}
					else if (emote.Equals(choices["zoom"]))
					{
						ToggleZoomView();
						zoomed = true; // zoom into selection (view module info)
					}
				}
				else
				{
					if (emote.Equals(choices["zoom"]))
					{
						ToggleZoomView();
						zoomed = false; // go back to the module list view
					}
				}

				if (emote.Equals(choices["stop"]))
				{
					RemoveReactionCallback(msg); // remove the reaction callback
					await HandleTimeout();
					return;
				}
				await msg.ModifyAsync(m => m.Embed = embed.Build());
			}

			void ToggleZoomView()
			{
				if (!zoomed)
				{
					embed.Title = modInfoArr[selected].moduleName;
					embed.Description = $"{modInfoArr[selected].moduleDescription}\n`{string.Join("`, `", modInfoArr[selected].moduleCommands)}`";
					embed.Fields.Clear();
					embed.Footer = new EmbedFooterBuilder()
					{
						Text = $"Use {Utils.GlobalSettings.prefix}help [command] for more info"
					};
				}
				else
				{
					embed.Title = "NekoMon Commands";
					embed.Description = "This is all dynamic!";
					embed.WithFields(embedListFields);
					embed.Footer = new EmbedFooterBuilder()
					{
						Text = $"Unsure how to use the interface? Do {GlobalSettings.prefix}help help for more info"
					};
				}
			}*/
		}

		[Command("Help", true, RunMode = RunMode.Async)]
		public async Task Help(string command)
		{
			command = command.ToLower();

			string module = null; // module stores the module the command was found in. For validating the command and also for display
			string commandReal = ""; // command real stores the non-lowered string of the command for display
			foreach (var pair in AllCommands)
			{
				var pairLower = pair.Value.Select(v => v.ToLower());
				if (pairLower.Contains(command))
				{
					module = pair.Key;
					foreach (string cmd in pair.Value)
						if (cmd.ToLower() == command)
						{
							commandReal = cmd;
							break;
						}
					break;
				}
			}

			if (module == null)
			{ // if module was null, we didn't find a command that was specified
				await SimpleReply(Context.Message, $"I don't know that one! Use {GlobalSettings.prefix}help for a list of commands!");
				return;
			}

			string helpdocFile = null;
			{
				string[] helpdocs = Directory.GetFiles("helpdocs").Where(f => Path.GetFileNameWithoutExtension(f).ToLower() == command).Select(f => Path.GetFileName(f)).ToArray();
				if (helpdocs.Length > 0)
					helpdocFile = helpdocs[0];
			}
			if (helpdocFile == null)
			{ // check if the helpdoc exists. If it doesn't, then one needs adding
				await SimpleReply(Context.Message, "Help info missing, please contact the developer.");
				Logger.WriteLineErr($"{command} helpdoc missing");
				return;
			}

			string[] helpInfoRaw;
			{
				FileStream stream = File.Open($"helpdocs/{helpdocFile}", FileMode.Open, FileAccess.Read);
				StreamReader reader = new(stream);
				helpInfoRaw = reader.ReadToEnd().Split("\r\n||\r\n").Select(s => s
					.Replace("$${}$$", GlobalSettings.prefix)
					.Replace("$${cmd}$$", commandReal) // all of these replaces are a very rudametry parsing system to implement runtime values into the helpdoc.
					.Replace("$${cmdFull}$$", GlobalSettings.prefix + commandReal)).ToArray();
				reader.Close();
				stream.Close();
			}

			EmbedBuilder embed = new()
			{
				Title = commandReal + " | " + module,
				Description = helpInfoRaw[0],
				ThumbnailUrl = Discord.CurrentUser.GetAvatarUrl()
			};
			EmbedFieldBuilder[] fields = new EmbedFieldBuilder[(helpInfoRaw.Length - 1) / 2];
			for (int i = 0; i < fields.Length; i++)
			{
				fields[i] = new EmbedFieldBuilder()
				{
					Name = helpInfoRaw[1 + (i * 2)],
					Value = helpInfoRaw[2 + (i * 2)]
				};
			}
			embed.WithFields(fields);

			IUserMessage msg = await Context.Message.ReplyAsync(embed: embed.Build());

			/*Dictionary<string, IEmote> choices = new()
			{
				["stop"] = new Emoji("⏹️")
			};
			await AddReactionCallback(msg, new ReactionCallback()
			{
				runMode = RunMode.Async,
				timeout = new TimeSpan(0, 5, 0),
				reactionChoices = choices.Values.ToArray(),

				HandleTimeout = HandleTimeout,
				ValidateReaction = ValidateReaction,
				HandleCallback = HandleCallback
			});

			async Task HandleTimeout()
			{
				await msg.DeleteAsync();
				await Context.Message.TryDeleteAsync(Context.Channel, Discord);
			}
			Task<bool> ValidateReaction(SocketReaction reaction)
			{
				if (reaction.MessageId != msg.Id) return Task.FromResult(false);
				if (reaction.UserId != Context.User.Id) return Task.FromResult(false);

				return Task.FromResult(true);
			}
			async Task HandleCallback(SocketReaction reaction)
			{
				if (reaction.Emote.Equals(choices["stop"]))
				{
					RemoveReactionCallback(msg);
					await HandleTimeout();
				}
			}*/
		}
	}
}

﻿using Discord;
using Discord.Commands;
using FlexLabs.EDAssistant.DiscordBot.Commands;
using FlexLabs.EDAssistant.DiscordBot.Extensions;
using FlexLabs.EDAssistant.Services.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexLabs.EDAssistant.DiscordBot
{
    public class Bot
    {
        private DiscordClient _client;

        public static IServiceProvider ServiceProvider { get; private set; }
        public static string ClientID { get; private set; }
        public static DateTime Started { get; private set; }
        public static Dictionary<ulong, string> ServerPrefixes { get; } = new Dictionary<ulong, string>();

        public Bot(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            _client = new DiscordClient(x =>
            {
                x.AppName = "Elite Dangerous Assistant Bot";
                x.AppVersion = Program.GetVersion();
            });

            _client.UsingCommands(x =>
            {
                x.HelpMode = HelpMode.Disabled;
                x.CustomPrefixHandler = m =>
                {
                    if (m.Channel.IsPrivate || m.Server == null) return 0;

                    var prefix = GetServerPrefix(m.Server.Id);
                    if (prefix == null || !m.Text.StartsWith(prefix))
                        return -1;

                    return prefix.Length;
                };
                x.ErrorHandler = async (s, e) =>
                {
                    var message = e.Message.RawText
                        .Replace(_client.CurrentUser.Mention, "")
                        .Replace(_client.CurrentUser.NicknameMention, "");
                    await Luis.LuisProcessor.Process(e.Channel, message);
                };
            });

            var commandService = _client.GetService<CommandService>();
            commandService.CreateCommand("help")
                .Hide()
                .Parameter("public", ParameterType.Multiple)
                .Do(Command_Help);

            commandService.CreateCommands_SetPrefix();

            commandService.CreateCommands_Welcome(_client);

            commandService.CreateCommands_Time();

            commandService.CreateCommands_KosRules();

            commandService.CreateCommands_Eddb();

            commandService.CreateCommands_Inara();

            commandService.CreateCommands_About();
        }

        public void Start(string botToken, string clientID)
        {
            ClientID = clientID;
            Started = DateTime.UtcNow;
            _client.ExecuteAndWait(async () =>
            {
                await _client.Connect(botToken, TokenType.Bot);
            });
        }

        private string GetServerPrefix(ulong serverID)
        {
            if (ServerPrefixes.ContainsKey(serverID))
                return ServerPrefixes[serverID];

            using (var serversService = ServiceProvider.GetService(typeof(ServersService)) as ServersService)
            {
                var server = serversService.Load(serverID);
                var commandPrefix = server?.CommandPrefix;

                if (commandPrefix != null)
                    ServerPrefixes[serverID] = commandPrefix;

                return commandPrefix;
            }
        }

        public static bool Check_PublicChannel(Command cmd, User u, Channel ch) => !ch.IsPrivate;
        public static bool Check_IsServerAdmin(Command cmd, User u, Channel ch) => !ch.IsPrivate && u.ServerPermissions.Administrator;

        private async Task Command_Help(CommandEventArgs e)
        {
            var all = (e.Channel.IsPrivate || e.User.ServerPermissions.Administrator) && e.Args?.Length > 0 && e.Args.Any(a => String.Equals("all", a, StringComparison.OrdinalIgnoreCase));
            var reveal = e.Channel.IsPrivate && e.Args?.Length > 0 && e.Args.Any(a => String.Equals("reveal", a, StringComparison.OrdinalIgnoreCase));

            var commandService = _client.GetService<CommandService>();
            var commands = commandService.AllCommands.OfType<Command>()
                .Where(c => reveal || !c.IsHidden)
                .Where(c => all || !c.IsModCommand())
                .Where(c =>
                {
                    var method = typeof(Command).GetMethod("CanRun", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        try
                        {
                            var canRun = (bool)method.Invoke(c, new object[] { e.User, e.Channel, null });
                            return canRun;
                        }
                        catch { }
                    }
                    return true;
                });
            var commandsHelp = commands.Select(command =>
            {
                var arguments = command.Parameters.Any()
                    ? " " + string.Join(" ", command.Parameters.Select(a => $"{{{a.Name}}}"))
                    : string.Empty;
                return new
                {
                    Arguments = arguments,
                    Name = command.Text,
                    Description = command.Description,
                };
            });
            var maxLength = Math.Max(10, commandsHelp.Max(c => c.Name.Length + c.Arguments.Length) + 1);

            var prefix = e.Server != null ? GetServerPrefix(e.Server.Id) : null;
            var message = new StringBuilder();
            message.AppendLine(!e.Channel.IsPrivate && e.User.ServerPermissions.Administrator && !all
                ? $"Popular commands for {e.Server?.Name ?? "direct messages"} (to include mod commands run `{prefix}help all`):"
                : $"Available commands for {e.Server?.Name ?? "direct messages"}:");
            message.AppendLine("```http");
            foreach (var command in commandsHelp)
            {
                var padding = maxLength - command.Name.Length - command.Arguments.Length;
                message.AppendLine($"{prefix}{command.Name}{command.Arguments}{new string(' ', padding)}: {command.Description}");
            }
            message.AppendLine("```");

            await e.Channel.SendMessage(message.ToString());
        }
    }
}

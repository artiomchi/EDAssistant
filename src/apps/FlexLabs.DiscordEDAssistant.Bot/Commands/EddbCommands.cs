﻿using Discord.Commands;
using FlexLabs.DiscordEDAssistant.Services.Integrations.Eddb;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FlexLabs.DiscordEDAssistant.Bot.Commands
{
    public static class EddbCommands
    {
        public static void CreateCommands_Eddb(this CommandService commandService)
        {
            commandService.CreateCommand("dist")
                .Description("Calculate distance between two systems")
                .Parameter("system1")
                .Parameter("system2")
                .Do(Command_Dist);

            commandService.CreateGroup("eddb", x =>
            {
                x.CreateCommand("sync")
                    .Hide()
                    .Description("Sync the core data from EDDB")
                    .AddCheck(Bot.Check_IsServerAdmin)
                    .Do(Command_Eddb_Sync);

                x.CreateCommand("sync allsystems")
                    .Hide()
                    .Description("Sync the full system list from EDDB")
                    .AddCheck(Bot.Check_IsServerAdmin)
                    .Do(Command_Eddb_Sync_AllSystems);
            });
        }

        private static async Task Command_Dist(CommandEventArgs e)
        {
            var system1 = e.GetArg("system1").Trim(',');
            var system2 = e.GetArg("system2");

            await e.Channel.SendIsTyping();

            using (var dataService = Bot.ServiceProvider.GetService(typeof(EddbDataService)) as EddbDataService)
            {
                var sys1 = dataService.GetSystem(system1);
                if (sys1 == null)
                {
                    await e.Channel.SendMessage($"Unknown system: `{system1}`");
                    return;
                }
                var sys2 = dataService.GetSystem(system2);
                if (sys2 == null)
                {
                    await e.Channel.SendMessage($"Unknown system: `{system2}`");
                    return;
                }

                var dist = Math.Sqrt(Math.Pow(sys1.X - sys2.X, 2) + Math.Pow(sys1.Y - sys2.Y, 2) + Math.Pow(sys1.Z - sys2.Z, 2));
                await e.Channel.SendMessage($"Distance between `{sys1.Name}` and `{sys2.Name}` is: `{dist.ToString("N2")} ly`");
            }
        }

        private static async Task Command_Eddb_Sync(CommandEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            using (var timer = new Timer(delegate { e.Channel.SendIsTyping(); }, null, 0, 3000))
            using (var syncService = Bot.ServiceProvider.GetService(typeof(EddbSyncService)) as EddbSyncService)
            {
                await syncService.SyncAsync();
            }

            await e.Channel.SendMessage($"EDDB sync completed in `{sw.Elapsed}`.");
        }

        private static async Task Command_Eddb_Sync_AllSystems(CommandEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            using (var timer = new Timer(delegate { e.Channel.SendIsTyping(); }, null, 0, 3000))
            using (var syncService = Bot.ServiceProvider.GetService(typeof(EddbSyncService)) as EddbSyncService)
            {
                await syncService.SyncAllSystemsAsync();
            }

            await e.Channel.SendMessage($"EDDB sync completed in `{sw.Elapsed}`.");
        }
    }
}

﻿using FlexLabs.DiscordEDAssistant.Models.Data;
using System;

namespace FlexLabs.DiscordEDAssistant.Repositories
{
    public interface IServersRepository : IDisposable
    {
        Server Load(ulong serverID);
        void Update(ulong serverID, Action<Server> updater);
    }
}

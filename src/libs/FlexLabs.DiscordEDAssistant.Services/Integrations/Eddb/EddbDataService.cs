﻿using FlexLabs.DiscordEDAssistant.Models.External.Eddb;
using FlexLabs.DiscordEDAssistant.Repositories.External.Eddb;
using System;
using System.Collections.Generic;

namespace FlexLabs.DiscordEDAssistant.Services.Integrations.Eddb
{
    public class EddbDataService : IDisposable
    {
        private readonly IEddbDataRepository _dataRepository;
        public EddbDataService(IEddbDataRepository dataRepository)
        {
            _dataRepository = dataRepository;
        }

        public void Dispose() => _dataRepository.Dispose();

        public StarSystem GetSystem(string name) => _dataRepository.GetSystem(name);
        public int? FindModuleID(string name) => _dataRepository.FindModuleID(name);
        public IEnumerable<Station> FindClosestStationsWithModules(StarSystem system, IEnumerable<int> moduleIDs)
            => _dataRepository.FindClosestStationsWithModules(system, moduleIDs);
    }
}

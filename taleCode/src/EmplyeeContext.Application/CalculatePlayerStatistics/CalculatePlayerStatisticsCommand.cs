﻿using SolTechnology.TaleCode.Infrastructure;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics;

public class CalculatePlayerStatisticsCommand : ICommand
{
    public string PlayerName { get; set; }
}
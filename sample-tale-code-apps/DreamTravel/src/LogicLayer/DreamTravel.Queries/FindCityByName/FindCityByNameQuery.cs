﻿using DreamTravel.Domain.Cities;
using FluentValidation;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Logging;

namespace DreamTravel.Queries.FindCityByName;

public class FindCityByNameQuery : IQuery<City>
{
    [LogScope]
    public string Name { get; set; } = null!;
}

public class FindCityByNameQueryValidator : AbstractValidator<FindCityByNameQuery>
{
    public FindCityByNameQueryValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty();
    }
}


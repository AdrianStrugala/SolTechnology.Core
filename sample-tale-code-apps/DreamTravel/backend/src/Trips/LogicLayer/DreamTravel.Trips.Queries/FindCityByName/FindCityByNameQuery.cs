﻿using FluentValidation;

namespace DreamTravel.Trips.Queries.FindCityByName;

public class FindCityByNameQuery
{
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
using DreamTravel.Trips.Domain.Cities;
using FluentValidation;
using MediatR;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.FindCityByCoordinates;

public class FindCityByCoordinatesQuery : IRequest<Result<City>>
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class FindCityByCoordinatesQueryValidator : AbstractValidator<FindCityByCoordinatesQuery>
{
    public FindCityByCoordinatesQueryValidator()
    {
        RuleFor(x => x.Lat)
            .InclusiveBetween(-90, 90);

        RuleFor(x => x.Lng)
            .InclusiveBetween(-180, 180);
    }
}
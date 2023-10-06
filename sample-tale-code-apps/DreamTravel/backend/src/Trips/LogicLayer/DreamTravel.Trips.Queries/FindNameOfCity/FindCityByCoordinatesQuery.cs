using FluentValidation;

namespace DreamTravel.Trips.Queries.FindNameOfCity;

public class FindCityByCoordinatesQuery
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
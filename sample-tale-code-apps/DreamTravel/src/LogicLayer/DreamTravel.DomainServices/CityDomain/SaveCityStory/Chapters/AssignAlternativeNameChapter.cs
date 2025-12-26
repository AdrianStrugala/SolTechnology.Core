using DreamTravel.DomainServices.CityDomain.SaveSteps;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.DomainServices.CityDomain.SaveCityStory.Chapters;

/// <summary>
/// Chapter that assigns alternative name to the city entity.
/// </summary>
public class AssignAlternativeNameChapter : Chapter<SaveCityContext>
{
    private readonly IAssignAlternativeNameStep _assignAlternativeNameStep;

    public AssignAlternativeNameChapter(IAssignAlternativeNameStep assignAlternativeNameStep)
    {
        _assignAlternativeNameStep = assignAlternativeNameStep;
    }

    public override Task<Result> Read(SaveCityContext context)
    {
        var cityName = context.Input.City.Name;
        _assignAlternativeNameStep.Invoke(context.CityEntity, cityName);

        return Result.SuccessAsTask();
    }
}

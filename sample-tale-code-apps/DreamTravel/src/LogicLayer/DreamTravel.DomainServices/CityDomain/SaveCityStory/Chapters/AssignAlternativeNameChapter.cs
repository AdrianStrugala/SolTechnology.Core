using DreamTravel.DomainServices.CityDomain.SaveSteps;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.DomainServices.CityDomain.SaveCityStory.Chapters;

/// <summary>
/// Chapter that assigns alternative name to the city entity.
/// </summary>
public class AssignAlternativeNameChapter : Chapter<SaveCityNarration>
{
    private readonly IAssignAlternativeNameStep _assignAlternativeNameStep;

    public AssignAlternativeNameChapter(IAssignAlternativeNameStep assignAlternativeNameStep)
    {
        _assignAlternativeNameStep = assignAlternativeNameStep;
    }

    public override Task<Result> Read(SaveCityNarration narration)
    {
        var cityName = narration.Input.City.Name;
        _assignAlternativeNameStep.Invoke(narration.CityEntity, cityName);

        return Result.SuccessAsTask();
    }
}

using DreamTravel.DomainServices.CityDomain.SaveSteps;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.DomainServices.CityDomain.SaveCityStory.Chapters;

/// <summary>
/// Chapter that increments search count statistics for the city.
/// </summary>
public class IncrementSearchCountChapter : Chapter<SaveCityContext>
{
    private readonly IIncrementSearchCountStep _incrementSearchCountStep;

    public IncrementSearchCountChapter(IIncrementSearchCountStep incrementSearchCountStep)
    {
        _incrementSearchCountStep = incrementSearchCountStep;
    }

    public override Task<Result> Read(SaveCityContext context)
    {
        _incrementSearchCountStep.Invoke(context.CityEntity, context.Today);

        return Result.SuccessAsTask();
    }
}

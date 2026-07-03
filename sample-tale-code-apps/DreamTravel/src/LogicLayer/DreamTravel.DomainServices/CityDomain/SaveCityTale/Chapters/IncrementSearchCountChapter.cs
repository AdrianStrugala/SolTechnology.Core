using DreamTravel.DomainServices.CityDomain.SaveSteps;
using SolTechnology.Core;
using SolTechnology.Core.Tale;

namespace DreamTravel.DomainServices.CityDomain.SaveCityTale.Chapters;

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

using CSharpFunctionalExtensions;

namespace DreamTravel.AvroConvertOnline.GenerateModel;

public interface IGenerateModelHandler
{
    Result<string, Exception> Handle(GenerateModelRequest request);
}
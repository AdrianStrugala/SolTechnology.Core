using CSharpFunctionalExtensions;
using SolTechnology.Avro;

namespace DreamTravel.AvroConvertOnline.GenerateModel
{
    public class GenerateModelHandler : IGenerateModelHandler
    {
        public Result<string, Exception> Handle(GenerateModelRequest request)
        {
            return
                Result.Try(() => AvroConvert.GenerateModel(request.Schema), exception => exception);
        }
    }

}
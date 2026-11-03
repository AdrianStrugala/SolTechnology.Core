namespace SolTechnology.Core.Tale.Tests.Inbound
{
    public class RecordStepChapter : Chapter<EngineTesTContext>
    {
        public override Task<Result> Read(EngineTesTContext context, CancellationToken cancellationToken)
        {
            context.ExecutionOrder.Add("Inbound");
            return Result.SuccessAsTask();
        }
    }
}

namespace SolTechnology.Core.Tale.Tests.Outbound
{
    public class RecordStepChapter : Chapter<EngineTesTContext>
    {
        public override Task<Result> Read(EngineTesTContext context, CancellationToken cancellationToken)
        {
            context.ExecutionOrder.Add("Outbound");
            return Result.SuccessAsTask();
        }
    }
}

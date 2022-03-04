using SolTechnology.Core.Guards;

namespace SolTechnology.TaleCode.SqlData.Repository.ExecutionErrorRepository
{
    public record ExecutionError
    {
        public ReferenceType ReferenceType { get; set; }
        public int ReferenceId { get; set; }
        public string Message { get; set; }
        public bool Valid { get; set; }

        private ExecutionError()
        {
            //required by ORM
        }

        public ExecutionError(ReferenceType referenceType, int referenceId, string message)
        {
            Guards.Int(referenceId, nameof(referenceId)).NotZero();

            ReferenceType = referenceType;
            ReferenceId = referenceId;
            Message = message;
            Valid = true;
        }
    }
}

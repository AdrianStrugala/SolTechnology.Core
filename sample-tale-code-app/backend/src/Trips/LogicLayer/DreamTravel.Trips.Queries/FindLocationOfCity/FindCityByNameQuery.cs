using System.ComponentModel.DataAnnotations;

namespace DreamTravel.Trips.Queries.FindLocationOfCity
{
    public class FindCityByNameQuery : IValidatableObject
    {
        public string Name { get; set; }


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> validationResult = new List<ValidationResult>();

            if (string.IsNullOrEmpty(Name))
            {
                validationResult.Add(new ValidationResult($"[{nameof(Name)}] cannot be null or empty", new[] { nameof(Name) }));
            }

            return validationResult;
        }
    }
}
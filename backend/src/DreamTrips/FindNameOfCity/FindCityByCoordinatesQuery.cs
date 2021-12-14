using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DreamTravel.DreamTrips.FindNameOfCity
{
    public class FindCityByCoordinatesQuery : IValidatableObject
    {
        public double Lat { get; set; }
        public double Lng { get; set; }


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> validationResult = new List<ValidationResult>();

            if (Lat > 90 || Lat < -90)
            {
                validationResult.Add(new ValidationResult($"Invalid value of [{nameof(Lat)}]: [{Lat}]. Must be between -90 and 90", new[] { nameof(Lat) }));
            }

            if (Lng > 180 || Lng < -180)
            {
                validationResult.Add(new ValidationResult($"Invalid value of [{nameof(Lng)}]: [{Lng}]. Must be between -180 and 180", new[] { nameof(Lng) }));
            }

            return validationResult;
        }
    }
}
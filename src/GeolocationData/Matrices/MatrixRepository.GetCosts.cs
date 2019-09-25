using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;

namespace DreamTravel.GeolocationData.Matrices
{
     public partial class MatrixRepository : IMatrixRepository
    {
        public Task<(double[], double[])> GetCosts(List<City> cities)
        {
            throw new NotImplementedException();
        }
    }
}

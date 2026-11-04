using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using SolTechnology.Core.API.Filters;

namespace SolTechnology.Core.API.Tests;

[TestFixture]
public class ApiCoreMvcOptionsExtensionsTests
{
    [Test]
    public void AddSolApiCoreFilters_RegistersFiltersResolvedFromDI()
    {
        // Arrange
        var options = new MvcOptions();

        // Act
        options.AddSolApiCoreFilters();

        // Assert
        options.Filters.Should().AllBeOfType<ServiceFilterAttribute>();
        options.Filters.Cast<ServiceFilterAttribute>().Select(filter => filter.ServiceType)
            .Should().Equal(typeof(ExceptionFilter), typeof(ResultConversionFilter));
    }
}

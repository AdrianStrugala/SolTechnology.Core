using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SolTechnology.Core.ApiClient.Tests
{
    public class ModuleInstallerTests
    {
        private readonly WebApplicationBuilder _sut;

        public ModuleInstallerTests()
        {
            _sut = WebApplication.CreateBuilder();
        }

        [Fact]
        public void AddSql_ConfigurationProvidedAsParameter_ApiClientsAreAddedToServiceCollection()
        {

            //Arrange 
            ApiClientConfiguration configuration = new ApiClientConfiguration
            {
                HttpClients = new List<HttpClient>
                {
                    new HttpClient
                    {
                        BaseAddress = "http://localhost:8080/",
                        Name = "Sample",
                        TimeoutSeconds = 21,
                        Headers = new List<Header>
                        {
                            new Header
                            {
                                Name = "HeaderName",
                                Value = "HeaderValue"
                            }
                        }
                    }
                }
            };


            //Act
            _sut.Services.AddApiClient<ISampleApiClient, SampleApiClient>("Sample", configuration); //Name has to match name provided in configuration


            //Assert
            var app = _sut.Build();

            var sampleClient = app.Services.GetService<ISampleApiClient>();
            Assert.NotNull(sampleClient);
            Assert.Equal(new Uri(configuration.HttpClients.First().BaseAddress), sampleClient.HttpClient.BaseAddress);
            Assert.Equal(TimeSpan.FromSeconds(configuration.HttpClients.First().TimeoutSeconds.Value), sampleClient.HttpClient.Timeout);
            var header = sampleClient.HttpClient.DefaultRequestHeaders.FirstOrDefault(h =>
                h.Key == configuration.HttpClients.First().Headers.First().Name);
            Assert.Equal(configuration.HttpClients.First().Headers.First().Value, header.Value.First());
        }
    }

    interface ISampleApiClient
    {
        string DownloadSth();

        System.Net.Http.HttpClient HttpClient { get; set; }
    }

    class SampleApiClient : ISampleApiClient
    {
        public System.Net.Http.HttpClient HttpClient { get; set; }

        public SampleApiClient(System.Net.Http.HttpClient httpClient)
        {
            HttpClient = httpClient;
        }
        public string DownloadSth()
        {
            return "it";
        }

    }
}
using Microsoft.Extensions.Configuration;

namespace SolTechnology.Core.API.Testing
{
    /// <summary>
    /// Fluent wrapper over the <c>ConfigurationBuilder</c> + <c>AddJsonFile</c> + <c>AddInMemoryCollection</c>
    /// pattern repeated in every app's test fixture (e.g. merging <c>appsettings.tests.json</c> with
    /// container-provided connection strings / dynamic mock URLs). Produces an <see cref="IConfiguration"/>
    /// consumable by the <see cref="APIFixture{TEntryPoint}"/> constructor — no breaking change to callers.
    /// </summary>
    public sealed class TestConfigurationBuilder
    {
        private readonly ConfigurationBuilder _builder = new();
        private readonly Dictionary<string, string?> _overrides = new();

        /// <summary>Adds a JSON settings file (e.g. <c>appsettings.tests.json</c>).</summary>
        public TestConfigurationBuilder AddJsonFile(string path, bool optional = false, bool reloadOnChange = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            _builder.AddJsonFile(path, optional, reloadOnChange);
            return this;
        }

        /// <summary>Overrides a single configuration key (e.g. a container connection string or dynamic mock URL).</summary>
        public TestConfigurationBuilder Override(string key, string? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            _overrides[key] = value;
            return this;
        }

        /// <summary>Overrides multiple configuration keys at once.</summary>
        public TestConfigurationBuilder Override(IEnumerable<KeyValuePair<string, string?>> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            foreach (var kvp in values)
            {
                _overrides[kvp.Key] = kvp.Value;
            }

            return this;
        }

        /// <summary>Builds the final <see cref="IConfiguration"/>. In-memory overrides win over the JSON files.</summary>
        public IConfiguration Build()
        {
            if (_overrides.Count > 0)
            {
                _builder.AddInMemoryCollection(_overrides);
            }

            return _builder.Build();
        }
    }
}


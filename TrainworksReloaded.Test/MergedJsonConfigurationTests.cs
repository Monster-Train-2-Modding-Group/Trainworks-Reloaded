using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Extensions;

namespace TrainworksReloaded.Test
{
    public class MergedJsonConfigurationTests : IDisposable
    {
        private readonly string directory;

        public MergedJsonConfigurationTests()
        {
            directory = Path.Combine(Path.GetTempPath(), "trainworks-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        private IConfigurationRoot Load(string json)
        {
            // PhysicalFileProvider rejects rooted paths, so the file name is relative to the base path.
            File.WriteAllText(Path.Combine(directory, "config.json"), json);
            return new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddMergedJsonFile(new List<string> { "config.json" })
                .Build();
        }

        [Fact]
        public void EmptyArray_Exists_WithNoChildren()
        {
            var config = Load("""{ "upgrades": [ { "id": "u", "trigger_upgrades": [] } ] }""");
            var section = config.GetSection("upgrades:0:trigger_upgrades");

            Assert.True(section.Exists());
            Assert.Empty(section.GetChildren());
        }

        [Fact]
        public void EmptyObject_Exists_WithNoChildren()
        {
            var config = Load("""{ "upgrades": [ { "id": "u", "extensions": {} } ] }""");
            var section = config.GetSection("upgrades:0:extensions");

            Assert.True(section.Exists());
            Assert.Empty(section.GetChildren());
        }

        [Fact]
        public void OmittedField_DoesNotExist()
        {
            var config = Load("""{ "upgrades": [ { "id": "u" } ] }""");

            Assert.False(config.GetSection("upgrades:0:trigger_upgrades").Exists());
        }

        [Fact]
        public void NonEmptyArray_HasChildren_AndNoScalarValue()
        {
            var config = Load("""{ "upgrades": [ { "id": "u", "trigger_upgrades": [ "a", { "id": "b" } ] } ] }""");
            var section = config.GetSection("upgrades:0:trigger_upgrades");

            Assert.Null(section.Value);
            Assert.Equal(["a", null], section.GetChildren().Select(x => x.Value));
            Assert.Equal("b", section.GetSection("1:id").Value);
        }

        [Fact]
        public void EmptyRoot_ProducesNoKeys()
        {
            var config = Load("{}");

            Assert.Empty(config.GetChildren());
        }
    }
}

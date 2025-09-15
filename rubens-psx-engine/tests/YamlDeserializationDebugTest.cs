using System;
using System.IO;
using NUnit.Framework;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using rubens_psx_engine.system.config;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class YamlDeserializationDebugTest
    {
        [Test]
        public void Debug_YamlDeserialization_ShowsActualValues()
        {
            // Test YAML with nested antialiasing and UI configs
            var testYaml = @"
rendering:
  enablePostProcessing: true
  antialiasing:
    enabled: true
    sampleCount: 8
  ui:
    useNativeResolution: false
    scaleFactor: 2.0
";

            Console.WriteLine("=== YAML INPUT ===");
            Console.WriteLine(testYaml);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var config = deserializer.Deserialize<RenderingConfig>(testYaml);

            Console.WriteLine("=== DESERIALIZED CONFIG ===");
            Console.WriteLine($"Config.Rendering.EnablePostProcessing: {config.Rendering.EnablePostProcessing}");
            Console.WriteLine($"Config.Rendering.Antialiasing: {config.Rendering.Antialiasing}");
            Console.WriteLine($"Config.Rendering.Antialiasing.Enabled: {config.Rendering.Antialiasing?.Enabled}");
            Console.WriteLine($"Config.Rendering.Antialiasing.SampleCount: {config.Rendering.Antialiasing?.SampleCount}");
            Console.WriteLine($"Config.Rendering.UI: {config.Rendering.UI}");
            Console.WriteLine($"Config.Rendering.UI.UseNativeResolution: {config.Rendering.UI?.UseNativeResolution}");
            Console.WriteLine($"Config.Rendering.UI.ScaleFactor: {config.Rendering.UI?.ScaleFactor}");

            // This test will tell us what's actually happening
            Assert.That(config.Rendering.EnablePostProcessing, Is.True, "EnablePostProcessing should be loaded from YAML");
        }
    }
}
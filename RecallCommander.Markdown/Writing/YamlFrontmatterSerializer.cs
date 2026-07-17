using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RecallCommander.Markdown.Writing;

/// <summary>
/// Serializes an object into a Markdown YAML frontmatter block.
/// Property names become snake_case (QuestionCount → question_count) and
/// timestamps use the artifact format "2026-07-13T19:30:00", matching the
/// documented artifact model.
/// </summary>
public static class YamlFrontmatterSerializer
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .WithTypeConverter(new TimestampConverter())
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    /// <summary>Returns the complete block including the '---' delimiters.</summary>
    public static string Serialize(object frontmatter)
    {
        ArgumentNullException.ThrowIfNull(frontmatter);
        return $"---\n{Serializer.Serialize(frontmatter)}---\n";
    }

    private sealed class TimestampConverter : IYamlTypeConverter
    {
        private const string Format = "yyyy-MM-ddTHH:mm:ss";

        public bool Accepts(Type type) => type == typeof(DateTimeOffset) || type == typeof(DateTime);

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) =>
            throw new NotSupportedException("Frontmatter is write-only.");

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            string formatted = value switch
            {
                DateTimeOffset timestamp => timestamp.ToString(Format, CultureInfo.InvariantCulture),
                DateTime timestamp => timestamp.ToString(Format, CultureInfo.InvariantCulture),
                _ => throw new NotSupportedException($"Unexpected timestamp type '{type}'."),
            };

            emitter.Emit(new Scalar(formatted));
        }
    }
}

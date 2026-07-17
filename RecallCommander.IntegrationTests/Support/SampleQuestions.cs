using System.Text;

namespace RecallCommander.IntegrationTests.Support;

/// <summary>
/// Builders for realistic Question Block Markdown used across the suite.
/// </summary>
public static class SampleQuestions
{
    public static string Block(
        string type,
        string prompt,
        string? answer = null,
        params string[] concepts)
    {
        var builder = new StringBuilder();
        builder.AppendLine(":::rc-question");
        builder.AppendLine();
        builder.AppendLine($"type: {type}");

        if (concepts.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine("concepts:");
            foreach (var concept in concepts)
            {
                builder.AppendLine($"- {concept}");
            }
        }

        builder.AppendLine();
        builder.AppendLine(":::rc-prompt");
        builder.AppendLine();
        builder.AppendLine(prompt);
        builder.AppendLine();
        builder.AppendLine(":::");

        if (answer is not null)
        {
            builder.AppendLine();
            builder.AppendLine(":::rc-answer");
            builder.AppendLine();
            builder.AppendLine(answer);
            builder.AppendLine();
            builder.AppendLine(":::");
        }

        builder.AppendLine();
        builder.AppendLine(":::");
        return builder.ToString();
    }

    public static string Recall(string prompt, string? answer = null, params string[] concepts) =>
        Block("Recall", prompt, answer, concepts);

    public static string Explanation(string prompt, string? answer = null, params string[] concepts) =>
        Block("Explanation", prompt, answer, concepts);

    public static string Synthesis(string prompt, string? answer = null, params string[] concepts) =>
        Block("Synthesis", prompt, answer, concepts);

    public static string MissingTypeBlock(string prompt) =>
        $"""
        :::rc-question

        :::rc-prompt

        {prompt}

        :::

        :::

        """;

    public static string MissingPromptBlock() =>
        """
        :::rc-question

        type: Recall

        :::

        """;

    public static string UnknownTypeBlock(string prompt) =>
        Block("Quiz", prompt);

    /// <summary>A realistic notes file: prose around all three question types.</summary>
    public static string CSharpFile() =>
        "# C# Memory Management Notes\n\n" +
        "Some personal notes about memory in C#.\n\n" +
        "---\n\n" +
        Recall(
            "What is boxing in C#?",
            "Boxing converts a value type into an object on the managed heap.",
            "Boxing", "Value Types") +
        "\nMore prose between questions.\n\n" +
        Explanation(
            "Explain how garbage collection works in .NET.",
            "The GC reclaims unreachable objects, organized into generations.",
            "Garbage Collection") +
        "\n" +
        Synthesis(
            "How do allocation patterns affect application performance?",
            answer: null,
            "Garbage Collection", "Performance");

    /// <summary>Two more valid questions for a second file.</summary>
    public static string DotNetFile() =>
        "# .NET Notes\n\n" +
        Recall("What is the CLR?", "The Common Language Runtime executes managed code.") +
        "\n" +
        Explanation("Explain how JIT compilation works.");

    /// <summary>Ordinary Markdown with no Question Blocks at all.</summary>
    public static string PlainNotesFile() =>
        """
        # Reading List

        Plain notes without any questions.

        - item one
        - item two

        ```csharp
        var x = 42;
        ```

        """;

    /// <summary>Valid and malformed blocks mixed in one file.</summary>
    public static string FileWithMalformedBlocks() =>
        Recall("Valid question before the broken ones?") +
        "\n" +
        MissingTypeBlock("Broken: no type.") +
        "\n" +
        MissingPromptBlock() +
        "\n" +
        UnknownTypeBlock("Broken: unknown type.") +
        "\n" +
        Recall("Valid question after the broken ones?");
}

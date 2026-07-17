using RecallCommander.Application.Artifacts;
using RecallCommander.Application.Assessments;
using RecallCommander.Application.Scanning;
using RecallCommander.Domain;
using RecallCommander.Infrastructure.Artifacts;
using RecallCommander.Infrastructure.FileSystem;
using RecallCommander.IntegrationTests.Support;
using RecallCommander.Markdown.Parsing;
using RecallCommander.Markdown.Writing;
using Xunit;

namespace RecallCommander.IntegrationTests.Integration;

/// <summary>
/// The full assessment pipeline below the CLI: real scan, real selection,
/// real rendering, real file persistence into a temporary workspace.
/// </summary>
public sealed class AssessmentGenerationTests : IDisposable
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 16, 18, 0, 0, TimeSpan.Zero);

    private readonly TestWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    private CreateAssessmentService CreateService()
    {
        ArtifactFileNameGenerator fileNames = new ArtifactFileNameGenerator();
        FixedTimeProvider timeProvider = new FixedTimeProvider(Now);

        ArtifactWriter<Assessment> writer = new ArtifactWriter<Assessment>(
            new AssessmentRenderer(),
            new ArtifactFileStore(fileNames),
            new FixedArtifactOutputPathProvider(_workspace.Root),
            fileNames,
            timeProvider);

        return new CreateAssessmentService(
            new ScanService(
                new StubQuestionSourceRepository(_workspace.QuestionsDirectory),
                new PhysicalFileSystem(),
                new QuestionBlockParser()),
            new RandomQuestionSelector(new Random(42)),
            writer,
            timeProvider);
    }

    private void AddSampleQuestions()
    {
        _workspace.WriteQuestionFile("csharp.md", SampleQuestions.CSharpFile());
        _workspace.WriteQuestionFile("nested/dotnet.md", SampleQuestions.DotNetFile());
    }

    [Fact]
    public async Task Writes_the_assessment_to_the_expected_location()
    {
        AddSampleQuestions();

        CreateAssessmentResult result = await CreateService().CreateAsync(requestedCount: 3);

        Assert.Equal(CreateAssessmentStatus.Created, result.Status);
        Assert.Equal(
            Path.Combine(_workspace.AssessmentsDirectory, "assessment-2026-07-16-001.md"),
            result.FilePath);
        Assert.True(File.Exists(result.FilePath));
    }

    [Fact]
    public async Task Generated_document_has_frontmatter_title_and_numbered_questions()
    {
        AddSampleQuestions();

        CreateAssessmentResult result = await CreateService().CreateAsync(requestedCount: 3);
        string document = await File.ReadAllTextAsync(result.FilePath!);

        Assert.StartsWith("---\n", document);
        Assert.Contains("type: assessment", document);
        Assert.Contains("id: assessment-2026-07-16-001", document);
        Assert.Contains("title: Assessment 2026-07-16", document);
        Assert.Contains("created: 2026-07-16T18:00:00", document);
        Assert.Contains("question_count: 3", document);

        Assert.Contains("# Assessment 2026-07-16", document);
        Assert.Contains("## Question 1", document);
        Assert.Contains("## Question 2", document);
        Assert.Contains("## Question 3", document);
        Assert.DoesNotContain("## Question 4", document);
    }

    [Fact]
    public async Task Only_question_prompts_appear_in_the_document()
    {
        AddSampleQuestions();

        CreateAssessmentResult result = await CreateService().CreateAsync(requestedCount: 5);
        string document = await File.ReadAllTextAsync(result.FilePath!);

        string body = document[document.IndexOf("# Assessment", StringComparison.Ordinal)..];
        Assert.DoesNotContain(":::", body);
        Assert.DoesNotContain("concepts", body);
        Assert.DoesNotContain("type:", body);
        Assert.DoesNotContain("Boxing converts a value type", body); // reference answer
    }

    [Fact]
    public async Task Selected_prompts_come_from_the_scanned_sources()
    {
        AddSampleQuestions();
        string[] knownPrompts = new[]
        {
            "What is boxing in C#?",
            "Explain how garbage collection works in .NET.",
            "How do allocation patterns affect application performance?",
            "What is the CLR?",
            "Explain how JIT compilation works.",
        };

        CreateAssessmentResult result = await CreateService().CreateAsync(requestedCount: 5);
        string document = await File.ReadAllTextAsync(result.FilePath!);

        Assert.All(knownPrompts, prompt => Assert.Contains(prompt, document));
    }

    [Fact]
    public async Task Sequence_number_increments_for_assessments_created_the_same_day()
    {
        AddSampleQuestions();
        CreateAssessmentService service = CreateService();

        CreateAssessmentResult first = await service.CreateAsync(requestedCount: 2);
        CreateAssessmentResult second = await service.CreateAsync(requestedCount: 2);

        Assert.EndsWith("assessment-2026-07-16-001.md", first.FilePath);
        Assert.EndsWith("assessment-2026-07-16-002.md", second.FilePath);
    }

    [Fact]
    public async Task Frontmatter_id_matches_the_generated_file_name()
    {
        AddSampleQuestions();
        CreateAssessmentService service = CreateService();

        CreateAssessmentResult first = await service.CreateAsync(requestedCount: 2);
        CreateAssessmentResult second = await service.CreateAsync(requestedCount: 2);

        foreach (CreateAssessmentResult? result in new[] { first, second })
        {
            string expectedId = Path.GetFileNameWithoutExtension(result.FilePath!);
            string document = await File.ReadAllTextAsync(result.FilePath!);
            Assert.Contains($"\nid: {expectedId}\n", document);
        }
    }

    [Fact]
    public async Task Assessment_survives_deletion_of_the_question_sources()
    {
        AddSampleQuestions();

        CreateAssessmentResult result = await CreateService().CreateAsync(requestedCount: 3);
        Directory.Delete(_workspace.QuestionsDirectory, recursive: true);

        string document = await File.ReadAllTextAsync(result.FilePath!);
        Assert.Contains("## Question 3", document);
    }
}

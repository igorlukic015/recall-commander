using RecallCommander.Domain;
using RecallCommander.Markdown.Writing;
using Xunit;

namespace RecallCommander.Markdown.Tests;

public sealed class ReviewRendererTests
{
    private const string ArtifactId = "review-2026-07-19-001";

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 7, 19, 20, 0, 0, TimeSpan.Zero);

    private readonly ReviewRenderer _renderer = new();

    private static Review SampleReview(string? attemptId = "assessment-2026-07-19-001", string? evaluator = "fake") => new(
        "Review - C# Assessment",
        CreatedAt,
        "1 of 2 questions were answered.",
        [
            new QuestionReview(
                "What is boxing in C#?",
                "Boxing wraps a value type in an object.",
                new ReviewEvaluation(
                    8,
                    UnderstandingLevel.Strong,
                    "The answer demonstrates a good understanding.",
                    strengths: ["The core idea is explained clearly."],
                    missingInformation: ["A concrete example."],
                    incorrectStatements: [],
                    suggestions: ["Add an example to strengthen the answer."])),
            new QuestionReview(
                "Explain garbage collection.",
                "",
                new ReviewEvaluation(
                    0,
                    UnderstandingLevel.Poor,
                    "The question was not answered.",
                    strengths: [],
                    missingInformation: ["An answer."],
                    incorrectStatements: [],
                    suggestions: ["Answer the question and create a new review."])),
        ],
        attemptId,
        evaluator);

    [Fact]
    public void Renders_the_documented_review_format()
    {
        string markdown = _renderer.Render(SampleReview(), ArtifactId);

        const string expected =
            """
            ---
            type: review
            id: review-2026-07-19-001
            attempt: assessment-2026-07-19-001
            title: Review - C# Assessment
            created: 2026-07-19T20:00:00
            evaluator: fake
            question_count: 2
            ---

            # Review - C# Assessment

            ## Overall Summary

            1 of 2 questions were answered.

            ---

            # Question 1

            ## Question

            What is boxing in C#?

            ## Answer

            Boxing wraps a value type in an object.

            ## Evaluation

            Score: 8/10

            Understanding: Strong

            The answer demonstrates a good understanding.

            ### Strengths

            - The core idea is explained clearly.

            ### Missing Information

            - A concrete example.

            ### Incorrect Statements

            None.

            ### Suggestions

            - Add an example to strengthen the answer.

            ---

            # Question 2

            ## Question

            Explain garbage collection.

            ## Answer

            *No answer was provided.*

            ## Evaluation

            Score: 0/10

            Understanding: Poor

            The question was not answered.

            ### Strengths

            None.

            ### Missing Information

            - An answer.

            ### Incorrect Statements

            None.

            ### Suggestions

            - Answer the question and create a new review.

            """;

        Assert.Equal(expected.ReplaceLineEndings("\n"), markdown);
    }

    [Fact]
    public void Frontmatter_id_is_the_artifact_id_it_was_rendered_with()
    {
        string markdown = _renderer.Render(SampleReview(), "review-2026-07-19-042");

        Assert.Contains("\nid: review-2026-07-19-042\n", markdown);
    }

    [Fact]
    public void Uses_the_review_slug_and_directory()
    {
        Assert.Equal("review", _renderer.Slug);
        Assert.Equal("Reviews", _renderer.DirectoryName);
    }

    [Fact]
    public void Omits_unknown_attempt_id_and_evaluator_from_frontmatter()
    {
        string markdown = _renderer.Render(SampleReview(attemptId: null, evaluator: null), ArtifactId);

        Assert.DoesNotContain("\nattempt:", markdown);
        Assert.DoesNotContain("\nevaluator:", markdown);
    }

    [Fact]
    public void Preserves_multiline_answer_markdown()
    {
        string answer = "The GC works in phases:\n\n- mark\n- sweep\n- compact";
        Review review = new Review(
            "Title",
            CreatedAt,
            "Summary.",
            [
                new QuestionReview(
                    "Explain garbage collection.",
                    answer,
                    new ReviewEvaluation(8, UnderstandingLevel.Strong, "Summary.", [], [], [], [])),
            ]);

        string markdown = _renderer.Render(review, ArtifactId);

        Assert.Contains(answer, markdown);
    }
}

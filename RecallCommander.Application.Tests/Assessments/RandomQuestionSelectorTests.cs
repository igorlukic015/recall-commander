using Xunit;
using RecallCommander.Application.Assessments;
using RecallCommander.Domain;

namespace RecallCommander.Application.Tests.Assessments;

public sealed class RandomQuestionSelectorTests
{
    private static List<Question> Questions(int count) =>
        Enumerable.Range(1, count)
            .Select(number => new Question(QuestionType.Recall, $"Question {number}?"))
            .ToList();

    [Fact]
    public void Selects_the_requested_number_of_questions()
    {
        var selector = new RandomQuestionSelector(new Random(42));

        var selected = selector.Select(Questions(20), 5);

        Assert.Equal(5, selected.Count);
    }

    [Fact]
    public void Never_selects_the_same_question_twice()
    {
        var selector = new RandomQuestionSelector(new Random(42));

        var selected = selector.Select(Questions(20), 20);

        Assert.Equal(20, selected.Distinct().Count());
    }

    [Fact]
    public void Returns_all_questions_when_fewer_exist_than_requested()
    {
        var selector = new RandomQuestionSelector(new Random(42));
        var questions = Questions(3);

        var selected = selector.Select(questions, 10);

        Assert.Equal(3, selected.Count);
        Assert.Equal(questions.OrderBy(q => q.Prompt), selected.OrderBy(q => q.Prompt));
    }

    [Fact]
    public void Selection_is_random_across_runs()
    {
        var questions = Questions(100);

        var first = new RandomQuestionSelector(new Random(1)).Select(questions, 10);
        var second = new RandomQuestionSelector(new Random(2)).Select(questions, 10);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Does_not_mutate_the_input_list()
    {
        var questions = Questions(10);
        var original = questions.ToList();

        new RandomQuestionSelector(new Random(42)).Select(questions, 5);

        Assert.Equal(original, questions);
    }

    [Fact]
    public void Rejects_count_below_one()
    {
        var selector = new RandomQuestionSelector(new Random(42));

        Assert.Throws<ArgumentOutOfRangeException>(() => selector.Select(Questions(3), 0));
    }
}

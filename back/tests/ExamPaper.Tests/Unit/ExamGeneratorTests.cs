using System;
using System.Collections.Generic;
using System.Linq;

using ExamPaper.Core.Interfaces;
using ExamPaper.Core.Models;
using ExamPaper.Service.Generator;

using Moq;

using Xunit;

namespace ExamPaper.Tests.Unit;

/// <summary>
///     Набор модульных тестов для проверки функциональности класса <see cref="ExamPaperGenerator" />.
/// </summary>
public class ExamPaperGeneratorTests
{
    private readonly Mock<IQuestionProvider> _questionProviderMock;
    private readonly Mock<IExamPaperFactory> _examPaperFactoryMock;
    private readonly ExamPaperGenerator _generator;

    /// <summary>
    ///     Инициализирует моки зависимостей и тестируемый экземпляр генератора.
    /// </summary>
    public ExamPaperGeneratorTests()
    {
        _questionProviderMock = new Mock<IQuestionProvider>();
        _examPaperFactoryMock = new Mock<IExamPaperFactory>();

        _generator = new ExamPaperGenerator(
            _questionProviderMock.Object,
            _examPaperFactoryMock.Object
        );

        _examPaperFactoryMock
            .Setup(f => f.CreateExamPaper(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<IQuestion>>()))
            .Returns((Guid id, string title, IEnumerable<IQuestion> questions) =>
                new ExamPaper.Core.Models.ExamPaper(id, title, questions));
    }

    /// <summary>
    ///     Создает коллекцию тестовых вопросов для использования в сценариях.
    /// </summary>
    /// <param name="count">Количество необходимых вопросов.</param>
    /// <returns>Список объектов, реализующих <see cref="IQuestion" />.</returns>
    private static List<IQuestion> CreateTestQuestions(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => (IQuestion)new Question(Guid.NewGuid(), $"Question {i}"))
            .ToList();
    }

    /// <summary>
    ///     Группа тестов для проверки валидации входных данных.
    /// </summary>
    public class ValidationTests : ExamPaperGeneratorTests
    {
        /// <summary>
        ///     Проверяет, что метод <see cref="ExamPaperGenerator.Generate" /> выбрасывает исключение при отсутствии настроек.
        /// </summary>
        [Fact]
        public void Generate_WithNullSettings_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _generator.Generate(null!));
        }

        /// <summary>
        ///     Проверяет реакцию системы на ситуацию, когда пул вопросов меньше, чем требуется для одного билета.
        /// </summary>
        [Fact]
        public void Generate_WhenNotEnoughQuestions_ThrowsInvalidOperationException()
        {
            List<IQuestion> questions = CreateTestQuestions(2);
            _questionProviderMock.Setup(p => p.GetAllQuestions()).Returns(questions);

            GenerationSettings settings = new() { TotalTicketsCount = 5, QuestionsPerTicketCount = 3 };

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => _generator.Generate(settings));
            Assert.Contains("Недостаточно вопросов", exception.Message);
        }
    }

    /// <summary>
    ///     Группа тестов для проверки корректности формирования структуры билетов.
    /// </summary>
    public class GenerationCorrectnessTests : ExamPaperGeneratorTests
    {
        /// <summary>
        ///     Проверяет, что итоговое количество билетов соответствует значению в <see cref="GenerationSettings.TotalTicketsCount" />.
        /// </summary>
        [Fact]
        public void Generate_WithValidParameters_ReturnsCorrectNumberOfTickets()
        {
            List<IQuestion> questions = CreateTestQuestions(10);
            _questionProviderMock.Setup(p => p.GetAllQuestions()).Returns(questions);

            GenerationSettings settings = new() { TotalTicketsCount = 5, QuestionsPerTicketCount = 2 };

            List<IExamPaper> tickets = _generator.Generate(settings).ToList();

            Assert.Equal(5, tickets.Count);
            _examPaperFactoryMock.Verify(f => f.CreateExamPaper(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<IQuestion>>()),
                Times.Exactly(5));
        }

        /// <summary>
        ///     Проверяет, что каждый сгенерированный билет содержит ровно столько вопросов, сколько указано в настройках.
        /// </summary>
        [Fact]
        public void Generate_EachTicket_ContainsCorrectNumberOfQuestions()
        {
            List<IQuestion> questions = CreateTestQuestions(20);
            _questionProviderMock.Setup(p => p.GetAllQuestions()).Returns(questions);

            int expectedCount = 3;
            GenerationSettings settings = new() { TotalTicketsCount = 4, QuestionsPerTicketCount = expectedCount };

            List<IExamPaper> tickets = _generator.Generate(settings).ToList();

            Assert.All(tickets, t => Assert.Equal(expectedCount, t.Questions.Count));
        }
    }

    /// <summary>
    ///     Группа тестов для проверки случайности выборки данных.
    /// </summary>
    public class RandomnessTests : ExamPaperGeneratorTests
    {
        /// <summary>
        ///     Проверяет, что два последовательных вызова генерации создают разные наборы вопросов.
        /// </summary>
        [Fact]
        public void Generate_MultipleTimes_ProducesDifferentResults()
        {
            List<IQuestion> questions = CreateTestQuestions(20);
            _questionProviderMock.Setup(p => p.GetAllQuestions()).Returns(questions);
            GenerationSettings settings = new() { TotalTicketsCount = 2, QuestionsPerTicketCount = 5 };

            List<IExamPaper> firstGen = _generator.Generate(settings).ToList();
            List<IExamPaper> secondGen = _generator.Generate(settings).ToList();

            bool areDifferent = !firstGen[0].Questions.Select(q => q.Id)
                .SequenceEqual(secondGen[0].Questions.Select(q => q.Id));

            Assert.True(areDifferent, "Наборы вопросов должны отличаться из-за случайного распределения.");
        }
    }

    /// <summary>
    ///     Группа тестов для проверки граничных условий и специальных форматов.
    /// </summary>
    public class EdgeCasesTests : ExamPaperGeneratorTests
    {
        /// <summary>
        ///     Проверяет, что генератор корректно применяет пользовательский шаблон для именования билетов.
        /// </summary>
        [Fact]
        public void Generate_WithCustomTemplate_UsesItForNames()
        {
            List<IQuestion> questions = CreateTestQuestions(5);
            _questionProviderMock.Setup(p => p.GetAllQuestions()).Returns(questions);

            GenerationSettings settings = new()
            {
                TotalTicketsCount = 1,
                QuestionsPerTicketCount = 2,
                TicketNameTemplate = "Test Template {0}"
            };

            List<IExamPaper> tickets = _generator.Generate(settings).ToList();

            Assert.Equal("Test Template 1", tickets[0].Title);
        }
    }
}
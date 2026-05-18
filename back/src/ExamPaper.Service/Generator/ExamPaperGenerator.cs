using System;
using System.Collections.Generic;
using System.Linq;

using ExamPaper.Core.Interfaces;

namespace ExamPaper.Service.Generator;

using System;
using System.Collections.Generic;
using System.Linq;

using Core.Interfaces;

/// <summary>
///     Сервисный класс, оркестрирующая процесс генерации билетов.
/// </summary>
public class ExamPaperGenerator : IExamGenerator
{
    private readonly IQuestionProvider _questionProvider;
    private readonly IExamPaperFactory _examPaperFactory;

    /// <summary>
    ///     Инициализирует генератор для чтения данных и создания объектов.
    /// </summary>
    public ExamPaperGenerator(
        IQuestionProvider questionProvider,
        IExamPaperFactory examPaperFactory)
    {
        _questionProvider = questionProvider ?? throw new ArgumentNullException(nameof(questionProvider));
        _examPaperFactory = examPaperFactory ?? throw new ArgumentNullException(nameof(examPaperFactory));
    }

    public IEnumerable<IExamPaper> Generate(IGenerationSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        List<IQuestion> availableQuestions = _questionProvider.GetAllQuestions().ToList();

        if (availableQuestions.Count < settings.QuestionsPerTicketCount)
        {
            throw new InvalidOperationException(
                $"Недостаточно вопросов для генерации билета. " +
                $"Доступно: {availableQuestions.Count}, требуется: {settings.QuestionsPerTicketCount}"
            );
        }

        Random random = new();

        return Enumerable
            .Range(1, settings.TotalTicketsCount)
            .Select(ticketNum =>
            {
                List<IQuestion> selectedQuestions = availableQuestions
                    .OrderBy(_ => random.Next())
                    .Take(settings.QuestionsPerTicketCount)
                    .ToList();

                string ticketTitle = string.Format(
                    settings.TicketNameTemplate ?? "Билет №{0}",
                    ticketNum
                );

                return _examPaperFactory.CreateExamPaper(
                    Guid.NewGuid(),
                    ticketTitle,
                    selectedQuestions
                );
            });
    }
}
using System.Collections.Generic;

namespace ExamPaper.Core.Interfaces;

/// <summary>
///     Контракт для экспорта экзаменационных билетов в файл.
/// </summary>
public interface IExamExporter
{
    /// <summary>
    ///     Метод для экспорта коллекции билетов в указанный файл.
    /// </summary>
    /// <param name="examPapers">Коллекция билетов для экспорта.</param>
    byte[] Export(IEnumerable<IExamPaper> examPapers);
}
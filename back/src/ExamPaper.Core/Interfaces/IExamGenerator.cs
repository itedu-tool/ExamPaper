using System.Collections.Generic;

namespace ExamPaper.Core.Interfaces;

using System.Collections.Generic;

public interface IExamGenerator
{
    /// <summary>
    ///     Формирует коллекцию экзаменационных билетов на основе заданных правил.
    ///     Оркестрирует получение вопросов и создание билетов.
    /// </summary>
    /// <param name="settings">Объект конфигурации, определяющий параметры генерации.</param>
    IEnumerable<IExamPaper> Generate(IGenerationSettings settings);
}
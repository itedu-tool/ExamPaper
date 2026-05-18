using System;

namespace ExamPaper.Core.Interfaces;

/// <summary>
///     Контракт для изменения пула экзаменационных вопросов
/// </summary>
public interface IQuestionModifier
{
    /// <summary>
    ///     Добавляет новый вопрос в пул
    /// </summary>
    /// <param name="question">Вопрос</param>
    /// <exception cref="ArgumentNullException">Если question == null</exception>
    /// <exception cref="InvalidOperationException">Если ID уже существует</exception>
    void AddQuestion(IQuestion question);

    /// <summary>
    ///     Удаляет вопрос по уникальному идентификатору
    /// </summary>
    /// <param name="questionId">GUID вопроса</param>
    void RemoveQuestion(Guid questionId);

    /// <summary>
    ///     Сохраняет все изменения в постоянное хранилище
    /// </summary>
    void SaveChanges();
}
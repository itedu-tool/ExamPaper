using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using ExamPaper.Core.Interfaces;
using ExamPaper.Core.Models;

namespace ExamPaper.Infrastructure.Repositories;

/// <summary>
///     Реализация провайдера вопросов для загрузки данных из файла в формате JSON.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="QuestionRepository" /> предоставляет функциональность для работы с коллекцией вопросов,
///         хранящихся в JSON-файле. Репозиторий поддерживает CRUD операции, загрузку и сохранение данных.
///     </para>
///     <para>
///         Все изменения в коллекции вопросов накапливаются в памяти и сохраняются в файл только при вызове
///         метода <see cref="SaveChanges" />.
///     </para>
///     <para>
///         <b>Пример использования:</b>
///         <code>
/// var repository = new QuestionRepository("questions.json");
/// 
/// // Получение всех вопросов
/// var allQuestions = repository.GetAllQuestions();
/// 
/// // Добавление нового вопроса
/// var newQuestion = new Question { Id = Guid.NewGuid(), Text = "Что такое SOLID?" };
/// repository.AddQuestion(newQuestion);
/// 
/// // Сохранение изменений
/// repository.SaveChanges();
/// </code>
///     </para>
/// </remarks>
public sealed class QuestionRepository : IQuestionRepository
{
    private readonly string _filePath;
    private readonly List<IQuestion> _questions;

    /// <summary>
    ///     Инициализирует новый экземпляр класса <see cref="QuestionRepository" />.
    /// </summary>
    /// <param name="filePath">Путь к JSON-файлу с вопросами.</param>
    /// <exception cref="ArgumentException">
    ///     Выбрасывается, если <paramref name="filePath" /> равен <c>null</c>, пустой строке или состоит только из пробелов.
    /// </exception>
    /// <remarks>
    ///     При создании репозитория происходит автоматическая загрузка данных из указанного файла.
    ///     Если файл не существует, инициализируется пустая коллекция.
    /// </remarks>
    public QuestionRepository(string filePath)
    {
        _filePath = CheckFilePath(filePath);
        _questions = LoadFromFile();
    }

    /// <summary>
    ///     Возвращает все доступные вопросы.
    /// </summary>
    /// <returns>
    ///     Доступная только для чтения коллекция вопросов (<see cref="IReadOnlyList{T}" />).
    ///     Если вопросы отсутствуют, возвращает пустую коллекцию.
    /// </returns>
    /// <remarks>
    ///     Метод возвращает снимок текущего состояния коллекции. Изменения, возвращённой коллекции,
    ///     не влияют на состояние репозитория.
    /// </remarks>
    public IEnumerable<IQuestion> GetAllQuestions()
    {
        return _questions.AsReadOnly();
    }

    /// <summary>
    ///     Добавляет новый вопрос в репозиторий.
    /// </summary>
    /// <param name="question">Вопрос для добавления.</param>
    /// <exception cref="ArgumentNullException">
    ///     Выбрасывается, если <paramref name="question" /> равен <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Выбрасывается, если в репозитории уже существует вопрос с таким же идентификатором (<see cref="IQuestion.Id" />).
    /// </exception>
    /// <remarks>
    ///     Вопрос добавляется только в память. Для сохранения изменений в файл необходимо вызвать
    ///     метод <see cref="SaveChanges" />.
    /// </remarks>
    public void AddQuestion(IQuestion question)
    {
        if (question == null)
        {
            throw new ArgumentNullException(nameof(question));
        }

        if (_questions.Any(q => q.Id == question.Id))
        {
            throw new InvalidOperationException($"Вопрос с Id {question.Id} уже существует");
        }

        _questions.Add(question);
    }

    /// <summary>
    ///     Удаляет вопрос по указанному идентификатору.
    /// </summary>
    /// <param name="questionId">Уникальный идентификатор вопроса (Guid).</param>
    /// <remarks>
    ///     <para>
    ///         Если вопрос с указанным <paramref name="questionId" /> не найден, метод не выполняет никаких действий
    ///         и не выбрасывает исключение.
    ///     </para>
    ///     <para>
    ///         Вопрос удаляется только из памяти. Для сохранения изменений в файл необходимо вызвать
    ///         метод <see cref="SaveChanges" />.
    ///     </para>
    /// </remarks>
    public void RemoveQuestion(Guid questionId)
    {
        IQuestion? question = GetQuestionById(questionId);
        if (question != null)
        {
            _questions.Remove(question);
        }
    }

    /// <summary>
    ///     Сохраняет все изменения (добавления, удаления) в файл.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Сериализует текущую коллекцию вопросов в JSON-формат с отступами (pretty print) и
    ///         записывает в файл, указанный при создании репозитория.
    ///     </para>
    ///     <para>
    ///         Если файл не существует, он будет создан. Существующий файл будет перезаписан.
    ///     </para>
    /// </remarks>
    /// <exception cref="UnauthorizedAccessException">
    ///     Может быть выброшено при отсутствии прав на запись в файл или директорию.
    /// </exception>
    /// <exception cref="IOException">
    ///     Может быть выброшено при ошибках ввода-вывода (например, диск заполнен, файл заблокирован).
    /// </exception>
    public void SaveChanges()
    {
        JsonSerializerOptions options = new() { WriteIndented = true };
        List<Question> dataToSave = _questions.Cast<Question>().ToList();
        string json = JsonSerializer.Serialize(dataToSave, options);
        File.WriteAllText(_filePath, json);
    }

    /// <summary>
    ///     Возвращает вопрос по его уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор вопроса (Guid).</param>
    /// <returns>
    ///     Объект, реализующий <see cref="IQuestion" />, если вопрос найден;
    ///     <c>null</c>, если вопрос с указанным идентификатором отсутствует в репозитории.
    /// </returns>
    /// <remarks>
    ///     Поиск выполняется за O(n) путём перебора коллекции.
    /// </remarks>
    public IQuestion? GetQuestionById(Guid id)
    {
        return _questions.FirstOrDefault(q => q.Id == id);
    }

    /// <summary>
    ///     Загружает вопросы из JSON-файла в память.
    /// </summary>
    /// <returns>
    ///     Список вопросов, загруженных из файла. Если файл не существует, пуст или содержит невалидный JSON,
    ///     возвращается пустой список.
    /// </returns>
    /// <remarks>
    ///     Метод использует регистронезависимую десериализацию JSON.
    ///     При возникновении ошибки десериализации (<see cref="JsonException" />) метод возвращает пустой список,
    ///     не выбрасывая исключение.
    /// </remarks>
    private List<IQuestion> LoadFromFile()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            string text = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
            List<Question>? deserialized = JsonSerializer.Deserialize<List<Question>>(
                text,
                options
            );
            return deserialized?.Cast<IQuestion>().ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>
    ///     Проверяет корректность пути к файлу.
    /// </summary>
    /// <param name="filePath">Путь к файлу для проверки.</param>
    /// <returns>Проверенный путь к файлу (без изменений).</returns>
    /// <exception cref="ArgumentException">
    ///     Выбрасывается, если <paramref name="filePath" /> равен <c>null</c>, пустой строке или состоит только из пробелов.
    /// </exception>
    private static string CheckFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException($"Путь к файлу не может быть пустым {nameof(filePath)}");
        }

        return filePath;
    }
}
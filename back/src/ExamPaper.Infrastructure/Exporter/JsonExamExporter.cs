using System.Collections.Generic;
using System.Text;
using System.Text.Json;

using ExamPaper.Core.Interfaces;

namespace ExamPaper.Infrastructure.Exporter;

/// <summary>
///     Класс-стратегия для экспорта экзаменационных билетов в JSON файл.
/// </summary>
public sealed class JsonExamExporter : IExamExporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    ///     Экспортирует список экзаменационных билетов через JSON файл, закодированный в байтах.
    /// </summary>
    /// <param name="examPapers"></param>
    /// <returns>JSON файл билетов в байтах</returns>
    /// <exception cref="JsonException">Если сериализация провалилась.</exception>
    public byte[] Export(IEnumerable<IExamPaper> examPapers)
    {
        string json = JsonSerializer.Serialize(examPapers, SerializerOptions);
        if (string.IsNullOrEmpty(json))
        {
            throw new JsonException("Json export failed.");
        }

        return Encoding.UTF8.GetBytes(json);
    }
}
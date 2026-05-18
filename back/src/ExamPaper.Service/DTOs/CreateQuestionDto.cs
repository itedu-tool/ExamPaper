namespace ExamPaper.Service.DTOs;

/// <summary>
///     Dto для создание вопросов из WebApi 
/// </summary>
public class CreateQuestionDto
{
    /// <summary>
    ///     Содержимое вопроса 
    /// </summary>
    public string Text { get; set; } = string.Empty;
}
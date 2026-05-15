using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ExamPaper.Core.Interfaces;
using ExamPaper.Core.Models;
using ExamPaper.Infrastructure.Exporter;
using ExamPaper.Infrastructure.Repositories;
using ExamPaper.Service.DTOs;
using ExamPaper.Service.Factories;
using ExamPaper.Service.Generator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

var allowedOrigins = builder.Configuration
                         .GetSection("Cors:AllowedOrigins")
                         .Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Version = "v1";
        document.Info.Title = "Генератор экзаменационных билетов";

        document.Info.License = new OpenApiLicense
        {
            Name = "Apache License, Version 2",
            Url = new Uri("https://opensource.org/licenses/Apache-2.0")
        };

        return Task.CompletedTask;
    });
});

string solutionDirectory = Directory.GetParent(builder.Environment.ContentRootPath)?.FullName
                           ?? builder.Environment.ContentRootPath;
string filePath = Path.Combine(solutionDirectory, "Data", "data.json");


builder.Services.AddScoped<IQuestionRepository, QuestionRepository>(_ => new QuestionRepository(filePath));
builder.Services.AddScoped<IQuestionProvider>(sp => sp.GetRequiredService<IQuestionRepository>());

builder.Services.AddScoped<IQuestionFactory, QuestionFactory>();
builder.Services.AddScoped<IExamPaperFactory, ExamPaperFactory>();
builder.Services.AddScoped<IExamGenerator, ExamPaperGenerator>();

builder.Services.AddScoped<PdfExamExporter>();
builder.Services.AddScoped<JsonExamExporter>();

WebApplication app = builder.Build();
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

//TODO Точно ли это нужно делать в проде?
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "Внутренняя ошибка сервера" });
    });
});

#region Questions

RouteGroupBuilder questionsGroup = app.MapGroup("/api/questions")
    .WithTags("Questions");

questionsGroup.MapGet("/", (IQuestionRepository repo) =>
    {
        IEnumerable<IQuestion> questions = repo.GetAllQuestions();
        return Results.Ok(questions);
    })
    .WithName("GetAllQuestions")
    .Produces<IEnumerable<IQuestion>>(StatusCodes.Status200OK);

questionsGroup.MapGet("/{id:guid}", (Guid id, IQuestionRepository repo) =>
    {
        IQuestion? question = repo.GetQuestionById(id);
        return question is not null ? Results.Ok(question) : Results.NotFound(new { error = "Вопрос не найден." });
    })
    .WithName("GetQuestionById")
    .Produces<IQuestion>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);

questionsGroup.MapPost("/", (
        [FromBody] CreateQuestionDto dto,
        IQuestionRepository repo,
        IQuestionFactory factory) =>
    {
        if (string.IsNullOrWhiteSpace(dto.Text))
        {
            return Results.BadRequest(new { error = "Текст вопроса не может быть пустым." });
        }

        try
        {
            Guid newId = Guid.NewGuid();
            IQuestion newQuestion = factory.CreateQuestion(newId, dto.Text);

            repo.AddQuestion(newQuestion);
            repo.SaveChanges();

            return Results.Created($"/api/questions/{newId}", newQuestion);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    })
    .WithName("CreateQuestion")
    .Produces<IQuestion>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status409Conflict);

questionsGroup.MapDelete("/{id:guid}", (Guid id, IQuestionRepository repo) =>
    {
        IQuestion? question = repo.GetQuestionById(id);
        if (question is null)
        {
            return Results.NotFound(new { error = "Вопрос не найден." });
        }

        repo.RemoveQuestion(id);
        repo.SaveChanges();

        return Results.NoContent();
    })
    .WithName("DeleteQuestion")
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status204NoContent);

#endregion

#region Examinations

RouteGroupBuilder examinationGroup = app.MapGroup("/api/exam")
    .WithTags("Exams");

examinationGroup.MapPost("/generate", (
        [FromBody] GenerationSettings settings,
        IExamGenerator generator) =>
    {
        try
        {
            IEnumerable<IExamPaper> tickets = generator.Generate(settings);
            return Results.Ok(tickets);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    })
    .WithName("GenerateExamPapers")
    .Produces<IEnumerable<IExamPaper>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest);

examinationGroup.MapPost("/export/pdf", (
        [FromBody] GenerationSettings settings,
        IExamGenerator generator,
        PdfExamExporter exporter) =>
    {
        try
        {
            List<IExamPaper> tickets = generator.Generate(settings).ToList();
            byte[] fileBytes = exporter.Export(tickets);
            return Results.File(fileBytes, "application/pdf", "exams.pdf");
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    })
    .WithName("ExportExamPdf")
    .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
    .Produces(StatusCodes.Status400BadRequest);

examinationGroup.MapPost("/export/json", (
        [FromBody] GenerationSettings settings,
        IExamGenerator generator,
        JsonExamExporter exporter) =>
    {
        try
        {
            List<IExamPaper> tickets = generator.Generate(settings).ToList();
            byte[] fileBytes = exporter.Export(tickets);
            return Results.File(fileBytes, "application/json", "exams.json");
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    })
    .WithName("ExportExamJson")
    .Produces(StatusCodes.Status200OK, contentType: "application/json")
    .Produces(StatusCodes.Status400BadRequest);

#endregion

await app.RunAsync();
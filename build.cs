using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

#region Логирование и отчёт
string logFileName = $"build_{DateTime.Now:yyyyMMdd_HHmmss}.log";
string reportFileName = $"build_report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
var logMessages = new StringBuilder();
var reportDetails = new List<(string Task, string Status, string Message, DateTime Time)>();

void WriteToLogAndConsole(string level, string message)
{
    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
    logMessages.AppendLine(line);
    switch (level)
    {
        case "INFO": Information(message); break;
        case "WARN": Warning(message); break;
        case "ERROR": Error(message); break;
        default: Information(message); break;
    }
}

void LogInfo(string msg) => WriteToLogAndConsole("INFO", msg);
void LogWarning(string msg) => WriteToLogAndConsole("WARN", msg);
void LogError(string msg) => WriteToLogAndConsole("ERROR", msg);
#endregion

#region Константы
const string BaseBackendDirName = "./back";
const string SolutionPath = $"{BaseBackendDirName}/ExamPaper.slnx";
const string BackSrcPath = $"{BaseBackendDirName}/src";
const string OpenApiPath = $"{BackSrcPath}/ExamPaper.Api/ExamPaper.Api.json";
const string DocFxPath = $"{BaseBackendDirName}/docs";
const string DocFxConfigPath = $"{DocFxPath}/docfx.json";
const string DocFxOutputIndex = $"{DocFxPath}/_site/index.html";
const string YamlLintArgs = "-c .yamllint.yml .github/workflows/ .yamllint.yml .spectral.yml";
const string MarkdownLintArgs = "./*.md --config .markdownlint.json";
const string BuildConfiguration = "Release";
#endregion

bool skipYamlLint = false;
bool skipMarkdownLint = false;
bool skipSpectral = false;
bool skipDocfx = false;
bool hasErrors = false;
bool lintErrorsFixed = false;

string FindCommand(string command)
{
    var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
    var paths = pathEnv.Split(System.IO.Path.PathSeparator);
    var extensions = new[] { ".cmd", ".exe", ".bat", "" };
    foreach (var p in paths)
    {
        if (string.IsNullOrWhiteSpace(p)) continue;
        foreach (var ext in extensions)
        {
            var fullPath = System.IO.Path.Combine(p, command + ext);
            if (System.IO.File.Exists(fullPath))
                return fullPath;
        }
    }
    return null;
}

bool CommandExists(string command) => FindCommand(command) != null;

bool TryInstall(string toolName, string installCommand, string command)
{
    LogInfo($"Устанавливаю {toolName}...");
    int exitCode = StartProcess(installCommand.Split(' ')[0], installCommand);
    if (exitCode != 0)
    {
        LogError($"Не удалось установить {toolName} (код ошибки: {exitCode}).");
        return false;
    }
    if (!CommandExists(command))
    {
        LogError($"Установка {toolName} завершена, но команда '{command}' не обнаружена в PATH.");
        return false;
    }
    LogInfo($"✅ {toolName} успешно установлен.");
    return true;
}

bool AskUser(string toolName, string installCommand, string command, string skipTaskName)
{
    if (!BuildSystem.IsLocalBuild)
    {
        LogError($"Инструмент '{toolName}' не найден. Установите его вручную.");
        return false;
    }

    LogInfo($"\n🔧 Инструмент '{toolName}' не найден.");
    LogInfo($"   Установка: {installCommand}");
    LogInfo($"   Пропустить задачу: {skipTaskName}");
    Console.WriteLine("   1 - Установить автоматически");
    Console.WriteLine("   2 - Пропустить задачу");
    Console.WriteLine("   3 - Остановить выполнение");
    Console.Write("Выберите действие [1/2/3]: ");
    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            bool installed = TryInstall(toolName, installCommand, command);
            if (installed) return true;
            LogInfo($"⚠️ Установка не удалась. Задача '{skipTaskName}' будет пропущена.");
            return false;
        case "2":
            LogInfo($"⚠️ Задача '{skipTaskName}' будет пропущена.");
            return false;
        case "3":
            throw new Exception("Выполнение остановлено пользователем.");
        default:
            LogInfo("Неверный ввод. Пропускаем задачу.");
            return false;
    }
}

LogInfo("Проверка наличия необходимых инструментов...");
if (!CommandExists("yamllint"))
{
    bool installed = AskUser("yamllint", "pip install yamllint", "yamllint", "Lint-Yaml");
    if (!installed) skipYamlLint = true;
}
else LogInfo("✅ yamllint найден.");

if (!CommandExists("markdownlint-cli2"))
{
    bool installed = AskUser("markdownlint-cli2", "npm install -g markdownlint-cli2", "markdownlint-cli2", "Lint-Markdown");
    if (!installed) skipMarkdownLint = true;
}
else LogInfo("✅ markdownlint-cli2 найден.");

if (!CommandExists("spectral"))
{
    bool installed = AskUser("spectral", "npm install -g @stoplight/spectral-cli", "spectral", "Lint-OpenAPI");
    if (!installed) skipSpectral = true;
}
else LogInfo("✅ spectral найден.");

if (!CommandExists("docfx"))
{
    bool installed = AskUser("docfx", "dotnet tool install -g docfx", "docfx", "Generate-Docs");
    if (!installed) skipDocfx = true;
}
else LogInfo("✅ docfx найден.");

if (!CommandExists("dotnet"))
    throw new Exception("dotnet не найден. Установите .NET SDK.");
if (!CommandExists("node"))
    throw new Exception("node не найден. npm требуется для установки некоторых инструментов.");

var target = Argument("target", "Interactive");
var configuration = Argument("configuration", BuildConfiguration);

void RecordTaskStart(string taskName) => LogInfo($"▶ Запуск задачи: {taskName}");
void RecordTaskEnd(string taskName, bool success, string message = "")
{
    string status = success ? "✅ УСПЕХ" : "❌ ОШИБКА";
    reportDetails.Add((taskName, status, message, DateTime.Now));
    if (!success) hasErrors = true;
}

Task("Lint-DotNetFormat")
    .Does(() =>
    {
        RecordTaskStart("Lint-DotNetFormat");
        try
        {
            LogInfo("🔍 Проверка форматирования C# кода...");
            DotNetFormat(SolutionPath, new DotNetFormatSettings { VerifyNoChanges = true, NoRestore = true });
            LogInfo("✅ Форматирование C# в порядке.");
            RecordTaskEnd("Lint-DotNetFormat", true);
        }
        catch (Exception ex)
        {
            LogError($"Ошибка: {ex.Message}");
            RecordTaskEnd("Lint-DotNetFormat", false, ex.Message);
        }
    });

Task("Lint-Yaml")
    .Does(() =>
    {
        RecordTaskStart("Lint-Yaml");
        if (skipYamlLint)
        {
            LogWarning("⚠️ yamllint пропущен.");
            RecordTaskEnd("Lint-Yaml", true, "Пропущен");
            return;
        }
        try
        {
            LogInfo("🔍 Линтинг YAML файлов...");
            int exitCode = StartProcess("yamllint", YamlLintArgs);
            if (exitCode != 0) throw new Exception("❌ YAML линтинг не пройден");
            LogInfo("✅ YAML файлы валидны.");
            RecordTaskEnd("Lint-Yaml", true);
        }
        catch (Exception ex)
        {
            LogError(ex.Message);
            RecordTaskEnd("Lint-Yaml", false, ex.Message);
        }
    });

Task("Lint-Markdown")
    .Does(() =>
    {
        RecordTaskStart("Lint-Markdown");
        if (skipMarkdownLint)
        {
            LogWarning("⚠️ markdownlint пропущен.");
            RecordTaskEnd("Lint-Markdown", true, "Пропущен");
            return;
        }
        try
        {
            LogInfo("🔍 Линтинг Markdown файлов...");
            var mdLint = FindCommand("markdownlint-cli2");
            if (string.IsNullOrEmpty(mdLint))
                throw new Exception("Команда markdownlint-cli2 не найдена в PATH.");
            int exitCode = StartProcess(mdLint, MarkdownLintArgs);
            if (exitCode != 0) throw new Exception("❌ Markdown линтинг не пройден");
            LogInfo("✅ Markdown файлы валидны.");
            RecordTaskEnd("Lint-Markdown", true);
        }
        catch (Exception ex)
        {
            LogError(ex.Message);
            RecordTaskEnd("Lint-Markdown", false, ex.Message);
        }
    });

Task("LintersBasic")
    .Does(() =>
    {
        RunTarget("Lint-Yaml");
        RunTarget("Lint-Markdown");
    });

Task("Fix")
    .Does(() =>
    {
        LogInfo("🔧 Автоматическое исправление ошибок...");
        try
        {
            LogInfo("→ Исправление Markdown файлов...");
            var mdLint = FindCommand("markdownlint-cli2");
            if (!string.IsNullOrEmpty(mdLint))
            {
                var fixArgs = $"--fix {MarkdownLintArgs}";
                StartProcess(mdLint, fixArgs);
            }
            LogInfo("✅ Исправление завершено.");
            lintErrorsFixed = true;
        }
        catch (Exception ex)
        {
            LogError($"Ошибка при исправлении: {ex.Message}");
        }
    });

Task("LintersWithFix")
    .Does(() =>
    {
        RunTarget("LintersBasic");

        if (hasErrors && BuildSystem.IsLocalBuild)
        {
            LogInfo("\nОбнаружены ошибки в YAML или Markdown. Хотите автоматически исправить (Markdown)?");
            Console.Write("Введите y/n (по умолчанию n): ");
            string resp = Console.ReadLine()?.Trim().ToLower();
            if (resp == "y")
            {
                RunTarget("Fix");
                hasErrors = false;
                reportDetails.Clear();
                LogInfo("Повторная проверка после исправлений...");
                RunTarget("LintersBasic");
            }
        }
    });

Task("Build-Test")
    .IsDependentOn("Lint-DotNetFormat")
    .Does(() =>
    {
        RecordTaskStart("Build-Test");
        try
        {
            LogInfo("🏗️ Сборка решения...");
            DotNetBuild(SolutionPath, new DotNetBuildSettings { Configuration = configuration, NoRestore = true });
            LogInfo("✅ Сборка успешна.");

            LogInfo("🧪 Запуск тестов...");
            DotNetTest(SolutionPath, new DotNetTestSettings { Configuration = configuration, NoBuild = true });
            LogInfo("✅ Все тесты пройдены.");
            RecordTaskEnd("Build-Test", true);
        }
        catch (Exception ex)
        {
            LogError($"Ошибка: {ex.Message}");
            RecordTaskEnd("Build-Test", false, ex.Message);
        }
    });

Task("Lint-OpenAPI")
    .IsDependentOn("Build-Test")
    .Does(() =>
    {
        RecordTaskStart("Lint-OpenAPI");
        if (skipSpectral)
        {
            LogWarning("⚠️ Spectral пропущен.");
            RecordTaskEnd("Lint-OpenAPI", true, "Пропущен");
            return;
        }
        try
        {
            LogInfo("🔍 Линтинг OpenAPI спецификации...");
            if (System.IO.File.Exists(OpenApiPath))
            {
                var spectral = FindCommand("spectral");
                if (string.IsNullOrEmpty(spectral))
                    throw new Exception("Команда spectral не найдена в PATH.");
                int exitCode = StartProcess(spectral, $"lint {OpenApiPath} --ruleset .spectral.yml");
                if (exitCode != 0) throw new Exception("❌ OpenAPI спецификация содержит ошибки");
                LogInfo("✅ OpenAPI спецификация валидна.");
            }
            else
            {
                LogWarning($"⚠️ Файл OpenAPI не найден: {OpenApiPath}");
            }
            RecordTaskEnd("Lint-OpenAPI", true);
        }
        catch (Exception ex)
        {
            LogError(ex.Message);
            RecordTaskEnd("Lint-OpenAPI", false, ex.Message);
        }
    });

Task("Generate-Docs")
    .Does(() =>
    {
        RecordTaskStart("Generate-Docs");
        if (skipDocfx)
        {
            LogWarning("⚠️ DocFX пропущен.");
            RecordTaskEnd("Generate-Docs", true, "Пропущен");
            return;
        }
        try
        {
            LogInfo("📚 Генерация документации DocFX...");
            var docfx = FindCommand("docfx");
            if (string.IsNullOrEmpty(docfx))
                throw new Exception("Команда docfx не найдена в PATH.");
            StartProcess(docfx, DocFxConfigPath);
            LogInfo($"✅ Документация собрана в {System.IO.Path.GetDirectoryName(DocFxOutputIndex)}");

            if (System.IO.File.Exists(DocFxOutputIndex))
            {
                LogInfo("🌐 Открываем документацию в браузере...");
                if (IsRunningOnWindows())
                    StartProcess("cmd", $"/c start {DocFxOutputIndex}");
                else if (IsRunningOnUnix())
                    StartProcess("open", DocFxOutputIndex);
            }
            RecordTaskEnd("Generate-Docs", true);
        }
        catch (Exception ex)
        {
            LogError($"Ошибка: {ex.Message}");
            RecordTaskEnd("Generate-Docs", false, ex.Message);
        }
    });

Task("Complete")
    .IsDependentOn("LintersWithFix")
    .IsDependentOn("Build-Test")
    .IsDependentOn("Lint-OpenAPI")
    .IsDependentOn("Generate-Docs")
    .Does(() => { });

Task("Run-API")
    .Does(() =>
    {
        LogInfo("🚀 Запуск WebAPI...");
        LogInfo("Нажмите Ctrl+C для остановки сервера.");
        StartProcess("dotnet", "run --project back/src/ExamPaper.Api --configuration Debug");
    });

Task("Interactive")
    .Does(() =>
    {
        RunTarget("Complete");
        GenerateHtmlReport();
        LogInfo($"Лог сохранён в {logFileName}");
        LogInfo($"Отчёт сохранён в {reportFileName}");

        if (!hasErrors && Argument("run-api", false))
            RunTarget("Run-API");
    });

void GenerateHtmlReport()
{
    var html = new StringBuilder();
    html.AppendLine("<!DOCTYPE html>");
    html.AppendLine("<html><head><meta charset='utf-8'><title>Build Report</title>");
    html.AppendLine("<style>body { font-family: monospace; margin:20px; } .task { margin-bottom: 15px; } .success { color: green; } .error { color: red; } .info { color: blue; }</style>");
    html.AppendLine("</head><body>");
    html.AppendLine($"<h1>Отчёт о сборке от {DateTime.Now:yyyy-MM-dd HH:mm:ss}</h1>");
    html.AppendLine("<h2>Результаты задач</h2>");
    foreach (var (task, status, msg, time) in reportDetails)
    {
        string statusClass = status.Contains("УСПЕХ") ? "success" : "error";
        html.AppendLine($"<div class='task'><strong>{task}</strong> – <span class='{statusClass}'>{status}</span> ({time:HH:mm:ss})<br/>");
        if (!string.IsNullOrEmpty(msg))
            html.AppendLine($"<span class='info'>{System.Net.WebUtility.HtmlEncode(msg)}</span>");
        html.AppendLine("</div>");
    }
    html.AppendLine("<h2>Лог выполнения</h2><pre>");
    html.AppendLine(System.Net.WebUtility.HtmlEncode(logMessages.ToString()));
    html.AppendLine("</pre></body></html>");
    System.IO.File.WriteAllText(reportFileName, html.ToString(), Encoding.UTF8);
}

void SaveLogToFile()
{
    System.IO.File.WriteAllText(logFileName, logMessages.ToString(), Encoding.UTF8);
}
AppDomain.CurrentDomain.ProcessExit += (s, e) => SaveLogToFile();
SaveLogToFile();

RunTarget(target);

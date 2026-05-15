using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Xunit;

namespace ExamPaper.Tests.Architecture;

/// <summary>
///     Архитектурные тесты для проверки физической структуры проекта.
///     Проверяет соответствие имен папок правилам: PascalCase, отсутствие пробелов и кириллицы.
/// </summary>
public class DirectoryNamingTests
{
    private static readonly Regex ValidFolderNameRegex = new("^[A-Z][a-zA-Z0-9]*$");

    [Fact]
    public void Directories_Should_Follow_Naming_Conventions()
    {
        string? solutionRoot = GetSolutionRoot();
        Assert.True(solutionRoot != null, "Не удалось найти корень решения (файл .sln).");

        List<string> errors = [];

        IEnumerable<string> projects = Directory
            .GetDirectories(solutionRoot, "ExamPaper*", SearchOption.TopDirectoryOnly)
            .Where(d => Directory.GetFiles(d, "*.csproj").Any());

        foreach (string projectPath in projects)
        {
            string projectName = Path.GetFileName(projectPath);

            string[] allDirs = Directory.GetDirectories(
                projectPath,
                "*",
                SearchOption.AllDirectories
            );

            foreach (string dirPath in allDirs)
            {
                if (IsSystemOrHiddenDirectory(dirPath))
                {
                    continue;
                }

                string dirName = Path.GetFileName(dirPath);

                if (!ValidFolderNameRegex.IsMatch(dirName))
                {
                    errors.Add(
                        $"Проект {projectName}: папка '{dirName}' нарушает правила (должна быть на латинице, в PascalCase и без пробелов)."
                    );
                }
            }
        }

        Assert.True(
            errors.Count == 0,
            "Обнаружены нарушения правил именования директорий:\n" + string.Join("\n", errors)
        );
    }

    private static string? GetSolutionRoot()
    {
        DirectoryInfo? currentDir = new(AppDomain.CurrentDomain.BaseDirectory);
        while (currentDir != null && !currentDir.GetFiles("*.slnx").Any())
        {
            currentDir = currentDir.Parent;
        }

        return currentDir?.FullName;
    }

    private static bool IsSystemOrHiddenDirectory(string path)
    {
        string[]? segments = path.Split(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        );

        return segments.Any(s =>
            s.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || s.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith(".")
        );
    }
}
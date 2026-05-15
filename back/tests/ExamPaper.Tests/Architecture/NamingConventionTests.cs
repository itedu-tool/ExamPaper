using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;

using ExamPaper.Core.Models;
using ExamPaper.Infrastructure.Repositories;
using ExamPaper.Service.Generator;

using Xunit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ExamPaper.Tests.Architecture;

/// <summary>
///     Содержит архитектурные тесты для проверки соблюдения командой разработчиков
///     единых соглашений об именовании классов, методов и других компонентов системы.
/// </summary>
public class NamingConventionTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(Question).Assembly,
            typeof(QuestionRepository).Assembly,
            typeof(ExamPaperGenerator).Assembly,
            typeof(NamingConventionTests).Assembly
        )
        .Build();

    /// <summary>
    ///     Проверяет, что абсолютно все интерфейсы в проекте (Core, Service, Infrastructure)
    ///     начинаются с заглавной буквы 'I' (например, IQuestion, IExamPaper).
    /// </summary>
    [Fact]
    public void AllInterfaces_Should_StartWith_I()
    {
        Interfaces()
            .Should().HaveNameMatching("^I.*")
            .Because(
                "все интерфейсы в проекте на C# должны начинаться с заглавной буквы 'I' по общепринятым стандартам")
            .Check(Architecture);
    }

    /// <summary>
    ///     Проверяет, что все классы, находящиеся в пространстве имен Repositories слоя Infrastructure,
    ///     имеют обязательный суффикс 'Repository' в названии.
    /// </summary>
    [Fact]
    public void Repositories_Should_Have_RepositorySuffix()
    {
        Classes()
            .That().ResideInNamespaceMatching(@"^ExamPaper\.Infrastructure\.Repositories(\..*)?$")
            .Should().HaveNameMatching(".*Repository$") // $ означает конец строки
            .Because("классы доступа к данным должны явно указывать свою роль суффиксом 'Repository'")
            .Check(Architecture);
    }

    /// <summary>
    ///     Проверяет, что все классы-фабрики, находящиеся в слое Service,
    ///     имеют обязательный суффикс 'Factory' в названии.
    /// </summary>
    [Fact]
    public void Factories_Should_Have_FactorySuffix()
    {
        Classes()
            .That().ResideInNamespaceMatching(@"^ExamPaper\.Service\.Factories(\..*)?$")
            .Should().HaveNameMatching(".*Factory$")
            .Because("классы, отвечающие за порождение сложных объектов, должны иметь суффикс 'Factory'")
            .Check(Architecture);
    }

    /// <summary>
    ///     Проверяет, что все классы экспорта данных, находящиеся в слое Infrastructure,
    ///     имеют обязательный суффикс 'Exporter' в названии.
    /// </summary>
    [Fact]
    public void Exporters_Should_Have_ExporterSuffix()
    {
        Classes()
            .That().ResideInNamespaceMatching(@"^ExamPaper\.Infrastructure\.Exporter(\..*)?$")
            .Should().HaveNameMatching(".*Exporter$")
            .Because("компоненты, выгружающие данные, должны идентифицироваться суффиксом 'Exporter'")
            .Check(Architecture);
    }


    /// <summary>
    ///     Проверяет, что все классы в тестовой сборке имеют суффикс 'Tests'.
    /// </summary>
    [Fact]
    public void AllTestClasses_Should_Have_TestsSuffix()
    {
        Classes()
            .That().ResideInAssembly(typeof(NamingConventionTests).Assembly)
            .And().AreNotAbstract()
            .And().AreNotNested()
            .Should().HaveNameMatching(".*Tests$")
            .Because(
                "согласно стандартам проекта, все тестовые классы должны заканчиваться на 'Tests' (во множественном числе)")
            .Check(Architecture);
    }
}
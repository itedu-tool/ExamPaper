using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Syntax.Elements.Types.Classes;
using ArchUnitNET.Fluent.Syntax.Elements.Types.Interfaces;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;

using ExamPaper.Core.Models;
using ExamPaper.Infrastructure.Repositories;
using ExamPaper.Service.Generator;

using Xunit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ExamPaper.Tests.Architecture;

/// <summary>
///     Содержит архитектурные тесты для проверки соблюдения правил проектирования,
///     правильного использования модификаторов доступа и защиты инкапсуляции.
/// </summary>
public class DesignRulesTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(Question).Assembly,
            typeof(QuestionRepository).Assembly,
            typeof(ExamPaperGenerator).Assembly
        ).Build();

    private static readonly IObjectProvider<IType> CoreLayer = Types()
        .That()
        .ResideInAssembly(typeof(Question).Assembly).And().ResideInNamespaceMatching(@"^ExamPaper\.Core(\..*)?$")
        .As("Core Layer");

    private static readonly IObjectProvider<IType> InfrastructureLayer = Types()
        .That()
        .ResideInAssembly(typeof(QuestionRepository).Assembly).And()
        .ResideInNamespaceMatching(@"^ExamPaper\.Infrastructure(\..*)?$")
        .As("Infrastructure Layer");

    /// <summary>
    ///     Проверяет, что все классы доменных моделей в слое Core
    ///     помечены модификатором 'sealed'. Это предотвращает непредсказуемое наследование
    ///     и изменение базового поведения фундаментальных сущностей проекта.
    /// </summary>
    [Fact]
    public void CoreModels_Should_BeSealed()
    {
        Classes()
            .That().Are(CoreLayer)
            .And()
            .ResideInNamespaceMatching(@"^ExamPaper\.Core\.Models(\..*)?$")
            .And()
            .AreNotAbstract()
            .And().AreNotSealed()
            .Should().NotExist()
            .Because("Все модели в слое Core должны быть sealed")
            .Check(Architecture);
    }


    /// <summary>
    ///     Проверяет, что классы конкретных реализаций в слое Infrastructure
    ///     (такие как репозитории и экспортеры) являются 'sealed'.
    ///     Инфраструктурные классы предназначены для выполнения конкретной работы,
    ///     а не для того, чтобы служить базовыми классами для других.
    /// </summary>
    [Fact]
    public void InfrastructureImplementations_Should_BeSealed()
    {
        Classes().That()
            .Are(InfrastructureLayer)
            .And().AreNotAbstract().And().AreNotSealed()
            .Should().NotExist().Because("Реализации в слое Infrastructure должны быть помечены как sealed.")
            .Check(Architecture);
    }


    /// <summary>
    ///     Проверяет отсутствие статических классов в слое Core.
    ///     Использование статического состояния в доменной логике нарушает принципы ООП
    ///     и затрудняет тестирование. Вместо них следует использовать интерфейсы и Иньекцию зависимотей.
    /// </summary>
    [Fact]
    public void Classes_In_Core_ShouldNot_Be_Static()
    {
        Classes()
            .That()
            .Are(CoreLayer)
            .And().AreAbstract()
            .And().AreSealed()
            .As("Static Classes")
            .Should().NotExist()
            .Because("Слой Core не должен содержать статических классов (используйте интерфейсы и DI)")
            .Check(Architecture);
    }

    /// <summary>
    ///     Проверяет что все контаркты определены в Core
    /// </summary>
    [Fact]
    public void AllInterfaces_Should_Reside_In_Core()
    {
        Interfaces()
            .That()
            .ResideInNamespaceMatching(@"^(?!ExamPaper\.Core\.Interfaces(\..*)?$).*$")
            .Should().NotExist()
            .Because(
                "Архитектура требует, чтобы все контракты системы (из любых слоев) были централизованы в ExamPaper.Core.Interfaces")
            .Check(Architecture);
    }


    /// <summary>
    ///     Универсальное правило зависимостей контрактов.
    ///     Проверяет, что ни один класс за пределами слоя Core не зависит от интерфейсов,
    ///     которые объявлены за пределами слоя Core.
    /// </summary>
    [Fact]
    public void ExternalLayers_Should_Only_Depend_On_Core_Interfaces()
    {
        GivenClassesConjunctionWithDescription? nonCoreClasses = Classes().That()
            .ResideInNamespaceMatching(@"^(?!ExamPaper\.Core).*$")
            .As("Classes outside Core");

        GivenInterfacesConjunction? nonCoreInterfaces = Interfaces().That()
            .ResideInNamespaceMatching(@"^(?!ExamPaper\.Core).*$")
            .As("Interfaces outside Core");

        Types()
            .That().Are(nonCoreClasses)
            .Should().NotDependOnAny(nonCoreInterfaces)
            .Because("Детали реализации (любые внешние слои) имеют право " +
                     "использовать и реализовывать только контракты, продиктованные слоем Core.")
            .Check(Architecture);
    }
}
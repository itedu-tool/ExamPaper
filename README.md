# Диаграммы

## Прецедентов

```mermaid
flowchart LR
  actor1[Пользователь]


    uc1((Добавление списка вопросов))
    uc2((Выбирает количество билетов и вопросов))
    uc3((Получает файл с билетами в PDF формате ))


  actor1 --> uc1
  uc1 --> uc2
  uc2 --> uc3
```

## Последовательности

```mermaid
sequenceDiagram
    participant Пользователь
    participant Система

    Пользователь->>Система: Добавляет список вопросов
    Пользователь->>Система: Указывает количество вопросов в билете
    Пользователь->>Система: Указывает количество билетов
    Система->>Система: Генерирует билеты
    Система->>Пользователь: Выдает PDF‑файл с билетами
```

## Компонентов

```mermaid
flowchart
subgraph Library
subgraph Core
Interfaces((Interfaces))
Models[Models]
Models-.Реализует.->Interfaces
end

subgraph Infrastructure
Exporters[Exporters]
Exporters-.Реализует.->Interfaces
Repositories[Repositories]
Repositories-.Реализует.->Interfaces
end

subgraph Service
Factories[Factories]
Factories-.Реализует.->Interfaces
Factories--Зависит-->Models
Generators[Generators]
Generators-.Реализует.->Interfaces
end
end

subgraph Presentation layer
API[API]
end
API-->Library
```

## Регламент

- ![Создание ветки](rules/BranchCreation.md);
- ![Создание файлов и директорий](rules/FileAndDirectoriesCreation.md);
- ![Создание тест-кейсов](rules/TestCaseRegulations.md).

## Цель проекта: Разработка генератора экзаменационных билетов.

Ключевые функции:

- Хранение вопросов и билетов в JSON-файле.
- Генерация билетов по запросу пользователя (количество вопросов варьируется с количеством билетов).
- Экспорт в PDF (используя библиотеку QuestPDF)

## Контракты базовых сущностей

```cs
namespace ExamPaper.Core.Interfaces
{
    public interface IQuestion
    {

        Guid Id { get; }
        string Text { get; }
    }
}
```

```cs
using System;
using System.Collections.Generic;

namespace ExamPaper.Core.Interfaces
{

    public interface IExamPaper
    {

        Guid Id { get; }
        string Title { get; }
        IReadOnlyCollection<IQuestion> Questions { get; }

    }
}
```

## SAST Tools

[PVS-Studio](https://pvs-studio.com/pvs-studio/?utm_source=website&utm_medium=github&utm_campaign=open_source) - static
analyzer for C, C++, C#, and Java code.

# xpar - Контекст проекта

## Назначение проекта
Консольное приложение — функциональная копия приложения `par` (Par 1.53.0).
`par` — это фильтр для переформатирования абзацев, который:
- читает строки из stdin
- преобразует согласно переданным в аргументах настройкам
- выводит в stdout

Помимо повторения алгоритмов форматирования оригинала,
необходимо учесть требование обработки Unicode.
Для удобства всегда считаем, что исходный и трансформированный текст в формате utf8.

## Важное отличие от оригинала

**Оригинальный `par` работает с однобайтовыми строками** (тип `char` в C, 1 байт = 1 символ).
Все алгоритмы оригинала (comprelen, comsuflen, разбиение на слова и т.д.) оперируют байтами.

**Цель xpar — добавить поддержку Unicode**, поэтому все алгоритмы должны работать с **группами графем** (grapheme clusters), а не с отдельными символами/байтами.
Одна группа графем может состоять из нескольких code points Unicode и занимать несколько `char` в .NET.

Это означает:
- При подсчёте длин префиксов/суффиксов используются ширины графем, а не количество символов
- При разбиении на слова границы определяются по графемам
- Charset-операции применяются к символам внутри графем или к отдельным code points
- `Wcwidth.UnicodeCalculator.GetWidth()` используется для расчёта визуальной ширины графем

**Различия в модели ввода:**

**Оригинальный `par` читает строки из stdin и сразу их обрабатывает** — функция `readlines()` читает строку, анализирует её, и если обнаружен защитный символ, пустая строка или конец абзаца, останавливает чтение и возвращает управление.
Позиция чтения stdin **сохраняется** — следующий вызов `readlines()` продолжит с того места, где остановился предыдущий.

**xpar сначала вычитывает все строки из stdin целиком, а затем обрабатывает** пакетом через `Transform(IReadOnlyList<string>)`.
Это упрощает архитектуру, но при анализе исходных алгоритмов (особенно `readlines`, обработка абзацев, protected lines) следует учитывать, что в `par` позиция чтения **неявно отслеживается подсистемой ввода**, а в xpar все строки уже загружены в память.

При переносе логики `readlines` и подобных методов необходимо:
- Либо явно отслеживать «позицию чтения» (индекс в массиве строк)
- Либо адаптировать алгоритм под работу с полным набором данных (например, сначала разбить на абзацы, затем обрабатывать каждый)

## Архитектура
- **par/** — исходный код оригинального приложения `par` на C (эталон для сверки)
- **xPar** — консольное приложение (точка входа)
- **xParLib** — библиотека классов, содержит логику преобразования строк
- **xParTests** — модульные тесты (xUnit v3)

## Ключевые классы и методы
- `xParLib.ParOptions` — класс для хранения параметров форматирования
- `xParLib.ParOptions.Parse(string[] args)` — метод для разбора аргументов командной строки
- `xParLib.StringTransformer.Transform(IReadOnlyList<string>, ParOptions)` — главный публичный метод библиотеки
- `xParLib.Charset` — класс для работы с наборами символов (charset syntax оригинала)
- `xParLib.LineReader.ReadLines(...)` — чтение строк до EOF/protected/blank line (static)
- `xParLib.LineProp` — свойства строки (P, S, Flags, Rc)
- `xParLib.LineSegment` — пара (строка, LineProp)
- `xParLib.ReadLinesResult` — результат работы `LineReader.ReadLines`
- `xParLib.CompresuflenResult` — результат вычисления comprelen/comsuflen (поля Prefix, Suffix)
- `LineReader.Compresuflen(...)` — вычисление общего префикса и суффикса (аналог `compresuflen()`)
  - Сигнатура: `Compresuflen(IReadOnlyList<LineSegment> segments, int startIndex, int endIndex, Charset bodyChars, bool body, int minPrefix, int minSuffix)`
- `LineReader.Delimit(...)` — определение бестелесных строк и разметка свойств (аналог `delimit()`)
  - Сигнатура: `Delimit(IReadOnlyList<LineSegment> segments, int startIndex, int endIndex, Charset bodyChars, int repeat, bool body, bool div, int minPrefix, int minSuffix)`
  - Внутри вызывает `Compresuflen`, затем рекурсивно обрабатывает подблоки между bodiless строками
- `LineReader.MarkSuperf(...)` — разметка избыточных (superfluous) строк (аналог `marksuperf()`)
  - Сигнатура: `MarkSuperf(IReadOnlyList<LineSegment> segments, int startIndex, int endIndex)`
  - Требует, чтобы `L_BODILESS` был уже установлен (после вызова `Delimit`)
- `LineReader.SetAffixes(...)` — вычисление финальных префикса и суффикса IP (аналог `setaffixes()`)
  - Сигнатура: `SetAffixes(IReadOnlyList<LineSegment> segments, int startIndex, int endIndex, Charset bodyChars, Charset quoteChars, int hang, bool body, bool quote, int? prefix, int? suffix)`
  - Возвращает `SetAffixesResult` (Prefix, Suffix, AugmentedFallbackPre, FallbackSuf)
  - Вызывает `Compresuflen` при необходимости, учитывает `hang` и `quote` для augmented fallback
- `xParLib.WordFlags` — флаги свойств слова (None, Shifted, Curious, Capital)
- `xParLib.Word` — представление слова в абзаце (Text, NextLine, Score, Width, Flags, SourceLineIndex, SeparatorAfterWord)
- `xParLib.ExtractWordsResult` — результат выделения слов (Words, Suffixes, L, ErrorMessage)
- `xParLib.ReformatModule` — класс для форматирования абзацев (аналог reformat.c)
  - `ReformatModule.ExtractWords(...)` — выделение слов, обработка guess, проверка/разбиение длины (аналог reformat.c строки 331–430)
    - Сигнатура: `ExtractWords(IReadOnlyList<LineSegment> segments, int startIndex, int endIndex, int prefix, int suffix, int width, Charset terminalChars, bool cap, bool guess, bool report)`
    - Возвращает `ExtractWordsResult`
  - `ReformatModule.Reformat(...)` — главный метод форматирования: ExtractWords → NormalBreaks/JustBreaks → ConstructLines (аналог reformat() из reformat.c, строка 297)
  - `ReformatModule.SimpleBreaks(List<Word>, int L, bool last)` — выбор разрывов строк, максимизация кратчайшей строки (аналог simplebreaks(), reformat.c строки 98–136)
  - `ReformatModule.NormalBreaks(List<Word>, int L, bool fit, bool last)` — выбор разрывов строк для just=0, три этапа оптимизации (аналог normalbreaks(), reformat.c строки 138–196)
  - `ReformatModule.JustBreaks(List<Word>, int L, bool last)` — выбор разрывов строк для just=1, justification по обоим краям (аналог justbreaks(), reformat.c строки 211–289)
  - `ReformatModule.ConstructLines(ExtractWordsResult, SetAffixesResult, int hang, bool just, bool last, bool touch)` — построение выходных строк с префиксами, телом и суффиксами (аналог "Construct the lines", reformat.c строки 459–522)
- Вспомогательные методы (private в ReformatModule): `GetGraphemeWidth`, `CalcVisualWidth`, `GetSuffix`, `CalcGraphemeWidthRange`, `IsAlphanumeric`, `IsCurious`, `IsCapitalized`, `SplitWord`
- Вспомогательные методы (private в LineReader): `IsBodiless`, `IsInserted`, `IsVacant`, `CountGraphemes`, `CountNonSpaceGraphemes`
- Вспомогательные методы (public в LineReader): `GetGraphemes(string)` — получение списка графем-кластеров строки

## Основные алгоритмы оригинала (par.doc)
- **comprelen** — длина общего префикса строк
- **comsuflen** — длина общего суффикса строк
- **fallback prelen/suflen** — резервная длина префикса/суффикса
- **bodiless lines** — бестелесные строки (заполнители)
- **quote handling** — обработка уровней цитирования
- **word wrapping** — разбиение слов по строкам с учётом width, fit, just, last
- **guess** — обработка разрывов предложений

## Что можно делать
- Добавлять новые методы преобразования в `StringTransformer`
- Расширять тестовое покрытие в `xParTests`
- Добавлять новые форматы вывода статистики
- Добавлять конфигурацию через аргументы командной строки
- Рефакторить с сохранением публичного API `Transform`
- Править описание алгоритмов в **ReadMe.md**
- Сверяться с логикой исходного проекта **par**
- Планировать постепенный перенос логики и поведения,
  полный перевод недопустим
- Предлагать следующие шаги и согласовывать их
- История преобразований должна быть отражена в файлах каталога **History** следующей структуры:
    - имя должно иметь вид h_001.md с номером текущего шага преобразования
    - должна быть глава **Цель шага** с кратким описанием цели шага
    - должна быть глава **План шага** с подробным описанием шага
    - должна быть глава **Результат шага** с подробным описанием, что было сделано
- **Старые шаги не редактируются** — файлы `History/h_*.md` зафиксированы как часть истории развития проекта.
  Все изменения, уточнения и исправления описываются только в текущем шаге.

## Требования к оформлению кода

- Комментарии к публичным классам и методам должны быть в формате **xmldoc** (`<summary>`, `<param>`, `<returns>` и т.д.)
- Комментарии должны быть на русском языке
- Все ссылки на исходные функции/методы оригинального `par` должны размещаться в секции `<remarks>` xmldoc
- xmldoc для классов и методов должен быть полным и соответствовать требованиям оформления

## Поддержание актуальности документации

После завершения каждого шага преобразования необходимо:
- Проверить, требуется ли обновить **README.md** (структура проекта, новые компоненты, описание API)
- Проверить, требуется ли обновить **QWEN.md** (ключевые классы, методы, алгоритмы, контекст)
- Внести изменения в оба файла, если появились новые публичные типы, методы или изменилась архитектура

## Стек
- .NET 10.0
- Wcwidth 2.0.0
- xUnit v3 3.2.2
- Moq 4.20.72

## Полезные команды
- **Запуск тестов:** `dotnet test --project xParTests`
- **Сборка решения:** `dotnet build`
- **Запуск приложения:** `dotnet run --project xPar`

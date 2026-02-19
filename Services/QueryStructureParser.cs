using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace QueryEditor1C.Services;

/// <summary>
/// Представляет элемент структуры запроса
/// </summary>
public class QueryStructureItem
{
    public string DisplayName { get; set; } = "";
    public string Type { get; set; } = ""; // "TempTable", "Query", "Subquery"
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public List<QueryStructureItem> Children { get; set; } = new();
    public bool IsUsed { get; set; } = true; // Для временных таблиц: используется ли в других запросах
    public string QueryText { get; set; } = ""; // Текст запроса для анализа
}

/// <summary>
/// Сервис для анализа структуры запросов
/// </summary>
public class QueryStructureParser
{
    /// <summary>
    /// Парсит структуру пакета запросов
    /// </summary>
    public List<QueryStructureItem> Parse(string query)
    {
        var result = new List<QueryStructureItem>();
        if (string.IsNullOrWhiteSpace(query))
            return result;

        // Разбиваем на отдельные запросы по разделителям ; или /////////
        var queriesWithOffsets = SplitQueriesWithOffsets(query);
        int queryNumber = 1;

        // Сначала собираем все запросы
        foreach (var (q, startOffset, endOffset) in queriesWithOffsets)
        {
            var item = ParseSingleQuery(q, queryNumber, startOffset, endOffset);
            if (item != null)
            {
                result.Add(item);
                queryNumber++;
            }
        }

        // Анализируем использование временных таблиц
        AnalyzeTempTableUsage(result);

        return result;
    }

    /// <summary>
    /// Анализирует какие временные таблицы используются в других запросах
    /// </summary>
    private void AnalyzeTempTableUsage(List<QueryStructureItem> queries)
    {
        // Собираем имена всех временных таблиц
        var tempTables = queries.Where(q => q.Type == "TempTable").ToList();
        var tempTableNames = tempTables.Select(t => t.DisplayName.ToUpperInvariant()).ToHashSet();

        // Для каждой временной таблицы проверяем, используется ли она в других запросах
        foreach (var tempTable in tempTables)
        {
            bool isUsed = false;
            var tempTableName = tempTable.DisplayName.ToUpperInvariant();

            foreach (var query in queries)
            {
                // Пропускаем саму таблицу (где она создается)
                if (query == tempTable) continue;

                // Ищем использование таблицы в тексте запроса
                // Временные таблицы используются в FROM, JOIN и т.д.
                if (IsTempTableUsedInQuery(query, tempTableName))
                {
                    isUsed = true;
                    break;
                }
            }

            tempTable.IsUsed = isUsed;
        }
    }

    /// <summary>
    /// Проверяет, используется ли временная таблица в запросе
    /// </summary>
    private bool IsTempTableUsedInQuery(QueryStructureItem query, string tempTableName)
    {
        if (string.IsNullOrEmpty(query.QueryText))
            return false;

        var queryTextUpper = query.QueryText.ToUpperInvariant();
        var tempTableNameUpper = tempTableName.ToUpperInvariant();

        // Ищем имя таблицы в тексте запроса
        // Используем регулярное выражение для точного поиска
        var pattern = $@"\b{Regex.Escape(tempTableNameUpper)}\b";
        var matches = Regex.Matches(queryTextUpper, pattern);

        // Если таблица найдена в тексте запроса (не в части ПОМЕСТИТЬ)
        foreach (Match match in matches)
        {
            // Проверяем, что это не часть "ПОМЕСТИТЬ <таблица>"
            var textBeforeMatch = queryTextUpper.Substring(0, match.Index);
            var linesBefore = textBeforeMatch.Split('\n');
            var lastLine = linesBefore.Length > 0 ? linesBefore[^1].Trim() : "";

            // Если перед совпадением нет "ПОМЕСТИТЬ" или "INTO" на той же строке
            // и это не просто объявление таблицы
            if (!lastLine.EndsWith("ПОМЕСТИТЬ") && 
                !lastLine.EndsWith("INTO") &&
                !lastLine.Contains($"ПОМЕСТИТЬ {tempTableNameUpper}") &&
                !lastLine.Contains($"INTO {tempTableNameUpper}"))
            {
                return true;
            }
        }

        // Также проверяем дочерние элементы
        foreach (var child in query.Children)
        {
            if (child.DisplayName.ToUpperInvariant().Contains(tempTableNameUpper))
                return true;
        }

        return false;
    }

    private List<(string query, int startOffset, int endOffset)> SplitQueriesWithOffsets(string fullQuery)
    {
        var queries = new List<(string, int, int)>();
        var currentQuery = new System.Text.StringBuilder();
        int currentStartOffset = 0;
        int lineStartOffset = 0;
        var lines = fullQuery.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            int lineLength = line.Length;
            
            // Пропускаем разделители временных таблиц (/////////)
            if (trimmed.StartsWith("//") && trimmed.Length > 10 && trimmed.All(c => c == '/' || c == '\r' || c == '\n'))
            {
                if (currentQuery.Length > 0)
                {
                    var queryText = currentQuery.ToString().Trim();
                    int endOffset = lineStartOffset;
                    queries.Add((queryText, currentStartOffset, endOffset));
                    currentQuery.Clear();
                }
                lineStartOffset += lineLength + 1; // +1 for newline
                continue;
            }
            
            // Конец запроса
            if (trimmed.EndsWith(";") && !trimmed.StartsWith("//"))
            {
                currentQuery.AppendLine(line);
                var queryText = currentQuery.ToString().Trim();
                int endOffset = lineStartOffset + lineLength;
                queries.Add((queryText, currentStartOffset, endOffset));
                currentQuery.Clear();
                currentStartOffset = lineStartOffset + lineLength + 1;
                lineStartOffset = currentStartOffset;
                continue;
            }
            
            currentQuery.AppendLine(line);
            lineStartOffset += lineLength + 1; // +1 for newline
        }

        // Добавляем последний запрос
        if (currentQuery.Length > 0)
        {
            var queryText = currentQuery.ToString().Trim();
            int endOffset = lineStartOffset;
            queries.Add((queryText, currentStartOffset, endOffset));
        }

        return queries.Where(q => !string.IsNullOrWhiteSpace(q.Item1)).ToList();
    }

    private List<string> SplitQueries(string query)
    {
        return SplitQueriesWithOffsets(query).Select(q => q.Item1).ToList();
    }

    private QueryStructureItem? ParseSingleQuery(string query, int queryNumber, int startOffset, int endOffset)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        var item = new QueryStructureItem
        {
            StartIndex = startOffset,
            EndIndex = endOffset,
            QueryText = query
        };

        // Ищем ПОМЕСТИТЬ или INTO
        var tempTableName = ExtractTempTableName(query);
        
        if (!string.IsNullOrEmpty(tempTableName))
        {
            item.DisplayName = tempTableName;
            item.Type = "TempTable";
        }
        else
        {
            item.DisplayName = $"Запрос пакета {queryNumber}";
            item.Type = "Query";
        }

        // Ищем вложенные запросы (ВЫБРАТЬ внутри ВЫБРАТЬ)
        item.Children = FindSubqueries(query, startOffset);

        return item;
    }

    private string? ExtractTempTableName(string query)
    {
        // Ищем ПОМЕСТИТЬ или INTO
        var patterns = new[]
        {
            @"ПОМЕСТИТЬ\s+(\w+)",
            @"INTO\s+(\w+)",
            @"ПОМЕСТИТЬ\s+(\w+\.\w+)",
            @"INTO\s+(\w+\.\w+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(query, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    private List<QueryStructureItem> FindSubqueries(string query, int baseOffset = 0)
    {
        var subqueries = new List<QueryStructureItem>();
        
        // Ищем вложенные ВЫБРАТЬ после В, EXISTS и т.д.
        var subqueryPattern = @"\(\s*ВЫБРАТЬ|\(\s*SELECT";
        var matches = Regex.Matches(query, subqueryPattern, RegexOptions.IgnoreCase);
        
        int subqueryNum = 1;
        foreach (Match match in matches)
        {
            // Находим конец подзапроса
            int start = match.Index + baseOffset;
            int end = FindClosingParenthesis(query, match.Index) + baseOffset;
            
            if (end > start)
            {
                var subqueryName = $"Подзапрос {subqueryNum}";
                var subqueryItem = new QueryStructureItem
                {
                    DisplayName = subqueryName,
                    Type = "Subquery",
                    StartIndex = start,
                    EndIndex = end
                };
                subqueries.Add(subqueryItem);
                subqueryNum++;
            }
        }

        return subqueries;
    }

    private int FindClosingParenthesis(string text, int startIndex)
    {
        int depth = 1;
        for (int i = startIndex + 1; i < text.Length; i++)
        {
            if (text[i] == '(') depth++;
            else if (text[i] == ')') depth--;
            
            if (depth == 0) return i;
        }
        return text.Length - 1;
    }
}
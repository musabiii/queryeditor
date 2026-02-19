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
        var queries = SplitQueries(query);
        int queryNumber = 1;

        foreach (var q in queries)
        {
            var item = ParseSingleQuery(q, queryNumber);
            if (item != null)
            {
                result.Add(item);
                queryNumber++;
            }
        }

        return result;
    }

    private List<string> SplitQueries(string query)
    {
        var queries = new List<string>();
        var currentQuery = new System.Text.StringBuilder();
        var lines = query.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Пропускаем разделители временных таблиц (/////////)
            if (trimmed.StartsWith("//") && trimmed.Length > 10 && trimmed.All(c => c == '/' || c == '\r' || c == '\n'))
            {
                if (currentQuery.Length > 0)
                {
                    queries.Add(currentQuery.ToString().Trim());
                    currentQuery.Clear();
                }
                continue;
            }
            
            // Конец запроса
            if (trimmed.EndsWith(";") && !trimmed.StartsWith("//"))
            {
                currentQuery.AppendLine(line);
                queries.Add(currentQuery.ToString().Trim());
                currentQuery.Clear();
                continue;
            }
            
            currentQuery.AppendLine(line);
        }

        // Добавляем последний запрос
        if (currentQuery.Length > 0)
        {
            queries.Add(currentQuery.ToString().Trim());
        }

        return queries.Where(q => !string.IsNullOrWhiteSpace(q)).ToList();
    }

    private QueryStructureItem? ParseSingleQuery(string query, int queryNumber)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        var item = new QueryStructureItem
        {
            StartIndex = 0,
            EndIndex = query.Length
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
        item.Children = FindSubqueries(query);

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

    private List<QueryStructureItem> FindSubqueries(string query)
    {
        var subqueries = new List<QueryStructureItem>();
        
        // Ищем вложенные ВЫБРАТЬ после В, EXISTS и т.д.
        var subqueryPattern = @"\(\s*ВЫБРАТЬ|\(\s*SELECT";
        var matches = Regex.Matches(query, subqueryPattern, RegexOptions.IgnoreCase);
        
        int subqueryNum = 1;
        foreach (Match match in matches)
        {
            // Находим конец подзапроса
            int start = match.Index;
            int end = FindClosingParenthesis(query, start);
            
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
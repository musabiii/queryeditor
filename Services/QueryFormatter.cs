using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace QueryEditor1C.Services;

/// <summary>
/// Сервис для форматирования запросов 1С
/// </summary>
public class QueryFormatter
{
    private const int IndentSize = 4;
    private const string IndentString = "    "; // 4 пробела
    
    // Ключевые слова секций запроса - всегда с новой строки, нулевой отступ
    private static readonly HashSet<string> SectionKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ВЫБРАТЬ", "SELECT",
        "ИЗ", "FROM", 
        "ГДЕ", "WHERE",
        "СГРУППИРОВАТЬ", "GROUP",
        "ИМЕЮЩИЕ", "HAVING",
        "УПОРЯДОЧИТЬ", "ORDER",
        "ОБЪЕДИНИТЬ", "UNION",
        "ПОМЕСТИТЬ", "INTO"
    };
    
    // Ключевые слова условий - с новой строки с отступом
    private static readonly HashSet<string> ConditionKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "И", "AND",
        "ИЛИ", "OR"
    };
    
    // Ключевые слова соединений - с новой строки
    private static readonly HashSet<string> JoinKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "СОЕДИНЕНИЕ", "JOIN",
        "ЛЕВОЕ", "LEFT",
        "ПРАВОЕ", "RIGHT", 
        "ВНУТРЕННЕЕ", "INNER",
        "ВНЕШНЕЕ", "OUTER",
        "ПОЛНОЕ", "FULL"
    };
    
    // Ключевые слова конструкции ВЫБОР
    private static readonly HashSet<string> CaseKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ВЫБОР", "CASE",
        "КОГДА", "WHEN",
        "ТОГДА", "THEN",
        "ИНАЧЕ", "ELSE",
        "КОНЕЦ", "END"
    };
    
    // Все ключевые слова
    private static readonly HashSet<string> AllKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ВЫБРАТЬ", "SELECT", "ИЗ", "FROM", "ГДЕ", "WHERE",
        "СОЕДИНЕНИЕ", "JOIN", "ЛЕВОЕ", "LEFT", "ПРАВОЕ", "RIGHT",
        "ВНУТРЕННЕЕ", "INNER", "ВНЕШНЕЕ", "OUTER", "ПОЛНОЕ", "FULL",
        "ПО", "ON", "И", "AND", "ИЛИ", "OR", "НЕ", "NOT",
        "ПЕРВЫЕ", "TOP", "РАЗЛИЧНЫЕ", "DISTINCT", "КАК", "AS",
        "ПОМЕСТИТЬ", "INTO", "УПОРЯДОЧИТЬ", "ORDER", "BY", "ПО",
        "СГРУППИРОВАТЬ", "GROUP", "ИМЕЮЩИЕ", "HAVING",
        "ОБЪЕДИНИТЬ", "UNION", "ВСЕ", "ALL",
        "ВЫБОР", "CASE", "КОГДА", "WHEN", "ТОГДА", "THEN",
        "ИНАЧЕ", "ELSE", "КОНЕЦ", "END",
        "МЕЖДУ", "BETWEEN", "ЕСТЬ", "IS", "NULL",
        "ПОДОБНО", "LIKE", "ИЕРАРХИЯ", "ИЕРАРХИИ",
        "УНИЧТОЖИТЬ", "ВЫРАЗИТЬ", "В", "IN",
        "ЕСТЬNULL", "ISNULL", "ПОДСТРОКА", "SUBSTRING",
        "ДАТА", "DATE", "НАЧАЛОПЕРИОДА", "BEGINOFPERIOD",
        "КОНЕЦПЕРИОДА", "ENDOFPERIOD", "ДОБАВИТЬКДАТЕ", "DATEADD",
        "РАЗНОСТЬДАТ", "DATEDIFF", "СУММА", "SUM",
        "МАКСИМУМ", "MAX", "МИНИМУМ", "MIN",
        "КОЛИЧЕСТВО", "COUNT", "СРЕДНЕЕ", "AVG",
        "ЗНАЧЕНИЕ", "VALUE"
    };

    public string Format(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return query;

        // Нормализуем запрос: убираем лишние пробелы и переносы
        query = NormalizeQuery(query);
        
        // Разбиваем на токены
        var tokens = Tokenize(query);
        
        // Форматируем
        return FormatTokens(tokens);
    }
    
    private string NormalizeQuery(string query)
    {
        // Заменяем все пробельные символы на один пробел
        query = Regex.Replace(query, @"\s+", " ");
        return query.Trim();
    }

    private List<Token> Tokenize(string query)
    {
        var tokens = new List<Token>();
        var currentToken = new StringBuilder();
        var inString = false;
        var stringChar = '\0';
        var inComment = false;
        
        for (int i = 0; i < query.Length; i++)
        {
            var ch = query[i];
            var nextCh = i < query.Length - 1 ? query[i + 1] : '\0';
            
            // Разделитель временных таблиц (///////////...)
            if (!inString && !inComment && ch == '/')
            {
                // Считаем количество подряд идущих /
                int slashCount = 1;
                int j = i + 1;
                while (j < query.Length && query[j] == '/')
                {
                    slashCount++;
                    j++;
                }
                
                // Если 10 или более / подряд - это разделитель временных таблиц
                if (slashCount >= 10)
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(CreateToken(currentToken.ToString()));
                        currentToken.Clear();
                    }
                    
                    var separator = query.Substring(i, slashCount);
                    tokens.Add(new Token(separator, TokenType.TempTableSeparator));
                    i = j - 1; // Перемещаем указатель на последний /
                    continue;
                }
            }
            
            // Комментарии
            if (!inString && ch == '/' && nextCh == '/')
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(CreateToken(currentToken.ToString()));
                    currentToken.Clear();
                }
                inComment = true;
                currentToken.Append(ch);
                continue;
            }
            
            if (inComment)
            {
                if (ch == '\n' || ch == '\r')
                {
                    tokens.Add(new Token(currentToken.ToString(), TokenType.Comment));
                    currentToken.Clear();
                    inComment = false;
                }
                else
                {
                    currentToken.Append(ch);
                }
                continue;
            }
            
            // Строки в кавычках
            if (!inComment && (ch == '"' || ch == '\''))
            {
                if (!inString)
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(CreateToken(currentToken.ToString()));
                        currentToken.Clear();
                    }
                    inString = true;
                    stringChar = ch;
                    currentToken.Append(ch);
                }
                else if (ch == stringChar)
                {
                    currentToken.Append(ch);
                    tokens.Add(new Token(currentToken.ToString(), TokenType.String));
                    currentToken.Clear();
                    inString = false;
                }
                else
                {
                    currentToken.Append(ch);
                }
                continue;
            }
            
            if (inString)
            {
                currentToken.Append(ch);
                continue;
            }
            
            // Разделители
            if (char.IsWhiteSpace(ch))
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(CreateToken(currentToken.ToString()));
                    currentToken.Clear();
                }
                continue;
            }
            
            // Специальные символы
            if (ch is ',' or '(' or ')' or ';' or '+' or '-' or '*' or '/' or '=' or '<' or '>' or '!')
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(CreateToken(currentToken.ToString()));
                    currentToken.Clear();
                }
                
                // Обрабатываем составные операторы
                if ((ch == '<' && nextCh == '>') || 
                    (ch == '<' && nextCh == '=') || 
                    (ch == '>' && nextCh == '=') ||
                    (ch == '!' && nextCh == '='))
                {
                    tokens.Add(new Token(ch.ToString() + nextCh.ToString(), TokenType.Operator));
                    i++; // Пропускаем следующий символ
                }
                else
                {
                    tokens.Add(new Token(ch.ToString(), TokenType.Punctuation));
                }
                continue;
            }
            
            // Точка
            if (ch == '.')
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(CreateToken(currentToken.ToString()));
                    currentToken.Clear();
                }
                tokens.Add(new Token(".", TokenType.Punctuation));
                continue;
            }
            
            // Амперсанд (параметры)
            if (ch == '&')
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(CreateToken(currentToken.ToString()));
                    currentToken.Clear();
                }
                currentToken.Append(ch);
                continue;
            }
            
            // Обычные символы
            currentToken.Append(ch);
        }
        
        // Добавляем последний токен
        if (currentToken.Length > 0)
        {
            if (inComment)
                tokens.Add(new Token(currentToken.ToString(), TokenType.Comment));
            else
                tokens.Add(CreateToken(currentToken.ToString()));
        }
        
        return tokens;
    }
    
    private Token CreateToken(string text)
    {
        var upperText = text.ToUpperInvariant();
        
        if (text.StartsWith("&"))
            return new Token(text, TokenType.Parameter);
        
        if (AllKeywords.Contains(upperText))
            return new Token(text, TokenType.Keyword);
        
        if (decimal.TryParse(text, out _) || int.TryParse(text, out _))
            return new Token(text, TokenType.Number);
        
        return new Token(text, TokenType.Identifier);
    }

    private string FormatTokens(List<Token> tokens)
    {
        var result = new StringBuilder();
        var indentLevel = 0;
        var inSelectFields = false; // Внутри списка полей ВЫБРАТЬ
        var inCase = false; // Внутри конструкции ВЫБОР
        var caseIndent = 0; // Отступ для ВЫБОР
        var isFirstToken = true;
        Token? prevToken = null;
        
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var nextToken = i < tokens.Count - 1 ? tokens[i + 1] : null;
            var upperValue = token.Value.ToUpperInvariant();
            
            // Если предыдущий токен был ПОМЕСТИТЬ, имя таблицы на той же строке
            if (prevToken != null && (prevToken.Value.ToUpperInvariant() is "ПОМЕСТИТЬ" or "INTO"))
            {
                result.Append(token.Value);
                isFirstToken = false;
                prevToken = token;
                continue;
            }
            
            // Комментарии - на отдельной строке
            if (token.Type == TokenType.Comment)
            {
                if (!isFirstToken)
                    result.AppendLine();
                result.Append(GetIndent(indentLevel));
                result.Append(token.Value);
                isFirstToken = false;
                continue;
            }
            
            // Секции запроса
            if (SectionKeywords.Contains(upperValue))
            {
                if (!isFirstToken)
                    result.AppendLine();
                
                // Сбрасываем отступы
                if (upperValue is "ВЫБРАТЬ" or "SELECT" or "ОБЪЕДИНИТЬ" or "UNION")
                {
                    indentLevel = 0;
                    inSelectFields = upperValue is "ВЫБРАТЬ" or "SELECT";
                }
                else if (upperValue is "ИЗ" or "FROM" or "ГДЕ" or "WHERE" or 
                         "СГРУППИРОВАТЬ" or "GROUP" or "ИМЕЮЩИЕ" or "HAVING" or 
                         "УПОРЯДОЧИТЬ" or "ORDER")
                {
                    indentLevel = 0;
                    inSelectFields = false;
                }
                
                result.Append(GetIndent(indentLevel));
                result.Append(token.Value.ToUpperInvariant());
                
                // Для ПОМЕСТИТЬ имя таблицы идет на той же строке
                if (upperValue is "ПОМЕСТИТЬ" or "INTO")
                {
                    if (nextToken != null && !SectionKeywords.Contains(nextToken.Value.ToUpperInvariant()))
                    {
                        result.Append(" ");
                        indentLevel = 0; // Имя таблицы без отступа
                    }
                }
                else if (!SectionKeywords.Contains(nextToken?.Value.ToUpperInvariant() ?? ""))
                {
                    result.AppendLine();
                    indentLevel = 1;
                }
                
                isFirstToken = false;
                prevToken = token;
                continue;
            }
            
            // Соединения
            if (JoinKeywords.Contains(upperValue))
            {
                // Если предыдущее слово было типом соединения (ЛЕВОЕ, ПРАВОЕ и т.д.), 
                // то текущее (СОЕДИНЕНИЕ/JOIN) идет на той же строке
                if (prevToken != null && 
                    (prevToken.Value.ToUpperInvariant() is "ЛЕВОЕ" or "LEFT" or "ПРАВОЕ" or "RIGHT" or 
                     "ВНУТРЕННЕЕ" or "INNER" or "ВНЕШНЕЕ" or "OUTER" or "ПОЛНОЕ" or "FULL"))
                {
                    result.Append(" ");
                    result.Append(token.Value.ToUpperInvariant());
                    
                    // После СОЕДИНЕНИЕ/JOIN добавляем пробел (таблица будет на той же строке)
                    if (nextToken != null && nextToken.Value.ToUpperInvariant() is not "ПО" and not "ON")
                    {
                        result.Append(" ");
                    }
                }
                else
                {
                    // Первое слово в соединении (ЛЕВОЕ, ПРАВОЕ и т.д.)
                    if (!isFirstToken)
                        result.AppendLine();
                    
                    result.Append(GetIndent(1));
                    result.Append(token.Value.ToUpperInvariant());
                    
                    // Если следующее не СОЕДИНЕНИЕ/JOIN, добавляем перенос
                    if (nextToken != null && 
                        !(nextToken.Value.ToUpperInvariant() is "СОЕДИНЕНИЕ" or "JOIN"))
                    {
                        result.AppendLine();
                    }
                }
                
                isFirstToken = false;
                prevToken = token;
                continue;
            }
            
            // Ключевое слово ПО (для соединений)
            if (upperValue is "ПО" or "ON")
            {
                result.AppendLine();
                result.Append(GetIndent(2));
                result.Append(token.Value.ToUpperInvariant());
                
                if (nextToken != null)
                    result.Append(" ");
                
                isFirstToken = false;
                prevToken = token;
                continue;
            }
            
            // Условия И, ИЛИ
            if (ConditionKeywords.Contains(upperValue))
            {
                result.AppendLine();
                result.Append(GetIndent(1));
                result.Append(token.Value.ToUpperInvariant());
                
                if (nextToken != null)
                    result.Append(" ");
                
                isFirstToken = false;
                prevToken = token;
                continue;
            }
            
            // Конструкция ВЫБОР
            if (CaseKeywords.Contains(upperValue))
            {
                var caseWord = upperValue;
                
                if (caseWord is "ВЫБОР" or "CASE")
                {
                    if (!isFirstToken)
                        result.Append(" ");
                    inCase = true;
                    caseIndent = indentLevel + 1;
                }
                else if (caseWord is "КОГДА" or "WHEN" or "ИНАЧЕ" or "ELSE")
                {
                    if (!isFirstToken)
                        result.AppendLine();
                    indentLevel = caseIndent;
                }
                else if (caseWord is "КОНЕЦ" or "END")
                {
                    if (!isFirstToken)
                        result.AppendLine();
                    indentLevel = caseIndent;
                    inCase = false;
                }
                
                if (caseWord is "КОГДА" or "WHEN" or "ИНАЧЕ" or "ELSE" or "КОНЕЦ" or "END")
                {
                    result.Append(GetIndent(indentLevel));
                }
                
                // Добавляем пробел перед ТОГДА/THEN
                if (caseWord is "ТОГДА" or "THEN" && !isFirstToken)
                {
                    result.Append(" ");
                }
                
                result.Append(token.Value.ToUpperInvariant());
                
                // Добавляем пробел после ТОГДА/THEN и других ключевых слов
                if (caseWord is "ТОГДА" or "THEN")
                {
                    result.Append(" ");
                }
                else if (caseWord is "КОГДА" or "WHEN" or "ИНАЧЕ" or "ELSE" && nextToken != null)
                {
                    result.Append(" ");
                }
                else if (caseWord is "КОНЕЦ" or "END")
                {
                    indentLevel = caseIndent - 1;
                }
                
                isFirstToken = false;
                prevToken = token;
                continue;
            }
            
            // Запятая в списке полей
            if (token.Value == ",")
            {
                result.AppendLine(",");
                result.Append(GetIndent(1));
                isFirstToken = false;
                prevToken = token;
                continue;
            }
            
            // Точка с запятой - разделитель запросов
            if (token.Value == ";")
            {
                result.AppendLine(";");
                result.AppendLine(); // Пустая строка между запросами
                indentLevel = 0;
                isFirstToken = true;
                prevToken = token;
                continue;
            }
            
            // Остальные токены
            if (!isFirstToken && NeedSpaceBetween(prevToken, token))
            {
                result.Append(" ");
            }
            
            result.Append(token.Value);
            
            isFirstToken = false;
            prevToken = token;
        }
        
        return result.ToString().Trim();
    }
    
    private bool NeedSpaceBetween(Token? prev, Token current)
    {
        if (prev == null) return false;
        
        // Не добавляем пробел после открывающей скобки
        if (prev.Value == "(") return false;
        
        // Не добавляем пробел перед закрывающей скобкой
        if (current.Value == ")") return false;
        
        // Не добавляем пробел перед точкой
        if (current.Value == ".") return false;
        
        // Не добавляем пробел после точки
        if (prev.Value == ".") return false;
        
        // Не добавляем пробел перед запятой
        if (current.Value == ",") return false;
        
        // Добавляем пробел после операторов сравнения
        if (prev.Value is "=" or "<>" or "<=" or ">=" or "<" or ">" or "!=")
            return true;
        
        // Не добавляем пробел перед открывающей скобкой после функций
        if (current.Value == "(" && prev.Type == TokenType.Keyword)
        {
            var funcName = prev.Value.ToUpperInvariant();
            if (funcName is "ЗНАЧЕНИЕ" or "VALUE" or "ЕСТЬNULL" or "ISNULL" or 
                "ПОДСТРОКА" or "SUBSTRING" or "ДАТА" or "DATE" or 
                "НАЧАЛОПЕРИОДА" or "BEGINOFPERIOD" or "КОНЕЦПЕРИОДА" or "ENDOFPERIOD" or
                "ДОБАВИТЬКДАТЕ" or "DATEADD" or "РАЗНОСТЬДАТ" or "DATEDIFF" or
                "СУММА" or "SUM" or "МАКСИМУМ" or "MAX" or "МИНИМУМ" or "MIN" or
                "КОЛИЧЕСТВО" or "COUNT" or "СРЕДНЕЕ" or "AVG")
                return false;
        }
        
        return true;
    }
    
    private string GetIndent(int level)
    {
        return new string(' ', level * IndentSize);
    }
}

public class Token
{
    public string Value { get; }
    public TokenType Type { get; }
    
    public Token(string value, TokenType type)
    {
        Value = value;
        Type = type;
    }
}

public enum TokenType
{
    Keyword,
    Identifier,
    String,
    Number,
    Operator,
    Punctuation,
    Comment,
    Parameter,
    NewLine,
    TempTableSeparator  // Разделитель временных таблиц /////////////
}
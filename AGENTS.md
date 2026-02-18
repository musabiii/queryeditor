# 1C Query Editor - Project Context

## Project Goal
Create a desktop text editor for Windows to edit 1C (1C:Enterprise) database queries.

## Technology Evaluation
- **Considered:** C# WPF, C# Avalonia, Electron + Monaco Editor, Python + PyQt
- **Current preference:** Need to decide
- **Development Environment:** VS Code 1.109.3 installed, .NET SDK NOT installed (only runtime)

## Required Features
- ✅ Syntax highlighting for 1C query language
- ✅ Syntax validation
- ✅ Code formatting (auto-format 1C queries)
- ⬜ IntelliSense (optional)
- ⬜ Database connection (optional)
- ⬜ Query execution (optional)

## 1C Query Language Keywords (for syntax highlighting)
### Main keywords
ВЫБРАТЬ, SELECT, ИЗ, FROM, ГДЕ, WHERE, СОЕДИНЕНИЕ, JOIN, ЛЕВОЕ, LEFT, ПРАВОЕ, RIGHT, 
ВНУТРЕННЕЕ, INNER, ВНЕШНЕЕ, OUTER, ПОЛНОЕ, FULL, ПО, ON, И, AND, ИЛИ, OR, НЕ, NOT,
ПЕРВЫЕ, TOP, РАЗЛИЧНЫЕ, DISTINCT, КАК, AS, ПОМЕСТИТЬ, INTO, УПОРЯДОЧИТЬ, ORDER BY,
СГРУППИРОВАТЬ, GROUP BY, ИМЕЮЩИЕ, HAVING, ОБЪЕДИНИТЬ, UNION, ВСЕ, ALL

### Functions
ЕСТЬNULL, ISNULL, ПОДСТРОКА, SUBSTRING, ДАТА, DATE, НАЧАЛОПЕРИОДА, BEGINOFPERIOD,
КОНЕЦПЕРИОДА, ENDOFPERIOD, ДОБАВИТЬКДАТЕ, DATEADD, РАЗНОСТЬДАТ, DATEDIFF,
СУММА, SUM, МАКСИМУМ, MAX, МИНИМУМ, MIN, КОЛИЧЕСТВО, COUNT, СРЕДНЕЕ, AVG

## Next Steps (To Do)
1. Choose technology stack (C# vs Electron vs Python)
2. Install required SDK (.NET SDK for C#, Node.js for Electron, Python for PyQt)
3. Set up project structure
4. Implement basic text editor with syntax highlighting
5. Add 1C-specific formatting rules
6. Implement syntax validation

## Development Commands
```bash
# Check .NET installation
dotnet --version

# Check Node.js (for Electron option)
node --version
npm --version

# Check Python (for PyQt option)
python --version
pip --version
```

## Notes
- 1C query language is case-insensitive
- Syntax similar to SQL but with Russian keywords
- Comments start with //
- Strings use double quotes ""
- Date literals use single quotes with # or just dates in single quotes

## Decisions Log
- [2025-02-18] Project initialized
- [2025-02-18] Evaluating C# vs other technologies for desktop editor
- [2025-02-18] VS Code present, missing .NET SDK for C# development
- [2025-02-18] Selected C# WPF + AvalonEdit
- [2025-02-18] Project structure created
- [2025-02-18] Syntax highlighting for 1C implemented (XSHD file)
- [2025-02-18] Basic UI with menu, toolbar, status bar created
- [2025-02-18] File operations (New, Open, Save) implemented
- [2025-02-18] Theme switching (Light/Dark) implemented
- [2025-02-18] Build successful!
- [2025-02-18] Application runs successfully!
- [2025-02-18] Added .gitignore

## Resources
- https://aka.ms/dotnet/download - Download .NET SDK
- AvalonEdit - Syntax highlighting library for C#
- Monaco Editor - Syntax highlighting for Electron
- Scintilla/PyQt - Syntax highlighting for Python

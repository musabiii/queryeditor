# 1C Query Editor - Agent Instructions

## Project Overview
C# WPF desktop text editor for 1C:Enterprise database queries with syntax highlighting, auto-formatting, and query structure navigation.

**Stack:** .NET 10, WPF, AvalonEdit, C# 12

## Build Commands

```bash
# Build project
dotnet build

# Build release
dotnet build -c Release

# Run application
dotnet run

# Clean build artifacts
dotnet clean

# Restore packages
dotnet restore
```

**Note:** No test project exists yet. To add tests, create a new xUnit/NUnit project with `dotnet new xunit -n QueryEditor1C.Tests`.

## Code Style Guidelines

### Formatting
- **Indentation:** 4 spaces (no tabs)
- **Line endings:** CRLF (Windows)
- **Max line length:** 120 characters
- **Braces:** K&R style (opening brace on same line)
- **Namespaces:** File-scoped (no curly braces)

### Naming Conventions
- **Classes/Structs/Interfaces:** PascalCase (`QueryFormatter`, `ITokenizer`)
- **Methods:** PascalCase (`FormatQuery`, `LoadSyntaxHighlighting`)
- **Properties:** PascalCase (`CurrentFilePath`, `IsModified`)
- **Fields:**
  - Private: `_camelCase` (`_formatter`, `_structureParser`)
  - Public: PascalCase
  - Constants/Readonly: PascalCase
- **Parameters:** camelCase
- **Local variables:** camelCase
- **Generic types:** PascalCase with T prefix (`TResult`, `TInput`)

### Types & Nullability
- **Nullable reference types:** Enabled (`<Nullable>enable</Nullable>`)
- Use `?` suffix for nullable types: `string? currentFilePath`
- Use `var` when type is obvious from right side
- Use target-typed new: `new()` instead of `new Dictionary<string, int>()`
- Prefer `is` pattern matching: `if (obj is string s)`

### Imports
- **Implicit usings:** Enabled (don't import common namespaces like `System`, `System.Linq`)
- Add explicit imports only for non-default namespaces
- Group imports: System → Third-party → Project
- No wildcard imports

### Error Handling
- Use exceptions for exceptional cases only
- Prefer `try-catch` over error codes
- Log errors before showing UI messages
- Always check for null before dereferencing nullable types

### Comments
- XML documentation for public APIs (`/// <summary>`)
- Russian comments acceptable for 1C domain concepts
- Avoid obvious comments (`// increment counter`)
- Use `// TODO:` for temporary code

### WPF/XAML Specific
- Event handlers: `ControlName_EventName` (e.g., `OpenFile_Click`)
- Private methods: PascalCase
- Use `nameof()` for property names in bindings
- Prefer data binding over direct control manipulation

### 1C Query Domain
- Keywords: Mixed Russian/English (ВЫБРАТЬ, SELECT, ИЗ, FROM)
- Preserve original casing in formatter output
- Support both Cyrillic and Latin aliases

## Project Structure
```
├── *.xaml              # WPF views
├── *.xaml.cs           # Code-behind
├── Services/           # Business logic
│   ├── QueryFormatter.cs
│   └── QueryStructureParser.cs
├── QueryEditor1C/
│   └── Resources/      # Embedded resources
└── Syntax/             # Syntax definitions
```

## Common Tasks

```bash
# Add new class
dotnet new class -n MyClass -o Services

# Add package reference
dotnet add package PackageName

# Build single file
dotnet build QueryEditor1C.csproj
```

## Key Dependencies
- **AvalonEdit 6.3.1.120** - Syntax highlighting editor
- **.NET 10** - Target framework

## Keyboard Shortcuts
- **Ctrl+N** - New file
- **Ctrl+O** - Open file
- **Ctrl+S** - Save file
- **Ctrl+F** - Find
- **Ctrl+H** - Replace

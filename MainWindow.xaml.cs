using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;
using QueryEditor1C.Services;

namespace QueryEditor1C;

public partial class MainWindow : Window
{
    private string? currentFilePath;
    private readonly QueryFormatter _formatter = new();
    private readonly QueryStructureParser _structureParser = new();

    public MainWindow()
    {
        InitializeComponent();
        LoadSyntaxHighlighting();
        textEditor.TextChanged += TextEditor_TextChanged;
        textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        
        // Горячие клавиши
        this.KeyDown += MainWindow_KeyDown;
        
        // Обновление структуры при изменении текста
        textEditor.TextChanged += (s, e) => UpdateQueryStructurePanel();
        
        // Пример запроса
        textEditor.Text = @"ВЫБРАТЬ
    Номенклатура.Наименование КАК Наименование,
    СУММА(Документ.Количество) КАК Количество
ИЗ
    Документ.РеализацияТоваровУслуг.Товары КАК Документ
    ЛЕВОЕ СОЕДИНЕНИЕ Справочник.Номенклатура КАК Номенклатура
        ПО Документ.Номенклатура = Номенклатура.Ссылка
ГДЕ
    Документ.Ссылка.Дата МЕЖДУ &ДатаНачала И &ДатаОкончания
СГРУППИРОВАТЬ ПО
    Номенклатура.Наименование";
    }

    private void LoadSyntaxHighlighting()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "QueryEditor1C.Resources.1CQuerySyntax.xshd";
            
            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var reader = new System.Xml.XmlTextReader(stream))
                    {
                        var highlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        textEditor.SyntaxHighlighting = highlighting;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки подсветки синтаксиса: {ex.Message}", "Ошибка", 
                          MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void TextEditor_TextChanged(object? sender, EventArgs e)
    {
        UpdateStatus();
    }

    private void Caret_PositionChanged(object? sender, EventArgs e)
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        int line = textEditor.TextArea.Caret.Line;
        int column = textEditor.TextArea.Caret.Column;
        cursorPosition.Text = $"Строка: {line}, Колонка: {column}";
        
        if (currentFilePath != null)
        {
            statusText.Text = $"{currentFilePath}*";
        }
    }

    private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.F:
                    Find_Click(sender, e);
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.H:
                    Replace_Click(sender, e);
                    e.Handled = true;
                    break;
            }
        }
    }

    #region Menu Handlers

    private void NewFile_Click(object sender, RoutedEventArgs e)
    {
        textEditor.Clear();
        currentFilePath = null;
        Title = "Редактор запросов 1C - Новый файл";
        statusText.Text = "Новый файл";
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Файлы запросов 1C (*.txt;*.query)|*.txt;*.query|Все файлы (*.*)|*.*",
            Title = "Открыть файл"
        };

        if (dialog.ShowDialog() == true)
        {
            currentFilePath = dialog.FileName;
            textEditor.Text = File.ReadAllText(currentFilePath);
            Title = $"Редактор запросов 1C - {Path.GetFileName(currentFilePath)}";
            statusText.Text = $"Открыт: {currentFilePath}";
        }
    }

    private void SaveFile_Click(object sender, RoutedEventArgs e)
    {
        if (currentFilePath == null)
        {
            SaveAsFile_Click(sender, e);
        }
        else
        {
            File.WriteAllText(currentFilePath, textEditor.Text);
            statusText.Text = $"Сохранено: {currentFilePath}";
        }
    }

    private void SaveAsFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Файлы запросов 1C (*.txt;*.query)|*.txt;*.query|Все файлы (*.*)|*.*",
            Title = "Сохранить файл"
        };

        if (dialog.ShowDialog() == true)
        {
            currentFilePath = dialog.FileName;
            File.WriteAllText(currentFilePath, textEditor.Text);
            Title = $"Редактор запросов 1C - {Path.GetFileName(currentFilePath)}";
            statusText.Text = $"Сохранено: {currentFilePath}";
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        textEditor.Undo();
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        textEditor.Redo();
    }

    private void Find_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FindReplaceDialog(textEditor)
        {
            Owner = this,
            Title = "Поиск"
        };
        dialog.Show();
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FindReplaceDialog(textEditor)
        {
            Owner = this,
            Title = "Поиск и замена"
        };
        dialog.Show();
    }

    private void ShowWhitespace_Click(object sender, RoutedEventArgs e)
    {
        var isChecked = btnShowWhitespace.IsChecked ?? false;
        textEditor.Options.ShowSpaces = isChecked;
        textEditor.Options.ShowTabs = isChecked;
        textEditor.Options.ShowEndOfLine = isChecked;
    }

    private void FormatQuery_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var currentText = textEditor.Text;
            if (string.IsNullOrWhiteSpace(currentText))
            {
                statusText.Text = "Нечего форматировать";
                return;
            }
            
            var formattedText = _formatter.Format(currentText);
            textEditor.Text = formattedText;
            statusText.Text = "Запрос отформатирован";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка форматирования: {ex.Message}", "Ошибка", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
            statusText.Text = "Ошибка форматирования";
        }
    }

    private void ValidateQuery_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Проверка синтаксиса будет реализована в следующей версии", "Информация");
    }

    private void ThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        // Проверяем, что контролы уже инициализированы
        if (textEditor == null) return;
        
        if (cmbThemes.SelectedIndex == 1)
        {
            textEditor.Background = new SolidColorBrush(Colors.Black);
            textEditor.Foreground = new SolidColorBrush(Colors.White);
        }
        else
        {
            textEditor.Background = new SolidColorBrush(Colors.White);
            textEditor.Foreground = new SolidColorBrush(Colors.Black);
        }
    }

    #endregion

    #region Панель структуры запроса
    
    /// <summary>
    /// Обновляет панель структуры запроса
    /// </summary>
    private void UpdateQueryStructurePanel()
    {
        if (queryStructureTree == null) return;
        
        var structure = _structureParser.Parse(textEditor.Text);
        queryStructureTree.Items.Clear();
        
        foreach (var item in structure)
        {
            var treeItem = new TreeViewItem
            {
                Header = item.DisplayName,
                Tag = item,
                IsExpanded = true
            };
            
            // Добавляем иконку в зависимости от типа
            if (item.Type == "TempTable")
            {
                treeItem.Header = "📦 " + item.DisplayName;
            }
            else if (item.Type == "Query")
            {
                treeItem.Header = "📝 " + item.DisplayName;
            }
            
            // Добавляем подзапросы
            foreach (var child in item.Children)
            {
                var childItem = new TreeViewItem
                {
                    Header = "📋 " + child.DisplayName,
                    Tag = child
                };
                treeItem.Items.Add(childItem);
            }
            
            queryStructureTree.Items.Add(treeItem);
        }
    }
    
    /// <summary>
    /// Обработчик выбора элемента в дереве структуры
    /// </summary>
    private void QueryStructureTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TreeViewItem treeItem && treeItem.Tag is QueryStructureItem item)
        {
            // Переходим к выбранному запросу в редакторе
            if (item.StartIndex >= 0 && item.StartIndex < textEditor.Text.Length)
            {
                var line = textEditor.Document.GetLineByOffset(item.StartIndex);
                textEditor.ScrollToLine(line.LineNumber);
                textEditor.Select(item.StartIndex, Math.Min(100, item.EndIndex - item.StartIndex));
            }
        }
    }
    
    #endregion
}

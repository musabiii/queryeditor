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
    private List<QueryStructureItem> _currentStructure = new();
    private bool _isUpdatingFromEditor = false;

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
        
        // Обновляем выделение в дереве структуры при перемещении курсора
        if (!_isUpdatingFromEditor)
        {
            UpdateStructureTreeSelection();
        }
    }
    
    /// <summary>
    /// Выделяет в дереве структуры элемент, соответствующий текущей позиции курсора
    /// </summary>
    private void UpdateStructureTreeSelection()
    {
        if (_currentStructure.Count == 0 || queryStructureTree == null) return;
        
        var caretOffset = textEditor.TextArea.Caret.Offset;
        
        // Ищем элемент, в котором находится курсор
        QueryStructureItem? selectedItem = null;
        foreach (var item in _currentStructure)
        {
            if (caretOffset >= item.StartIndex && caretOffset <= item.EndIndex)
            {
                selectedItem = item;
                break;
            }
        }
        
        if (selectedItem != null)
        {
            // Находим соответствующий TreeViewItem
            foreach (TreeViewItem treeItem in queryStructureTree.Items)
            {
                if (treeItem.Tag is QueryStructureItem item && item == selectedItem)
                {
                    _isUpdatingFromEditor = true;
                    treeItem.IsSelected = true;
                    
                    // Обновляем стрелки связей если это временная таблица
                    if (selectedItem.Type == "TempTable")
                    {
                        UpdateStructureTreeArrows(selectedItem);
                    }
                    else
                    {
                        ClearStructureTreeArrows();
                    }
                    
                    _isUpdatingFromEditor = false;
                    break;
                }
            }
        }
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
        
        _currentStructure = _structureParser.Parse(textEditor.Text);
        queryStructureTree.Items.Clear();
        
        foreach (var item in _currentStructure)
        {
            var treeItem = new TreeViewItem
            {
                Tag = item,
                IsExpanded = true
            };
            
            // Создаем TextBlock для заголовка с возможностью изменения цвета
            var headerText = new System.Windows.Controls.TextBlock();
            
            // Добавляем иконку в зависимости от типа
            if (item.Type == "TempTable")
            {
                headerText.Text = "📦 " + item.DisplayName;
                
                // Если временная таблица не используется - делаем серым
                if (!item.IsUsed)
                {
                    headerText.Foreground = System.Windows.Media.Brushes.Gray;
                }
            }
            else if (item.Type == "Query")
            {
                headerText.Text = "📝 " + item.DisplayName;
            }
            
            treeItem.Header = headerText;
            
            queryStructureTree.Items.Add(treeItem);
        }
    }
    
    /// <summary>
    /// Обработчик выбора элемента в дереве структуры
    /// </summary>
    private void QueryStructureTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        System.Diagnostics.Debug.WriteLine($"SelectedItemChanged called. _isUpdatingFromEditor={_isUpdatingFromEditor}, NewValue={e.NewValue?.GetType().Name}");
        
        if (_isUpdatingFromEditor) return;
        
        // Проверяем, что новое значение не null
        if (e.NewValue == null) return;
        
        // Получаем выбранный элемент
        TreeViewItem? treeItem = null;
        QueryStructureItem? item = null;
        
        if (e.NewValue is TreeViewItem tvi)
        {
            treeItem = tvi;
            item = tvi.Tag as QueryStructureItem;
            System.Diagnostics.Debug.WriteLine($"Found TreeViewItem with Tag={item?.DisplayName}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"NewValue is not TreeViewItem, it's {e.NewValue.GetType().Name}");
        }
        
        if (treeItem != null && item != null)
        {
            // Обновляем стрелки связей для временных таблиц
            if (item.Type == "TempTable")
            {
                UpdateStructureTreeArrows(item);
            }
            else
            {
                // Сбрасываем стрелки если выбран не temp table
                ClearStructureTreeArrows();
            }
            
            // Переходим к выбранному запросу в редакторе
            if (item.StartIndex >= 0 && item.StartIndex < textEditor.Text.Length)
            {
                _isUpdatingFromEditor = true;
                try
                {
                    var line = textEditor.Document.GetLineByOffset(item.StartIndex);
                    textEditor.ScrollToLine(line.LineNumber);
                    
                    // Выделяем весь текст элемента
                    var length = Math.Min(item.EndIndex - item.StartIndex, textEditor.Text.Length - item.StartIndex);
                    textEditor.Select(item.StartIndex, length);
                    
                    // Фокусируем редактор
                    textEditor.Focus();
                }
                finally
                {
                    _isUpdatingFromEditor = false;
                }
            }
        }
    }
    
    /// <summary>
    /// Обновляет стрелки связей в дереве структуры при выборе временной таблицы
    /// </summary>
    private void UpdateStructureTreeArrows(QueryStructureItem selectedTable)
    {
        var (tablesThatUseSelected, tablesUsedInSelected) = _structureParser.GetRelatedTempTables(_currentStructure, selectedTable);
        
        // Собираем имена таблиц для быстрого поиска
        var tablesThatUseNames = tablesThatUseSelected.Select(t => t.DisplayName).ToHashSet();
        var tablesUsedNames = tablesUsedInSelected.Select(t => t.DisplayName).ToHashSet();
        
        // Обновляем все элементы дерева
        foreach (TreeViewItem treeItem in queryStructureTree.Items)
        {
            if (treeItem.Tag is QueryStructureItem item && item.Type == "TempTable")
            {
                UpdateTreeItemHeader(treeItem, item, tablesThatUseNames, tablesUsedNames);
            }
        }
    }
    
    /// <summary>
    /// Сбрасывает стрелки в дереве структуры
    /// </summary>
    private void ClearStructureTreeArrows()
    {
        foreach (TreeViewItem treeItem in queryStructureTree.Items)
        {
            if (treeItem.Tag is QueryStructureItem item)
            {
                // Просто обновляем заголовок без стрелок
                var headerText = new System.Windows.Controls.TextBlock();
                
                if (item.Type == "TempTable")
                {
                    headerText.Text = "📦 " + item.DisplayName;
                    if (!item.IsUsed)
                    {
                        headerText.Foreground = System.Windows.Media.Brushes.Gray;
                    }
                }
                else if (item.Type == "Query")
                {
                    headerText.Text = "📝 " + item.DisplayName;
                }
                
                treeItem.Header = headerText;
            }
        }
    }
    
    /// <summary>
    /// Обновляет заголовок элемента дерева с учетом стрелок связей
    /// </summary>
    private void UpdateTreeItemHeader(TreeViewItem treeItem, QueryStructureItem item, HashSet<string> tablesThatUse, HashSet<string> tablesUsedIn)
    {
        var headerText = new System.Windows.Controls.TextBlock();
        var displayName = item.DisplayName;
        
        // Определяем стрелки (обе справа)
        string arrow = "";
        
        if (tablesThatUse.Contains(displayName))
        {
            // Эта таблица использует выделенную → стрелка вправо справа
            arrow = " →";
        }
        else if (tablesUsedIn.Contains(displayName))
        {
            // Эта таблица используется в выделенной ← стрелка влево справа
            arrow = " ←";
        }
        
        // Формируем текст: 📦 ИмяТаблицы → или 📦 ИмяТаблицы ←
        headerText.Text = "📦 " + displayName + arrow;
        
        // Если временная таблица не используется - делаем серым
        if (!item.IsUsed)
        {
            headerText.Foreground = System.Windows.Media.Brushes.Gray;
        }
        
        treeItem.Header = headerText;
    }
    
    /// <summary>
    /// Обработчик клика мышью в дереве структуры
    /// </summary>
    private void QueryStructureTree_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("PreviewMouseLeftButtonDown in TreeView");
        
        // Получаем элемент, на который кликнули
        var source = e.OriginalSource as System.Windows.DependencyObject;
        if (source == null) return;
        
        // Ищем TreeViewItem вверх по визуальному дереву
        var treeViewItem = FindParent<TreeViewItem>(source);
        if (treeViewItem != null)
        {
            System.Diagnostics.Debug.WriteLine($"Found TreeViewItem: {treeViewItem.Tag?.GetType().Name}");
            
            // Устанавливаем выделение
            if (treeViewItem.Tag is QueryStructureItem item)
            {
                System.Diagnostics.Debug.WriteLine($"Item: {item.DisplayName}, StartIndex: {item.StartIndex}");
            }
        }
    }
    
    /// <summary>
    /// Находит родительский элемент указанного типа в визуальном дереве
    /// </summary>
    private T? FindParent<T>(System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
    {
        while (child != null)
        {
            if (child is T parent)
                return parent;
            child = System.Windows.Media.VisualTreeHelper.GetParent(child);
        }
        return null;
    }
    
    #endregion
}

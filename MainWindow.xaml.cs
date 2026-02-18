using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;

namespace QueryEditor1C;

public partial class MainWindow : Window
{
    private string? currentFilePath;

    public MainWindow()
    {
        InitializeComponent();
        LoadSyntaxHighlighting();
        textEditor.TextChanged += TextEditor_TextChanged;
        textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        
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
        MessageBox.Show("Поиск будет реализован в следующей версии", "Информация");
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Замена будет реализована в следующей версии", "Информация");
    }

    private void FormatQuery_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Форматирование будет реализовано в следующей версии", "Информация");
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
}
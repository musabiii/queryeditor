using System;
using System.Text.RegularExpressions;
using System.Windows;
using ICSharpCode.AvalonEdit;

namespace QueryEditor1C;

public partial class FindReplaceDialog : Window
{
    private readonly TextEditor _editor;
    private int _lastFindIndex = -1;

    public FindReplaceDialog(TextEditor editor)
    {
        InitializeComponent();
        _editor = editor;
        
        // Если есть выделенный текст, используем его для поиска
        if (!string.IsNullOrEmpty(_editor.SelectedText))
        {
            txtFind.Text = _editor.SelectedText;
        }
    }

    private void FindNext_Click(object sender, RoutedEventArgs e)
    {
        FindNext();
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        if (_editor.SelectedText == txtFind.Text)
        {
            _editor.Document.Replace(_editor.SelectionStart, _editor.SelectionLength, txtReplace.Text);
        }
        FindNext();
    }

    private void ReplaceAll_Click(object sender, RoutedEventArgs e)
    {
        var searchText = txtFind.Text;
        var replaceText = txtReplace.Text;
        
        if (string.IsNullOrEmpty(searchText))
        {
            MessageBox.Show("Введите текст для поиска", "Поиск", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var compareOptions = chkCaseSensitive.IsChecked == true 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;

        var text = _editor.Text;
        int count = 0;
        
        if (chkWholeWord.IsChecked == true)
        {
            // Замена целых слов с использованием регулярных выражений
            var pattern = Regex.Escape(searchText);
            var regex = new Regex($@"\b{pattern}\b", chkCaseSensitive.IsChecked == true ? RegexOptions.None : RegexOptions.IgnoreCase);
            var matches = regex.Matches(text);
            count = matches.Count;
            text = regex.Replace(text, replaceText);
        }
        else
        {
            // Обычная замена
            int index = 0;
            while ((index = text.IndexOf(searchText, index, compareOptions)) != -1)
            {
                text = text.Remove(index, searchText.Length).Insert(index, replaceText);
                index += replaceText.Length;
                count++;
            }
        }

        _editor.Text = text;
        MessageBox.Show($"Заменено вхождений: {count}", "Замена", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private bool FindNext()
    {
        var searchText = txtFind.Text;
        if (string.IsNullOrEmpty(searchText))
        {
            MessageBox.Show("Введите текст для поиска", "Поиск", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        var compareOptions = chkCaseSensitive.IsChecked == true 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;

        var text = _editor.Text;
        var startIndex = _lastFindIndex >= 0 ? _lastFindIndex + 1 : _editor.CaretOffset;

        if (chkWholeWord.IsChecked == true)
        {
            // Поиск целых слов с использованием регулярных выражений
            var pattern = Regex.Escape(searchText);
            var regex = new Regex($@"\b{pattern}\b", chkCaseSensitive.IsChecked == true ? RegexOptions.None : RegexOptions.IgnoreCase);
            var match = regex.Match(text, startIndex);
            
            if (match.Success)
            {
                _lastFindIndex = match.Index;
                _editor.Select(match.Index, match.Length);
                _editor.TextArea.Caret.BringCaretToView();
                return true;
            }
            
            // Поиск с начала
            match = regex.Match(text);
            if (match.Success && match.Index < startIndex)
            {
                _lastFindIndex = match.Index;
                _editor.Select(match.Index, match.Length);
                _editor.TextArea.Caret.BringCaretToView();
                return true;
            }
        }
        else
        {
            // Обычный поиск
            var index = text.IndexOf(searchText, startIndex, compareOptions);
            
            if (index >= 0)
            {
                _lastFindIndex = index;
                _editor.Select(index, searchText.Length);
                _editor.TextArea.Caret.BringCaretToView();
                return true;
            }

            // Поиск с начала текста
            index = text.IndexOf(searchText, 0, startIndex, compareOptions);
            if (index >= 0)
            {
                _lastFindIndex = index;
                _editor.Select(index, searchText.Length);
                _editor.TextArea.Caret.BringCaretToView();
                return true;
            }
        }

        MessageBox.Show("Текст не найден", "Поиск", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }
}
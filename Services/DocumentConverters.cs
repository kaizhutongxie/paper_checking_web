using System;
using System.Text;
using System.Text.RegularExpressions;
using Xceed.Words.NET;
using paper_checking_web.Models;

namespace paper_checking_web.Services;

/// <summary>
/// 文档转换器接口 - 仅支持 Word 文档
/// </summary>
public interface IDocumentConverter
{
    string? ConvertToString(string filePath, string blockText);
    Task<string> ConvertToTextAsync(string filePath);
    bool SupportsExtension(string extension);
}

/// <summary>
/// Word 文档转换器 (使用 DocX)
/// 将 Word (.docx) 文件转换为纯文本
/// </summary>
public class WordConverter : IDocumentConverter
{
    public bool SupportsExtension(string extension)
    {
        return extension.Equals("docx", StringComparison.OrdinalIgnoreCase);
    }

    public string? ConvertToString(string filePath, string blockText)
    {
        try
        {
            // 使用 DocX 库读取 DOCX 文件
            var doc = DocX.Load(filePath);
            using (doc)
            {
                var text = doc.Text;
                
                // 清理文本：替换换行符，只保留中文和标点
                text = text.Replace("#", "").Replace('\r', '#').Replace('\n', '#');
                text = Regex.Replace(text, @"[^\u4e00-\u9fa5\《\》\（\）\——\；\，\。\""\！\#]", "");
                text = new Regex("[#]+").Replace(text, "@@").Trim();
                
                return TextFormat(text, blockText);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Word 转换失败：{ex.Message}");
            return null;
        }
    }

    public async Task<string> ConvertToTextAsync(string filePath)
    {
        var result = ConvertToString(filePath, string.Empty);
        if (result == null)
            throw new Exception($"无法读取 Word 文件：{filePath}");
        
        return await Task.FromResult(result);
    }

    protected string TextFormat(string text, string blockText)
    {
        if (string.IsNullOrEmpty(blockText))
            return text;

        var blocks = blockText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var block in blocks)
        {
            text = text.Replace(block.Trim(), string.Empty);
        }
        return text;
    }
}

/// <summary>
/// 转换器工厂 - 仅支持 Word 文档
/// </summary>
public class ConverterFactory
{
    private readonly WordConverter _wordConverter;

    public ConverterFactory()
    {
        _wordConverter = new WordConverter();
    }

    public IDocumentConverter? GetConverter(string extension, SystemSettings settings)
    {
        var lowerExt = extension.ToLowerInvariant();
        
        if (lowerExt == "docx")
            return _wordConverter;
        
        return null;
    }
}

using System;
using FileAnalysisService.DTOs;

namespace FileAnalysisService.Services;

public class AnalysisService
{
    public int GetParagraphsNum(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var paragraphs = text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        return paragraphs.Length;
    }

    public int GetWordsNum(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var words = text
            .Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        return words.Length;
    }

    public int GetSymbolsNum(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return text.Length;
    }

    public AnalysisDto Analyse(Guid fileId, string text)
    {
        return new AnalysisDto
        {
            FileId=fileId,
            Paragraphs=GetParagraphsNum(text),
            Words=GetWordsNum(text),
            Symbols=GetSymbolsNum(text),
        };
    }
}

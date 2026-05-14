using System.Text.RegularExpressions;

namespace Apotheca.Api.Utils;

public static class MarkdownUtils
{
    private static readonly Regex FencedCodeBlock   = new(@"```[\s\S]*?```",   RegexOptions.Compiled);
    private static readonly Regex InlineCode        = new(@"`(.+?)`",          RegexOptions.Compiled);
    private static readonly Regex Images            = new(@"!\[.*?\]\(.*?\)",  RegexOptions.Compiled);
    private static readonly Regex Links             = new(@"\[(.+?)\]\(.*?\)", RegexOptions.Compiled);
    private static readonly Regex Headings          = new(@"^#{1,6}\s+",       RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex BoldItalic        = new(@"\*{3}(.+?)\*{3}", RegexOptions.Compiled);
    private static readonly Regex BoldAsterisks     = new(@"\*{2}(.+?)\*{2}", RegexOptions.Compiled);
    private static readonly Regex BoldUnderscores   = new(@"_{2}(.+?)_{2}",   RegexOptions.Compiled);
    private static readonly Regex ItalicAsterisks   = new(@"\*(.+?)\*",       RegexOptions.Compiled);
    private static readonly Regex ItalicUnderscores = new(@"_(.+?)_",         RegexOptions.Compiled);
    private static readonly Regex Strikethrough     = new(@"~~(.+?)~~",       RegexOptions.Compiled);
    private static readonly Regex Blockquotes       = new(@"^>\s*",           RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex UnorderedList     = new(@"^\s*[-*+]\s+",    RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex OrderedList       = new(@"^\s*\d+\.\s+",    RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex HorizontalRule    = new(@"^[-*_]{3,}\s*$",  RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex Whitespace        = new(@"\s+",             RegexOptions.Compiled);

    public static string? StripMarkdown(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return null;

        var text = markdown;
        text = FencedCodeBlock.Replace(text, " ");
        text = InlineCode.Replace(text, "$1");
        text = Images.Replace(text, "");
        text = Links.Replace(text, "$1");
        text = Headings.Replace(text, "");
        text = BoldItalic.Replace(text, "$1");
        text = BoldAsterisks.Replace(text, "$1");
        text = BoldUnderscores.Replace(text, "$1");
        text = ItalicAsterisks.Replace(text, "$1");
        text = ItalicUnderscores.Replace(text, "$1");
        text = Strikethrough.Replace(text, "$1");
        text = Blockquotes.Replace(text, "");
        text = UnorderedList.Replace(text, "");
        text = OrderedList.Replace(text, "");
        text = HorizontalRule.Replace(text, "");
        text = Whitespace.Replace(text, " ");

        return text.Trim() is { Length: > 0 } trimmed ? trimmed : null;
    }
}

using Apotheca.Api.Utils;

namespace Apotheca.Api.Tests.Utils;

[TestFixture]
public class MarkdownUtilsTests
{
    // --- Null / empty ---

    [Test]
    public void StripMarkdown_WhenNull_ReturnsNull()
    {
        Assert.That(MarkdownUtils.StripMarkdown(null), Is.Null);
    }

    [Test]
    public void StripMarkdown_WhenEmpty_ReturnsNull()
    {
        Assert.That(MarkdownUtils.StripMarkdown(""), Is.Null);
    }

    [Test]
    public void StripMarkdown_WhenWhitespaceOnly_ReturnsNull()
    {
        Assert.That(MarkdownUtils.StripMarkdown("   \n\t  "), Is.Null);
    }

    [Test]
    public void StripMarkdown_WhenPlainText_ReturnsUnchanged()
    {
        Assert.That(MarkdownUtils.StripMarkdown("Hello world"), Is.EqualTo("Hello world"));
    }

    // --- Code ---

    [Test]
    public void StripMarkdown_FencedCodeBlock_Removed()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("Example:\n```\nvar x = 1;\n```\nDone"),
            Is.EqualTo("Example: Done"));
    }

    [Test]
    public void StripMarkdown_InlineCode_SyntaxRemovedTextKept()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("Call `doSomething()` here"),
            Is.EqualTo("Call doSomething() here"));
    }

    // --- Links and images ---

    [Test]
    public void StripMarkdown_Link_SyntaxRemovedTextKept()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("See [the docs](https://example.com) for more"),
            Is.EqualTo("See the docs for more"));
    }

    [Test]
    public void StripMarkdown_Image_Removed()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("Here is an image: ![alt text](image.png)"),
            Is.EqualTo("Here is an image:"));
    }

    // --- Headings ---

    [Test]
    [TestCase("# H1 heading")]
    [TestCase("## H2 heading")]
    [TestCase("### H3 heading")]
    [TestCase("###### H6 heading")]
    public void StripMarkdown_Headings_SyntaxRemoved(string input)
    {
        var result = MarkdownUtils.StripMarkdown(input);
        Assert.That(result, Does.Not.Contain("#"));
    }

    [Test]
    public void StripMarkdown_Heading_TextKept()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("## Section heading\nSome content"),
            Is.EqualTo("Section heading Some content"));
    }

    // --- Emphasis ---

    [Test]
    public void StripMarkdown_BoldItalicAsterisks_SyntaxRemovedTextKept()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("This is ***important*** text"),
            Is.EqualTo("This is important text"));
    }

    [Test]
    public void StripMarkdown_BoldAsterisks_SyntaxRemovedTextKept()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("This is **bold** text"),
            Is.EqualTo("This is bold text"));
    }

    [Test]
    public void StripMarkdown_BoldUnderscores_SyntaxRemovedTextKept()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("This is __bold__ text"),
            Is.EqualTo("This is bold text"));
    }

    [Test]
    public void StripMarkdown_ItalicAsterisks_SyntaxRemovedTextKept()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("This is *italic* text"),
            Is.EqualTo("This is italic text"));
    }

    [Test]
    public void StripMarkdown_ItalicUnderscores_SyntaxRemovedTextKept()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("This is _italic_ text"),
            Is.EqualTo("This is italic text"));
    }

    [Test]
    public void StripMarkdown_Strikethrough_SyntaxRemovedTextKept()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("This is ~~wrong~~ right"),
            Is.EqualTo("This is wrong right"));
    }

    // --- Block elements ---

    [Test]
    public void StripMarkdown_Blockquote_SyntaxRemoved()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("> This is a quote"),
            Is.EqualTo("This is a quote"));
    }

    [Test]
    public void StripMarkdown_UnorderedList_BulletRemoved()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("- First\n- Second"),
            Is.EqualTo("First Second"));
    }

    [Test]
    public void StripMarkdown_OrderedList_NumberRemoved()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("1. First\n2. Second"),
            Is.EqualTo("First Second"));
    }

    [Test]
    public void StripMarkdown_HorizontalRule_Removed()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("Above\n---\nBelow"),
            Is.EqualTo("Above Below"));
    }

    // --- Whitespace ---

    [Test]
    public void StripMarkdown_MultipleNewlines_CollapsedToSpace()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("First line\n\nSecond line"),
            Is.EqualTo("First line Second line"));
    }

    [Test]
    public void StripMarkdown_LeadingAndTrailingWhitespace_Trimmed()
    {
        Assert.That(
            MarkdownUtils.StripMarkdown("  some text  "),
            Is.EqualTo("some text"));
    }

    // --- Combined ---

    [Test]
    public void StripMarkdown_ComplexDocument_StripsAll()
    {
        var input = """
            # Title

            Some **bold** and *italic* text with a [link](https://example.com).

            ```
            code block
            ```

            - Item one
            - Item two
            """;

        var result = MarkdownUtils.StripMarkdown(input);

        Assert.That(result, Does.Not.Contain("#"));
        Assert.That(result, Does.Not.Contain("**"));
        Assert.That(result, Does.Not.Contain("*"));
        Assert.That(result, Does.Not.Contain("["));
        Assert.That(result, Does.Not.Contain("`"));
        Assert.That(result, Does.Not.Contain("-"));
        Assert.That(result, Does.Contain("Title"));
        Assert.That(result, Does.Contain("bold"));
        Assert.That(result, Does.Contain("italic"));
        Assert.That(result, Does.Contain("link"));
        Assert.That(result, Does.Contain("Item one"));
        Assert.That(result, Does.Contain("Item two"));
    }
}

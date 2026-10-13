using ZynstormECFPlatform.Services.Ri.Html;
using Xunit;

namespace ZynstormECFPlatform.Tests.Ri.Html;

public class RiHtmlTemplateValidatorTests
{
    [Fact]
    public void Validate_Reference_IsValid_AndKeepsMarkers()
    {
        var result = RiHtmlTemplateValidator.Validate(RiHtmlTestTemplates.Reference);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains("{{Descripcion}}", result.SanitizedHtml);
        Assert.Contains("{{Total}}", result.SanitizedHtml);
    }

    [Fact]
    public void Validate_WithoutItemsRow_IsRejected()
    {
        var result = RiHtmlTemplateValidator.Validate("<html><body><p>{{Total}}</p></body></html>");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("{{Descripcion}}"));
    }

    [Fact]
    public void Validate_WithoutTotal_IsRejected()
    {
        var result = RiHtmlTemplateValidator.Validate("<html><body><table><tr><td>{{Descripcion}}</td></tr></table></body></html>");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("{{Total}}"));
    }

    [Fact]
    public void Validate_Empty_IsRejected()
    {
        Assert.False(RiHtmlTemplateValidator.Validate("   ").IsValid);
        Assert.False(RiHtmlTemplateValidator.Validate(null).IsValid);
    }

    [Fact]
    public void Validate_TooLarge_IsRejected()
    {
        var html = "<p>" + new string('a', RiHtmlTemplateValidator.MaxBytes + 1) + "</p>";

        var result = RiHtmlTemplateValidator.Validate(html);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("2 MB"));
    }

    [Fact]
    public void Validate_StripsScriptsHandlersAndJavascriptUrls()
    {
        var html = """
<html><head><script>alert(1)</script><base href="http://evil"><link rel="stylesheet" href="http://evil/x.css"></head>
<body onload="x()"><a href="javascript:alert(1)" onclick="y()">x</a><iframe src="http://evil"></iframe>
<object data="x"></object><embed src="x">
<style>@import url(http://evil/a.css); p{color:red}</style>
<table><tr><td>{{Descripcion}}</td></tr></table>{{Total}}</body></html>
""";

        var result = RiHtmlTemplateValidator.Validate(html);

        Assert.True(result.IsValid);
        var s = result.SanitizedHtml;
        Assert.DoesNotContain("<script", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onload", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<object", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<embed", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<base", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<link", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@import", s, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("color:red", s);
    }

    [Fact]
    public void Validate_WarnsUnknownMarkers_AndMissingBlocks()
    {
        var html = "<html><body><table><tr><td>{{Descripcion}}</td></tr></table>{{Total}}{{Inventado}}</body></html>";

        var result = RiHtmlTemplateValidator.Validate(html);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("{{Inventado}}"));
        Assert.Contains(result.Warnings, w => w.Contains("{{eNCF}}"));
        Assert.Contains(result.Warnings, w => w.Contains("{{QR}}"));
    }

    [Fact]
    public void Validate_Reference_HasNoWarnings()
    {
        var result = RiHtmlTemplateValidator.Validate(RiHtmlTestTemplates.Reference);

        Assert.Empty(result.Warnings);
    }
}

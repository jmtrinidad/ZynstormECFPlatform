using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Ri;
using Xunit;

namespace ZynstormECFPlatform.Tests.Ri;

public class RiFormatResolverTests
{
    private static RiHtmlTemplate Template(string name) => new() { Name = name, FileName = name + ".html", Html = "<p>" + name + "</p>" };

    [Fact]
    public void Resolve_ClientWithoutTypeNorTemplate_IsDefault()
    {
        var result = RiFormatResolver.Resolve(new Client());

        Assert.Equal(RiFormatSource.Default, result.Source);
        Assert.Null(result.Template);
    }

    [Fact]
    public void Resolve_TypeWithoutTemplate_IsDefault()
    {
        var client = new Client { ClientType = new ClientType { Name = "Otro" } };

        Assert.Equal(RiFormatSource.Default, RiFormatResolver.Resolve(client).Source);
    }

    [Fact]
    public void Resolve_TypeWithTemplate_UsesTypeTemplateAndSamples()
    {
        var client = new Client
        {
            ClientType = new ClientType
            {
                Name = "Royaltrix Software",
                SampleDataJson = "{\"Medico\":\"Dr. X\"}",
                RiHtmlTemplate = Template("royaltrix")
            }
        };

        var result = RiFormatResolver.Resolve(client);

        Assert.Equal(RiFormatSource.ClientType, result.Source);
        Assert.Equal("royaltrix", result.Template!.Name);
        Assert.Equal("Royaltrix Software", result.ClientTypeName);
        Assert.Equal("{\"Medico\":\"Dr. X\"}", result.SampleDataJson);
    }

    private static ClientTypeTemplate Link(RiHtmlTemplate t) => new() { RiHtmlTemplate = t, RiHtmlTemplateId = t.RiHtmlTemplateId };

    [Fact]
    public void FormatsOf_TypeWithoutFormats_IsEmpty()
    {
        Assert.Empty(RiFormatResolver.FormatsOf(new ClientType { Name = "Vacío" }));
    }

    [Fact]
    public void FormatsOf_OnlyDefault_ListsItAsDefault()
    {
        var a = Template("a");
        var type = new ClientType { Name = "T", RiHtmlTemplate = a, RiHtmlTemplateId = 1 };

        var formats = RiFormatResolver.FormatsOf(type);

        var single = Assert.Single(formats);
        Assert.Equal("a", single.Template.Name);
        Assert.True(single.IsDefault);
    }

    [Fact]
    public void FormatsOf_LinksPlusDefault_MarksOnlyTheDefault_AndNoDuplicates()
    {
        var a = Template("a"); a.RiHtmlTemplateId = 1;
        var b = Template("b"); b.RiHtmlTemplateId = 2;
        var c = Template("c"); c.RiHtmlTemplateId = 3;
        var type = new ClientType
        {
            Name = "T",
            RiHtmlTemplate = b,
            RiHtmlTemplateId = 2,
            TemplateLinks = [Link(a), Link(b), Link(c)]
        };

        var formats = RiFormatResolver.FormatsOf(type);

        Assert.Equal(["a", "b", "c"], formats.Select(f => f.Template.Name).ToArray());
        Assert.Equal(["b"], formats.Where(f => f.IsDefault).Select(f => f.Template.Name).ToArray());
    }

    [Fact]
    public void FormatsOf_DefaultNotInLinks_IsStillIncluded()
    {
        var a = Template("a"); a.RiHtmlTemplateId = 1;
        var d = Template("d"); d.RiHtmlTemplateId = 4;
        var type = new ClientType { Name = "T", RiHtmlTemplate = d, RiHtmlTemplateId = 4, TemplateLinks = [Link(a)] };

        var formats = RiFormatResolver.FormatsOf(type);

        Assert.Equal(2, formats.Count);
        Assert.Contains(formats, f => f.Template.Name == "d" && f.IsDefault);
        Assert.Contains(formats, f => f.Template.Name == "a" && !f.IsDefault);
    }

    [Fact]
    public void Resolve_ClientTemplate_WinsOverTypeTemplate_ButKeepsTypeSamples()
    {
        var client = new Client
        {
            RiHtmlTemplate = Template("propio"),
            ClientType = new ClientType
            {
                Name = "Royaltrix Software",
                SampleDataJson = "{\"Medico\":\"Dr. X\"}",
                RiHtmlTemplate = Template("royaltrix")
            }
        };

        var result = RiFormatResolver.Resolve(client);

        Assert.Equal(RiFormatSource.Client, result.Source);
        Assert.Equal("propio", result.Template!.Name);
        Assert.Equal("{\"Medico\":\"Dr. X\"}", result.SampleDataJson);
    }
}

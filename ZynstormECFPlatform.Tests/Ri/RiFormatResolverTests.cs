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

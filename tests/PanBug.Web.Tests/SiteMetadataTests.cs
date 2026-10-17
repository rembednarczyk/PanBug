using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace PanBug.Web.Tests;

/// <summary>Metadane strony: podgląd linków, manifest i pliki, na które wskazują.</summary>
public class SiteMetadataTests
{
    private static readonly string WwwRoot = Path.Combine(FindRepositoryRoot(), "src", "PanBug.Web", "wwwroot");
    private static readonly string Index = File.ReadAllText(Path.Combine(WwwRoot, "index.html"));

    private static string Meta(string attr, string name)
    {
        var m = Regex.Match(Index, $"<meta {attr}=\"{Regex.Escape(name)}\" content=\"([^\"]*)\"");
        Assert.True(m.Success, $"Brak <meta {attr}=\"{name}\">");
        return m.Groups[1].Value;
    }

    [Fact]
    public void Open_graph_uses_absolute_urls_consistent_with_canonical()
    {
        var canonical = Regex.Match(Index, "<link rel=\"canonical\" href=\"([^\"]+)\"").Groups[1].Value;
        Assert.StartsWith("https://", canonical);
        Assert.EndsWith("/", canonical);
        Assert.Equal(canonical, Meta("property", "og:url"));
        Assert.Equal(canonical + "og-image.png", Meta("property", "og:image"));
        Assert.Equal(canonical + "og-image.png", Meta("name", "twitter:image"));
        Assert.Equal("summary_large_image", Meta("name", "twitter:card"));
    }

    [Fact]
    public void Og_image_is_a_1200x630_png()
    {
        var bytes = File.ReadAllBytes(Path.Combine(WwwRoot, "og-image.png"));
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes[..4]);
        int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        Assert.Equal((1200, 630), (width, height));
        Assert.Equal("1200", Meta("property", "og:image:width"));
        Assert.Equal("630", Meta("property", "og:image:height"));
        Assert.True(bytes.Length < 600 * 1024, "Obrazek OG powinien mieć mniej niż 600 KB");
    }

    [Fact]
    public void Manifest_and_linked_icons_exist()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(WwwRoot, "manifest.webmanifest")));
        var icons = manifest.RootElement.GetProperty("icons").EnumerateArray().Select(i => i.GetProperty("src").GetString()!).ToList();
        Assert.Contains(manifest.RootElement.GetProperty("icons").EnumerateArray(), i => i.GetProperty("purpose").GetString() == "maskable");

        var linked = Regex.Matches(Index, "<link rel=\"(?:icon|apple-touch-icon|manifest)\" href=\"([^\"]+)\"").Select(m => m.Groups[1].Value);
        foreach (var file in icons.Concat(linked))
            Assert.True(File.Exists(Path.Combine(WwwRoot, file)), $"Brak pliku {file}");
    }

    [Fact]
    public void Structured_data_is_valid_json()
    {
        var json = Regex.Match(Index, "<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Singleline).Groups[1].Value;
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("WebApplication", doc.RootElement.GetProperty("@type").GetString());
    }

    [Fact]
    public void App_start_is_redirected_home_before_blazor_reads_the_address()
    {
        // Router czyta adres przy starcie Blazora, więc przekierowanie musi stać po <base href>
        // (z którego liczy adres startowy) i przed blazor.webassembly.js.
        int Pos(string fragment)
        {
            int i = Index.IndexOf(fragment, StringComparison.Ordinal);
            Assert.True(i >= 0, $"Brak {fragment} w index.html");
            return i;
        }

        int start = Pos("<script src=\"start-at-home.js\"></script>");
        Assert.True(Pos("<base href=") < start);
        Assert.True(start < Pos("_framework/blazor.webassembly.js"));

        var script = File.ReadAllText(Path.Combine(WwwRoot, "start-at-home.js"));
        Assert.Contains("document.baseURI", script);
        Assert.Contains("history.replaceState", script);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PanBug.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Nie znaleziono PanBug.sln.");
    }
}

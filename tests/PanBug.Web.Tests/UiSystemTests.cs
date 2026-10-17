using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PanBug.Web.Layout;
using PanBug.Web.Shared;
using Xunit;

namespace PanBug.Web.Tests;

/// <summary>Elementy systemu UI: ikony Lucide, logo i przełącznik motywu.</summary>
public class UiSystemTests : WebTestBase
{
    [Fact]
    public void Icon_renders_decorative_svg_with_lucide_shape()
    {
        var cut = RenderComponent<Icon>(p => p.Add(i => i.Name, "bug").Add(i => i.Size, 20));

        var svg = cut.Find("svg.icon");
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.Equal("20", svg.GetAttribute("width"));
        Assert.Equal("0 0 24 24", svg.GetAttribute("viewBox"));
        Assert.NotEmpty(svg.QuerySelectorAll("path"));
    }

    [Fact]
    public void Unknown_icon_name_fails_fast()
    {
        Assert.Throws<ArgumentException>(() => LucideIcons.Shape("nie-ma-takiej"));
    }

    [Fact]
    public void Every_icon_used_in_markup_exists()
    {
        var root = FindRepositoryRoot();
        var used = Directory.EnumerateFiles(Path.Combine(root, "src", "PanBug.Web"), "*.razor", SearchOption.AllDirectories)
            .SelectMany(file => Regex.Matches(File.ReadAllText(file), "(?:<Icon Name|Shape\\()=?\"([a-z-]+)\"")
                .Select(m => m.Groups[1].Value))
            .Distinct()
            .ToList();

        Assert.NotEmpty(used);
        foreach (var name in used)
            Assert.False(string.IsNullOrEmpty(LucideIcons.Shape(name)), name);
    }

    [Fact]
    public void Logo_instances_use_unique_gradient_ids()
    {
        var first = RenderComponent<Logo>().Find("radialGradient").Id;
        var second = RenderComponent<Logo>().Find("radialGradient").Id;
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Theme_toggle_reads_state_and_switches_icon()
    {
        JSInterop.Setup<bool>("panbug.theme.isDark").SetResult(false);
        JSInterop.Setup<bool>("panbug.theme.toggle").SetResult(true);

        var cut = RenderComponent<ThemeToggle>();
        var button = cut.Find("button");
        Assert.Equal("Włącz ciemny motyw", button.GetAttribute("aria-label"));

        button.Click();

        cut.WaitForAssertion(() =>
            Assert.Equal("Włącz jasny motyw", cut.Find("button").GetAttribute("aria-label")));
        Assert.Single(JSInterop.Invocations, i => i.Identifier == "panbug.theme.toggle");
    }

    [Fact]
    public void Layout_marks_current_page_in_navigation()
    {
        JSInterop.Setup<bool>("panbug.theme.isDark").SetResult(false);
        Services.GetRequiredService<NavigationManager>().NavigateTo("pomoc");

        var cut = RenderComponent<MainLayout>();

        var active = cut.FindAll(".topnav a.active");
        Assert.Single(active);
        Assert.Contains("Pomoc", active[0].TextContent);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PanBug.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Nie znaleziono PanBug.sln.");
    }
}



using GorodTV.Core.Models;
using GorodTV.Core.Models.DTOs.Response;

namespace GorodTV.Core.Services;

/// <summary>DTO → доменные модели UI.</summary>
public static class ChannelMapper
{
    private static readonly Color[] TilePalette =
    {
        Color.FromArgb("#1B66E5"), Color.FromArgb("#E5342B"), Color.FromArgb("#1450B4"),
        Color.FromArgb("#1FA764"), Color.FromArgb("#F08C2D"), Color.FromArgb("#8B3DE8"),
    };

    private static readonly (Color tint, Color icon)[] CategoryPalette =
    {
        (Color.FromArgb("#E4EDFC"), Color.FromArgb("#1B66E5")),
        (Color.FromArgb("#FCE7E6"), Color.FromArgb("#E5342B")),
    };

    public static ChannelItem ToChannel(ChannelDto dto) => new()
    {
        Id = int.TryParse(dto.Id, out var n) ? n : 0,
        Name = dto.Name,
        CategoryId = dto.Category,
        EpgId = dto.EpgId ?? "",
        IconUrl = string.IsNullOrWhiteSpace(dto.Icon) ? null : dto.Icon,
        Link = dto.Link ?? "",
        Abbrev = MakeAbbrev(dto.Name),
        TileColor = PickColor(dto.Name),
    };

    public static List<CategoryItem> BuildCategories(
        IReadOnlyList<CategoryDto> categories, IReadOnlyList<ChannelDto> channels)
    {
        var counts = channels.GroupBy(c => c.Category)
                             .ToDictionary(g => g.Key, g => g.Count());
        var result = new List<CategoryItem>(categories.Count);
        for (int i = 0; i < categories.Count; i++)
        {
            var c = categories[i];
            var (tint, icon) = CategoryPalette[i % CategoryPalette.Length];
            counts.TryGetValue(c.Id, out var cnt);
            result.Add(new CategoryItem
            {
                Id = c.Id,
                Title = c.Name,
                IconUrl = string.IsNullOrWhiteSpace(c.Icon) ? null : c.Icon,
                Tint = tint,
                IconColor = icon,
                Count = cnt,
                FallbackGlyph = GlyphForCategory(c.Name),
            });
        }
        return result;
    }

    public static List<ChannelItem> BuildChannels(
        IReadOnlyList<ChannelDto> channels, string? categoryId = null)
    {
        IEnumerable<ChannelDto> src = channels;
        if (!string.IsNullOrEmpty(categoryId) && categoryId != "all")
            src = channels.Where(c => c.Category == categoryId);
        return src.Select(ToChannel).ToList();
    }

    private static string MakeAbbrev(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "ТВ";
        var w = name.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (w.Length >= 2) return $"{char.ToUpper(w[0][0])}{char.ToUpper(w[1][0])}";
        return (w[0].Length >= 2 ? w[0][..2] : w[0]).ToUpperInvariant();
    }

    private static Color PickColor(string key)
    {
        int hash = 0;
        foreach (var ch in key) hash = (hash * 31 + ch) & 0x7FFFFFFF;
        return TilePalette[hash % TilePalette.Length];
    }

    private static string GlyphForCategory(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("эфир")) return "\ue63a";
        if (n.Contains("музык")) return "\ue405";
        if (n.Contains("кино") || n.Contains("сериал")) return "\ue684";
        if (n.Contains("новост")) return "\ueb81";
        if (n.Contains("познани") || n.Contains("мир")) return "\ue80b";
        if (n.Contains("спорт")) return "\uea2f";
        if (n.Contains("дет")) return "\ueb41";
        if (n.Contains("развлек")) return "\uea65";
        if (n.Contains("hd")) return "\ue052";
        if (n.Contains("взросл")) return "\ue01e";
        if (n.Contains("камер") || n.Contains("cam")) return "\ue04b";
        if (n.Contains("интернет") || n.Contains("тест")) return "\uea07";
        return "\ue72c";
    }
}

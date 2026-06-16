
namespace GorodTV.Core.Services;

/// <summary>
/// Подставляет плейсхолдеры в URL потока канала.
/// Шаблон: https://gorod.tv/s/live/1/%IPMAC%/.../%TIMESTAMP%.m3u8
///   %IPMAC%     -> "ssiptv"
///   %TIMESTAMP% -> "0" для прямого эфира, либо unixtime передачи для архива
/// </summary>
public static class StreamUrlBuilder
{
    private const string IpMacValue = "ssiptv";

    public static string Build(string rawLink, long timestamp = 0)
    {
        if (string.IsNullOrWhiteSpace(rawLink)) return rawLink;

        return rawLink
            .Replace("%IPMAC%", IpMacValue)
            .Replace("%TIMESTAMP%", timestamp.ToString());
    }
}


using GorodTV.Core.Models.DTOs.Request;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text;

namespace GorodTV.Core.Services;

/// <summary>
/// Формирует относительные URL запросов и подписи sign (HMAC-RIPEMD160).
/// Алгоритм взят 1:1 из рабочего приложения, не менять.
/// </summary>
public static class BaseApiRequests
{
    private const string PrivateApiKey = "AcJnvy4t359IPdbz";

    public static string GetAuthRequestString(LoginRequest request) =>
        $"/api?operation=auth&login={request.ContractNumber}&password={request.Password}&sign={Sign(request.ContractNumber, request.Password)}";

    public static string GetCategoryRequestString(string sessionId) =>
        $"/api?operation=categories&sessionId={sessionId}&sign={CategoriesHmac(sessionId)}";

    public static string GetChannelsRequestString(string sessionId) =>
        $"/api?operation=channels&sessionId={sessionId}&sign={ChannelsHmac(sessionId)}";

    public static string GetIsSessionValidString(string sessionId) =>
        $"/api?operation=categories&sessionId={sessionId}&sign={CategoriesHmac(sessionId)}";

    public static string GetEpgRequestString(string startTime, string channelId, string sessionId) =>
        $"/api?operation=epg&sessionId={sessionId}&channel={channelId}&startTime={startTime}&allday=true&sign={EpgHmac(channelId, startTime, sessionId)}";
    
    public static string GetCategoriesAndChannelsRequestString(
            string username, string password, string sessionId) =>
        $"/api?operation=categoriesAndChannels&interval=day&sessionId={sessionId}" +
        $"&sign={Sign(username, password)}";

    public static string GetUnixTimeRequestString => "/api?operation=unixtime";

    private static string Sign(string contractNumber, string password) => HashedSign($"auth{contractNumber}{password}");
    private static string CategoriesHmac(string sessionId) => HashedSign("categories" + sessionId);
    private static string ChannelsHmac(string sessionId) => HashedSign("channels" + sessionId);
    private static string EpgHmac(string channelId, string startTime, string sessionId) =>
        HashedSign("epg" + sessionId + channelId + startTime);

    private static string HashedSign(string data)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(PrivateApiKey);
        byte[] messageBytes = Encoding.UTF8.GetBytes(data);

        var hmac = new HMac(new RipeMD160Digest());
        hmac.Init(new KeyParameter(keyBytes));
        hmac.BlockUpdate(messageBytes, 0, messageBytes.Length);

        byte[] result = new byte[hmac.GetMacSize()];
        hmac.DoFinal(result, 0);

        return BitConverter.ToString(result).Replace("-", "").ToLower();
    }
}
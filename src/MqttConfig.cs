public class MqttConfig
{

    public Uri Url { get; set; } = new Uri("mqtt://127.0.0.1:1883");

    public string HttpToken { get; set; }

    public string User => string.Join(":", Url.UserInfo.Split(':').SkipLast(1));

    public string Password => string.Join(":", Url.UserInfo.Split(':').Skip(1));

    public bool HasCredentials => !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password) && Url.UserInfo.Contains(":");

    public bool UseTls => Url.Scheme.Equals("mqtts", StringComparison.OrdinalIgnoreCase);
}
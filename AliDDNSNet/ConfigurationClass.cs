namespace AliDDNSNet
{
    public class ConfigurationClass
    {
        public string? IPv4Primary { get; set; }
        public string? IPv4Second { get; set; }
        public string? IPv6Primary { get; set; }
        public string? IPv6Second { get; set; }
#pragma warning disable IDE1006 // 命名样式
        public string? access_id { get; set; }

        public string? access_key { get; set; }

        public int interval { get; set; }

        public string? domain { get; set; }

        public string? sub_domain { get; set; }


        public string? type { get; set; }
#pragma warning restore IDE1006 // 命名样式
    }
}

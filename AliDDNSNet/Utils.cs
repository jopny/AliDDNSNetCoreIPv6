using AliDDNSNet.Request;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace AliDDNSNet
{
    public static class Utils
    {
        public static ConfigurationClass? Config { get; set; }

        /// <summary>
        /// 生成请求签名
        /// </summary>
        /// <param name="srcStr">请求体</param>
        /// <returns>HMAC-SHA1 的 Base64 编码</returns>
        public static string GenerateSignature(this string srcStr)
        {
            var signStr = $"GET&{HttpUtility.UrlEncode("/")}&{HttpUtility.UrlEncode(srcStr)}";

            // 替换已编码的 URL 字符为大写字符
            signStr = signStr.Replace("%2f", "%2F").Replace("%3d", "%3D").Replace("%2b", "%2B")
                .Replace("%253a", "%253A");

            var hmac = new HMACSHA1(Encoding.UTF8.GetBytes($"{Config?.access_key}&"));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signStr)));
        }

        /// <summary>
        /// 根据字典构建请求字符串
        /// </summary>
        /// <param name="parameters">参数字典</param>
        /// <returns></returns>
        public static string BuildRequestString(this SortedDictionary<string, string> parameters)
        {
            var sb = new StringBuilder();
            foreach (var kvp in parameters)
            {
                sb.Append('&');
                sb.Append(HttpUtility.UrlEncode(kvp.Key));
                sb.Append('=');
                sb.Append(HttpUtility.UrlEncode(kvp.Value));
            }

            return sb.ToString()[1..];
        }

        /// <summary>
        /// 追加签名参数
        /// </summary>
        /// <param name="parameters">参数列表</param>
        public static string AppendSignature(this SortedDictionary<string, string> parameters, string sign)
        {
            parameters.Add("Signature", sign);
            return parameters.BuildRequestString();
        }

        /// <summary>
        /// 生成通用参数字典
        /// </summary>
        public static SortedDictionary<string, string> GenerateGenericParameters()
        {
            var dict = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                {"Format", "json"},
                {"AccessKeyId", Config?.access_id??""},
                {"SignatureMethod", "HMAC-SHA1"},
                {"SignatureNonce", Guid.NewGuid().ToString()},
                {"Version", "2015-01-09"},
                {"SignatureVersion", "1.0"},
                {"Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")}
            };

            return dict;
        }

        /// <summary>
        /// 对阿里云 API 发送 GET 请求
        /// </summary>
        public static async Task<string> SendGetRequest(IRequest request)
        {
            var sign = request.Parameters.BuildRequestString().GenerateSignature();
            var postUri = $"http://alidns.aliyuncs.com/?{request.Parameters.AppendSignature(sign)}";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var resuest = new HttpRequestMessage(HttpMethod.Get, postUri);
            using var response = await client.SendAsync(resuest);
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// 获得当前机器的公网 IPv4[废弃]
        /// </summary>
        public static async Task<string> GetCurentPublicIPv4()
        {
            using var client = new HttpClient();
            using var resuest = new HttpRequestMessage(HttpMethod.Get, "http://members.3322.org/dyndns/getip");
            using var response = await client.SendAsync(resuest);
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// 获得当前机器的公网(IPv6/IPv4)
        /// http://v4.ipv6-test.com/json/widgetdata.php?callback=?
        /// http://v6.ipv6-test.com/json/widgetdata.php?callback=?
        /// 错误处理：
        //1. DNS解析不到：请求的名称有效，但是找不到请求的类型的数据。 (ipv6.vm3.test-ipv6.com:80)
        //2. 打不开网页，超时
        //3. 返回的页面不是预期内容，出现json解析错误
        /// </summary>
        public static async Task<string?> GetCurentPublicIP(string? sUrl)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var resuest = new HttpRequestMessage(HttpMethod.Get, sUrl); //待处理本地没有IPv6地址时的提示信息
            using var response = await client.SendAsync(resuest);
            string JsonStr = await response.Content.ReadAsStringAsync();
            //var myInfo = JsonConvert.DeserializeObject<dynamic>(JsonStr.Replace("?(", "").Replace(")", ""));
            //return myInfo.address;
            var myIP = JObject.Parse(JsonStr.Replace("callback(", "").Replace(")", ""));

            return (myIP["ip"]?.Value<string>()??"").Split(',')[0];
        }

        /// <summary>
        /// 读取配置文件
        /// </summary>
        /// <param name="filePath">配置文件路径</param>
        public static async Task<ConfigurationClass?> ReadConfigFileAsync(string filePath)
        {
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var read = new StreamReader(fs);
            return JsonConvert.DeserializeObject<ConfigurationClass?>(await read.ReadToEndAsync());
        }

        public static string GetLocalIPAddress()
        {
            string CurIP = "";
            // 获取所有网络适配器
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

            // 遍历每个网络适配器
            foreach (NetworkInterface adapter in adapters.Where(p =>p.OperationalStatus is OperationalStatus.Up && p.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                Console.WriteLine($"{adapter.Description},MAC:{adapter.GetPhysicalAddress()},{adapter.NetworkInterfaceType},{adapter.OperationalStatus}");
                // 获取 IP 地址列表
                foreach (UnicastIPAddressInformation address in adapter.GetIPProperties().UnicastAddresses.Where(p => !IPAddress.IsLoopback(p.Address)))
                {
                    Console.WriteLine($"   {address.Address},{address.Address.AddressFamily}," +
                        $"{address.Address.IsIPv6LinkLocal}");
                    if(address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        string[] parts = address.Address.ToString().Split('%');
                        CurIP = parts[0];
                        break;
                    }
                }
            }
            return CurIP;
            //NetworkInterface.GetAllNetworkInterfaces()
            //    .SelectMany(i => i.GetIPProperties().UnicastAddresses)
            //    //.Select(a => a.Address)
            //    .Where(p => p.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && !System.Net.IPAddress.IsLoopback(p.Address))
            //.ToList()
            //.ForEach(adapter => Console.WriteLine($"IP:{adapter.Address}"));

            //var MyIP = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
            //  .Select(p => p.GetIPProperties())
            //  .SelectMany(p => p.UnicastAddresses)
            //  .Where(p => p.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && !System.Net.IPAddress.IsLoopback(p.Address))

            //  .FirstOrDefault()?.Address.ToString();
            //Console.WriteLine("MyIP:"+MyIP);
        }

        public static bool IsIPv6(string? InStr)
        {
            string ipv6Regex = @"^((([0-9a-fA-F]{0,4})(:[0-9a-fA-F]{0,4})*)?)::((([0-9a-fA-F]{0,4})(:[0-9a-fA-F]{0,4})*)?)$";

            // 验证是否匹配
            return Regex.IsMatch(InStr??"", ipv6Regex);
        }
        public static bool IsIPv4(string? InStr)
        {
            string ipv4Regex = @"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$"; 

            // 验证是否匹配
            return Regex.IsMatch(InStr ?? "", ipv4Regex);
        }

    }
}

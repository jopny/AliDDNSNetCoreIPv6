using AliDDNSNet.Request;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AliDDNSNet
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "AliDDNSNet"
            };
            app.HelpOption("-?|-h|--help");

            var attachments = app.Option("-f|--file <FILE>", "配置文件路径.", CommandOptionType.SingleValue);

            app.OnExecute(async () =>
            {
                #region > 配置初始化 <
                // 加载配置文件：
                var filePath = attachments.HasValue()
                    ? attachments.Value()
                    : $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}settings.json";

                if (!File.Exists(filePath))
                {
                    Console.WriteLine("当前目录没有配置文件，或者配置文件位置不正确。");
                    return -1;
                }

                var config = await Utils.ReadConfigFile(filePath);
                Utils.config = config;
                #endregion

                //通过API获取子域名的解析记录列表
                var subDomains = JObject.Parse(await Utils.SendGetRequest(new DescribeDomainRecordsRequest(config.domain,config.sub_domain)));
                
                //如果同时配置有IPv6+Ipv4记录时的处理
                //字符串前$为字符串插值
                //字符串内为：SelectToken with JSONPath，帮助： https://www.newtonsoft.com/json/help/html/SelectToken.htm
                if (subDomains.SelectTokens($"$.DomainRecords.Record[?(@.RR == '{config.sub_domain}')]") == null)
                {
                    Console.WriteLine("指定的子域名不存在，请新建一个子域名解析。");
                    return 0;
                }

                Console.WriteLine("已经找到对应的域名与解析");
                Console.WriteLine("======================");
                Console.WriteLine($"子域名:{config.sub_domain}.{config.domain}");

                //列出子域名的所有类型记录
                IEnumerable<JToken> DomainRecs = subDomains.SelectTokens($"$.DomainRecords.Record[?(@.RR == '{config.sub_domain}')]");

                foreach (JToken item in DomainRecs)
                {
                    Console.WriteLine( "  RR:" + item["Line"] + " line " + item["Type"] + " " + item["Value"]);

                    var dnsIp = item["Value"].Value<string>();

                    // 更新IPv6
                    if (item["Type"].ToString() == "AAAA" && (config.type == "AAAA" || config.type == "*"))   
                    {                        
                        try
                        {
                            //http://ipv6.vm0.test-ipv6.com/ip/?callback=?                            
                            var currentIP = (await Utils.GetCurentPublicIP(Utils.config.IPv6Primary));

                            if (currentIP == dnsIp)
                            {
                                Console.WriteLine("    解析地址与当前主机 IP 地址一致，无需更改.");
                                //return 0;
                            }
                            else
                            {
                                Console.WriteLine("    检测到 IP 地址不一致，正在更改中......");
                                var rrId = item["RecordId"].Value<string>();

                                var response = await Utils.SendGetRequest(new UpdateDomainRecordRequest(rrId, config.sub_domain, item["Type"].Value<string>(), currentIP, config.interval.ToString()));

                                var resultRRId = JObject.Parse(response).SelectToken("$.RecordId").Value<string>();

                                if (resultRRId == null || resultRRId != rrId)
                                {
                                    Console.WriteLine($"    更改{config.type}记录失败，请稍后再试。");
                                }
                                else
                                {
                                    Console.WriteLine($"    更改{config.type}记录成功。新IP:{currentIP}");
                                }
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine("IPv6-AAAA地址出错了：" + e.Message);
                        }

                    }
                    else if(item["Type"].ToString() == "A" && (config.type == "A" || config.type == "*"))
                    {
                        try
                        {
                            //获取公网IP服务可能故障，转移到配置中，以便调整
                            //http://ipv4.vm0.test-ipv6.com/ip/?callback=?
                            var currentIP = (await Utils.GetCurentPublicIP(Utils.config.IPv4Primary));

                            if (currentIP == dnsIp)
                            {
                                Console.WriteLine("    解析地址与当前主机 IP 地址一致，无需更改.");
                                //return 0;
                            }
                            else
                            {
                                Console.WriteLine("    检测到 IP 地址不一致，正在更改中......");
                                //var rrId = subDomains.SelectToken($"$.DomainRecords.Record[?(@.RR == '{config.sub_domain}')].RecordId").Value<string>();
                                var rrId = item["RecordId"].Value<string>();
                        
                                var response = await Utils.SendGetRequest(new UpdateDomainRecordRequest(rrId, config.sub_domain, item["Type"].Value<string>(), currentIP, config.interval.ToString()));

                                var resultRRId = JObject.Parse(response).SelectToken("$.RecordId").Value<string>();

                                if (resultRRId == null || resultRRId != rrId)
                                {
                                    Console.WriteLine($"    更改{config.type}记录失败，请稍后再试。");
                                }
                                else
                                {
                                    Console.WriteLine($"    更改{config.type}记录成功。新IP:{currentIP}");
                                }
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine("IPv4-A地址出错了：" + e.Message);
                        }
                    }
                }
                return 0;
            });

            return await Task.FromResult(app.Execute(args));
        }
    }
}

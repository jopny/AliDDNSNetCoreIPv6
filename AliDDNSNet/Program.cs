using AliDDNSNet.Request;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            app.OnExecuteAsync(async cancellationToken =>
            {
                #region > 配置初始化 <
                // 加载配置文件：
                var filePath = attachments.HasValue()
                    ? attachments.Value()
                    : $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}settings.json";

                if (!File.Exists(filePath))
                {
                    Console.WriteLine("当前目录没有配置文件，或者配置文件位置不正确。");
                    return ;
                }

                Console.WriteLine("配置文件：" + filePath);

                var config = await Utils.ReadConfigFileAsync(filePath);
                Console.WriteLine("待更新域名。" + config.domain);
                Utils.config = config;
                #endregion

                //通过API获取子域名的解析记录列表
                var subDomains = JObject.Parse(await Utils.SendGetRequest(new DescribeDomainRecordsRequest(config.domain,config.sub_domain)));
                Console.WriteLine("" + subDomains);
                /*失败提示1：Access_ID错误
                 * {
                        "RequestId": "0B12DE1A-2304-5E86-AAFD-B0206BDECD01",
                        "Message": "Specified access key is not found.",
                        "Recommend": "https://api.aliyun.com/troubleshoot?q=InvalidAccessKeyId.NotFound&product=Alidns&requestId=0B12DE1A-2304-5E86-AAFD-B0206BDECD01",
                        "HostId": "alidns.aliyuncs.com",
                        "Code": "InvalidAccessKeyId.NotFound"
                    }
                   失败提示2，Access_ID正确，Accesskey错误
                    {
                        "RequestId": "7A4A78A8-FEA6-57F0-9ABC-F34866CA740C",
                        "Message": "Specified signature is not matched with our calculation. server string to sign is:GET&%2F&AccessKeyId%3DLTAIXlclcHSpb2tY%26Action%3DDescribeDomainRecords%26DomainName%3Dxx.cn%26Format%3Djson%26RRKeyWord%3Dipv6%26SignatureMethod%3DHMAC-SHA1%26SignatureNonce%3Dbaba602a-6121-4bd7-abcf-e45ebca46d5d%26SignatureVersion%3D1.0%26Timestamp%3D2023-10-25T07%253A35%253A41Z%26Version%3D2015-01-09",
                        "Recommend": "https://api.aliyun.com/troubleshoot?q=SignatureDoesNotMatch&product=Alidns&requestId=7A4A78A8-FEA6-57F0-9ABC-F34866CA740C",
                        "HostId": "alidns.aliyuncs.com",
                        "Code": "SignatureDoesNotMatch"
                    }
                    失败提示3：指定域名不存在
                    {
                      "RequestId": "8584D687-2E67-53C1-8DD7-95E288A02343",
                      "HostId": "alidns.aliyuncs.com",
                      "Code": "InvalidDomainName.NoExist",
                      "Message": "The specified domain name does not exist. Refresh the page and try again.",
                      "Recommend": "https://api.aliyun.com/troubleshoot?q=InvalidDomainName.NoExist&product=Alidns&requestId=8584D687-2E67-53C1-8DD7-95E288A02343"
                    }
                 子域名不存在
                    {
                      "TotalCount": 0,
                      "PageSize": 20,
                      "RequestId": "09347E6A-18C5-58D3-B29E-B17F3F9AF6D3",
                      "DomainRecords": {
                        "Record": []
                      },
                      "PageNumber": 1
}
                 */
                if (!string.IsNullOrEmpty((string)subDomains["Message"]))   //出错则终止
                {
                    Console.WriteLine("" + subDomains["Message"]);
                    return;
                }
                //Console.WriteLine("" + subDomains["Message"]);
                //如果同时配置有IPv6+Ipv4记录时的处理
                //字符串前$为字符串插值
                //字符串内为：SelectToken with JSONPath，帮助： https://www.newtonsoft.com/json/help/html/SelectToken.htm
                if ((int)subDomains["TotalCount"] ==0 || !subDomains.SelectTokens($"$.DomainRecords.Record[?(@.RR == '{config.sub_domain}')]").Any())
                {
                    Console.WriteLine($"子域名{config.sub_domain}.{config.domain}不存在，请确认子域名是否正确。");
                    return;
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
                return ;
            });

            return await Task.FromResult(app.Execute(args));
        }
    }
}

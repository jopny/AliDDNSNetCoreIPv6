## 0.简要介绍

AliDDNSNetCoreIPv6 是基于 .NET Core 开发的动态 DNS 解析工具，借助于阿里云的 DNS API 来实现域名与动态 IP 的绑定功能。这样你随时就可以通过域名来访问你的设备，而不需要担心 IP 变动的问题。阿里云DNS支持IPv6的AAAA记录，本项目可同时修改A记录和AAAA记录。

类似工具对比
| Tools              | IPv4                | IPv6                        | Memo                                                                          |
|--------------------|---------------------|-----------------------------|-------------------------------------------------------------------------------|
| OpenWRT/PANDAVA    | Router IPv4 Address | Route IPv6 Address          | For Router, Is ok                                                             |
| USB Module ESP8266 | Router IPv4 Address | ESP8266 IPv6 Address        |  If you have some servers in the same local network, you want each server's   |
| AliDDNSNetIPv6     | Router IPv4 Address | Current Device IPv6 Address |ipv6 is update to your domain name, you need AliDDNSNetIPv6.                   |

## 1.使用说明

> 使用本工具的时候，请详细阅读使用说明。

### 1.1 配置说明

通过更改 ```settings.json.example``` 的内容来实现 DDNS 更新，其文件内部各个选项的说明如下：

```json
{
  //获取外网IP服务,考虑到外部服务可能因终端而需要更换
  "IPv4Primary": "http://ipv4.vm3.test-ipv6.com/ip/?callback=?",
  "IPv4Second": "http://ipv4.vm2.test-ipv6.com/ip/?callback=?",
  "IPv6Primary": "http://ipv6.vm3.test-ipv6.com/ip/?callback=?",
  "IPv6Second": "http://ipv6.vm2.test-ipv6.com/ip/?callback=?",
  // 阿里云的 Access Id
  "access_id": "",
  // 阿里云的 Access Key
  "access_key": "",
  // TTL 时间
  "interval": 600,
  // 主域名
  "domain": "example.com",
  // 子域名前缀
  "sub_domain": "test",
  // 记录类型，已支持IPv6双栈，同时更新A记录和AAAA记录
  "type": "AAAA"  //可选值:"A",更新A记录，"AAAA",更新AAAA记录，"*",同时更新A记录和AAAA记录。
}
```

其中 ```Access Id``` 与 ```Access Key``` 可以登录阿里云之后在右上角可以得到。

### 1.2 使用说明

在运行程序的时候，请先更改同目录下的 ```settings.json``` 文件，修改其配置为你自己的相关配置,然后执行以下命令：

```shell
./AliDDNSNet
```

当然如果你有其他的配置文件也可以通过指定 ```-f``` 参数来制定配置文件路径。例如：

```shell
./AliDDNSNet -f ./settings1.json
```

### 1.4 Change Log
1. Updated to .Net 8.0
2. Improve error catching process
3. 
### 1.3 Change Log
1. Updated to .Net 7.0
2. Replace outdated Microsoft.Extensions.CommandLineUtils to McMaster.Extensions.CommandLineUtils;
3. Update error handling mechanism.

## 2.下载地址

[Windows](https://github.com/jopny/AliDDNSNetCoreIPv6/releases/download/v0.2.0/AliDDNSNetCoreIPv6-0.2.0.zip)，Linux

如果你的设备支持 Docker 环境，建议通过 Docker 运行 .NET Core 7.0 环境来执行本程序。

感谢:
https://github.com/real-zony/AliDDNSNet

## 0.简要介绍

AliDDNSNetCoreIPv6 是基于 .NET Core 开发的动态 DNS 解析工具，借助于阿里云的 DNS API 来实现域名与动态 IP 的绑定功能。这样你随时就可以通过域名来访问你的设备，而不需要担心 IP 变动的问题。阿里云DNS支持IPv6的AAAA记录，本项目可同时修改A记录和AAAA记录。

## 1.使用说明

> 使用本工具的时候，请详细阅读使用说明。

### 1.1 配置说明

通过更改 ```settings.json.example``` 的内容来实现 DDNS 更新，其文件内部各个选项的说明如下：

```json
{
  //获取外网IP服务,考虑到外部服务可能因终端而需要更换
  "IPv4Primary": "http://ipv4.vm1.test-ipv6.com/ip/?callback=?",
  "IPv4Second": "http://ipv4.vm2.test-ipv6.com/ip/?callback=?",
  "IPv6Primary": "http://ipv6.vm1.test-ipv6.com/ip/?callback=?",
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
  // 记录类型，已支持IPv6双栈，同时更新A记录和AAAA记录，暂未删除
  "type": "AAAA"
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
./AliDDNSNet -f ./settings.json3
```

## 2.下载地址

Windows，Linux

如果你的设备支持 Docker 环境，建议通过 Docker 运行 .NET Core 3.1 环境来执行本程序。

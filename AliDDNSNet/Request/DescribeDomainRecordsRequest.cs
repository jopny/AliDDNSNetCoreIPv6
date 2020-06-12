using System.Collections.Generic;

namespace AliDDNSNet.Request
{
    public class DescribeDomainRecordsRequest : IRequest
    {
        public SortedDictionary<string, string> Parameters { get; }

        public DescribeDomainRecordsRequest(string domainName,string RRKeyWord)
        {
            Parameters = Utils.GenerateGenericParameters();
            Parameters.Add("Action", "DescribeDomainRecords");
            Parameters.Add("DomainName", domainName);
            Parameters.Add("RRKeyWord", RRKeyWord);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;

namespace CachelessDnsClient
{
    public class Client
    {
        /// <summary>
        /// Does a simple lookup against the SOA to see if record exists.
        /// Good for initial check for name exisitance
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Task<bool> HostEntryExists(string name)
        {
            return GetHostEntry(name, false);
        }

        public Task<bool> HostEntryReplicated(string name)
        {
            return GetHostEntry(name, true);
        }

        async Task<bool> GetHostEntry(string name, bool replicas)
        {
            DomainName hostName = null;
            if (!DomainName.TryParse(name, out hostName))
            {
                throw new ArgumentException(string.Format("'{0}' is not a valid domain name", name));
            }

            // Get initial Master server to do direct queries
            DomainName hostDomain = hostName.GetParentName();
            IDnsResolver resolver = new DnsStubResolver();
            var soaRecords = await resolver.ResolveAsync<SoaRecord>(hostDomain, RecordType.Soa);
            if (soaRecords != null && soaRecords.Count > 0)
            {
                // Some local servers provide truncated SOA list, so query authority
                var hostDomainIps = await resolver.ResolveHostAsync(soaRecords[0].MasterName);
                var ip4s = hostDomainIps.Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                DnsClient client = new DnsClient(ip4s, 60);
                var soaMessage = client.Resolve(hostDomain, RecordType.Soa);

                if (soaMessage != null && soaMessage.ReturnCode == ReturnCode.NoError && soaMessage.AuthorityRecords.Count > 0)
                {

                    foreach (NsRecord server in soaMessage.AuthorityRecords)
                    {
                        // Create client talking directly to Master server
                        // NOTE: Async client appears not to work
                        hostDomainIps = await resolver.ResolveHostAsync(server.NameServer);
                        ip4s = hostDomainIps.Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                        client = new DnsClient(ip4s, 60);
                        var dnsMessage = client.Resolve(hostName, RecordType.A, RecordClass.INet, new DnsQueryOptions() { IsRecursionDesired = true });
                        if (dnsMessage == null || dnsMessage.ReturnCode == ReturnCode.NxDomain || dnsMessage.AnswerRecords.Count == 0)
                        {
                            // Record not found
                            return false;
                        }

                        // If we aren't checking all replica's, return true from first result
                        if (!replicas)
                        {
                            return true;
                        }
                    }

                    // All SOA servers returned a record
                    return true;
                }
            }

            // No SOA record found
            return false;
        }
    }
}

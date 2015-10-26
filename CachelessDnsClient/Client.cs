using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Heijden.DNS;

namespace CachelessDnsClient
{
    public class Client
    {
        Resolver localResolver;

        public Client()
        {
            this.localResolver = new Resolver(Resolver.GetDnsServers());
        }

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

        static IPAddress GetLocalDnsServerAddress()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    var networkProperties = networkInterface.GetIPProperties();
                    if (networkProperties.DnsAddresses.Count > 0)
                    {
                        return networkProperties.DnsAddresses.First();
                    }
                }
            }
            throw new ArgumentException("No DNS servers found");
        }

        async Task<bool> GetHostEntry(string name, bool replicas)
        {
            HostName hostName = new HostName(name);

            // Get initial Master server to do direct queries
            HostName hostDomain = hostName.GetParent();
            if (hostDomain == null)
            {
                throw new ArgumentException(string.Format("'{0}' is not a valid host name", name));
            }
            var soaRecords = localResolver.Query(hostDomain.Name, QType.SOA);
            if (soaRecords != null && soaRecords.RecordsSOA.Length > 0)
            {
                // Some local servers provide truncated SOA list, so query authority
                var hostDomainIps = await localResolver.ResolveAsync(soaRecords.RecordsSOA[0].MNAME);
                var remoteResolver = new Resolver(GetIpV4Address(hostDomainIps))
                {
                    Recursion = true,
                    TimeOut = 20,
                    TransportType =  Heijden.DNS.TransportType.Tcp
                };

                var soaMessage = remoteResolver.Query(hostDomain.Name, QType.SOA);

                if (soaMessage != null && soaRecords.RecordsSOA.Length > 0)
                {
                    // Search each SOA record
                    bool foundInReplicas = true;
                    foreach (var server in soaMessage.RecordsSOA)
                    {
                        // Search each IP associated with NS Server
                        hostDomainIps = localResolver.GetHostByName(server.MNAME);
                        foreach (var ipEndpoint in GetIpV4Address(hostDomainIps))
                        {
                            remoteResolver = new Resolver(ipEndpoint);
                            remoteResolver.Recursion = true;
                            remoteResolver.UseCache = false;
                            var dnsMessage = remoteResolver.Query(hostName.Name, QType.A);
                            if (dnsMessage == null || !string.IsNullOrEmpty(dnsMessage.Error) || dnsMessage.Answers.Count == 0)
                            {
                                // Record not found
                                Console.WriteLine("Record not found in NS server '{0}'", server.MNAME);
                                foundInReplicas = false;
                            }

                            // If we aren't checking all replica's, return true from first result
                            if (!replicas)
                            {
                                return foundInReplicas;
                            }
                        }
                    }

                    // All SOA servers returned a record
                    return foundInReplicas;
                }
            }

            // No SOA record found
            return false;
        }

        IPEndPoint[] GetIpV4Address(IPHostEntry hostEntry)
        {
            var ip4Addresses = hostEntry.AddressList.Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            List<IPEndPoint> endpoints = new List<IPEndPoint>();
            foreach (var address in ip4Addresses)
            {
                endpoints.Add(new IPEndPoint(address, Resolver.DefaultPort));
            }
            return endpoints.ToArray();
        }
    }
}

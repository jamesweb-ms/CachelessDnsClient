using System;
using System.Net;
using System.Threading.Tasks;
using Heijden.DNS;

namespace CachelessDnsClient
{
    public static class DnsExtension
    {
        public static Task<IPHostEntry> ResolveAsync(this Resolver resolver, string name)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }
            return Task<IPHostEntry>.Factory.FromAsync(resolver.BeginResolve, resolver.EndResolve, name, null);
        }
    }
}

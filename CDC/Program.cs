using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CachelessDnsClient;

namespace CDC
{
    class Program
    {
        static Client client = new Client();
        static void Main(string[] args)
        {
            if (args.Length < 0 || args.Length > 2)
            {
                Help();
                return;
            }

            if (args.Length == 1)
            {
                QueryHost(args[0]);
                return;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "/exists":
                    QueryHost(args[1]);
                    break;
                case "/replica":
                    QueryHostReplica(args[1]);
                    break;
                default:
                    Help();
                    break;
            }
        }

        static void QueryHostReplica(string name)
        {
            string result = "not replicated";
            if (client.HostEntryReplicated(name).Result)
            {
                result = "replicated";
            }
            Console.WriteLine("Hostname '{0}' {1}", name, result);
        }

        static void QueryHost(string name)
        {
            string result = "not found";
            if (client.HostEntryExists(name).Result)
            {
                result = "found";
            }
            Console.WriteLine("Hostname '{0}' {1}", name, result);
        }

        static void Help()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  cdc host          # Looks up host name");
            Console.WriteLine("  cdc /exists host  # Looks up host name");
            Console.WriteLine("  cdc /replica host # Looks up host name on all SOA Name servers");
        }
    }
}

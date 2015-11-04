using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CachelessDnsClient
{
    public class HostName
    {
        public HostName(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }

        public HostName GetParent()
        {
            int delimiter = this.Name.IndexOf('.') + 1;
            if (delimiter > 0 && delimiter < this.Name.Length)
            {
                return new HostName(this.Name.Substring(delimiter));
            }
            return null;
        }
    }
}

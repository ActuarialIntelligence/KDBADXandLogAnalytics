using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demo.Infrastructure.connectors.Interfaces
{
    public interface IConnect
    {
        string GetData(string host, int port);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_websocket.Interfaces
{
    public interface ICommandHandler
    {
         Task HandleAsync(int clientId, string payload);
    }
}
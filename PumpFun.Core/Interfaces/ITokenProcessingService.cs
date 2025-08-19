using System;
using System.Threading.Channels;
using PumpFun.Core.Models;

namespace PumpFun.Core.Interfaces;

public interface ITokenProcessingService
{
Task ProcessTokenCreationQueue(
            TokenCreationChannel queue,
            CancellationToken cancellationToken);
}

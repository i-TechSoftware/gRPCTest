using System.Reflection;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Info;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace TestGRPCServer.Services
{
    public class InformerService:Informer.InformerBase
    {
        private readonly ILogger<InformerService> _logger;

        public InformerService(ILogger<InformerService> logger)
        {
            _logger = logger;
        }

        /*[Authorize]*/
        public override Task<ServerInformation> GetServerInformation(Empty request, ServerCallContext context)
        {
            _logger.LogInformation($"Client [{context.Host}] get information about server");
            return Task.FromResult(new ServerInformation() {Version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute> ().InformationalVersion});
            
        }

        public override Task<PingResponse> GetServerPing(Empty request, ServerCallContext context)
        {
            _logger.LogInformation($"Client [{context.Host}] get ping");
            return Task.FromResult(new PingResponse(){PingResponse_ = 1});
        }

    }
}
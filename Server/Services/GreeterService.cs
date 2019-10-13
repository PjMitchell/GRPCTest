using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using TestGrpContract;

namespace TestGrpService
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }

        public override async Task GetNooberStream(Empty request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
        {
            while(!context.CancellationToken.IsCancellationRequested)
            {
                await responseStream.WriteAsync(new HelloReply { Message = "Heya " });
                await Task.Delay(2000);
            }
        }

        public override async Task Echo(IAsyncStreamReader<EchoMessage> requestStream, IServerStreamWriter<EchoMessage> responseStream, ServerCallContext context)
        {
            await responseStream.WriteAsync(new EchoMessage { Message = "Echo" });
            await foreach(var item in requestStream.ReadAllAsync(context.CancellationToken))
            {
                var message = $"Echo {item.Message}";
                _logger.LogInformation(message);
                await responseStream.WriteAsync(new EchoMessage { Message = message });
                await Task.Delay(2000);
            }

        }

        public override async Task<FoodResponse> FeedMe(IAsyncStreamReader<FoodMessage> requestStream, ServerCallContext context)
        {
            int total = 0;
            var items = new List<int>();
            await  foreach(var input in requestStream.ReadAllAsync(context.CancellationToken))
            {
                total += input.Value;
                items.Add(input.Value);
                _logger.LogInformation($"Nom Nom Nom {input.Value}");
                _logger.LogInformation($"More?");
            }

            var message = total switch
            {
                0 => "I am hungry",
                _ when total > 10 => "Some what full",
                _ when total > 300 => "I am full",
                _ => "What happed here?"
            };
            _logger.LogInformation(message);
            var response = new FoodResponse { Total = total, Message = message };
            response.AllValues.Add(items);
            return response;
        }

        public async override Task GetRandomNumberStream(Empty request, IServerStreamWriter<RandomNumber> responseStream, ServerCallContext context)
        {
            var rand = new Random();
            var iteration = rand.Next(1, 6);
            for (int i = 0; i < iteration; i++)
            {
                await responseStream.WriteAsync(new RandomNumber { Number = rand.Next(1, 100) });
            }
        }
    }
}

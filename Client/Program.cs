using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using TestGrpContract;
using static TestGrpContract.Greeter;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using System.Runtime.CompilerServices;

namespace TestClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new GreeterClient(channel);
            await RunFeedMe(client);
            await Task.Delay(60000);
        }

        private static async Task RunBiStream(GreeterClient client)
        {
            var duplex = client.Echo();
            await foreach (var item in duplex.ResponseStream.ToEnumerable())
            {
                var message = item.Message;
                if (message.Length > 10000)
                    message = "Let's start again";
                Console.WriteLine(message);
                await duplex.RequestStream.WriteAsync(new EchoMessage { Message = message });
            }
        }

        private static async Task RunFeedMe(GreeterClient client)
        {
            var feedMeRequest = client.FeedMe();
            var random = client.GetRandomNumberStream(new Empty());


            await foreach (var item in random.ResponseStream.ToEnumerable())
            {
                await feedMeRequest.RequestStream.WriteAsync(new FoodMessage { Value = item.Number });
                await Task.Delay(2000);
            }

            await feedMeRequest.RequestStream.CompleteAsync();
            var result = await feedMeRequest.ResponseAsync;
            Console.WriteLine(result.Message);
            Console.WriteLine(result.Total);
            Console.WriteLine(string.Join(',', result.AllValues));
        }

        private static async Task RunStream(GreeterClient client)
        {
            await foreach (var item in Go(client, CancellationToken.None).Select((s, i) => $"{s} ({i})"))
            {
                Console.WriteLine(item);
            }
        }

        static async IAsyncEnumerable<string> Go(GreeterClient client, [EnumeratorCancellation]  CancellationToken ct)
        {
            using (var stream = client.GetNooberStream(new Empty(), null,null, ct))
            {
                while(await stream.ResponseStream.MoveNext(ct))
                {
                    yield return stream.ResponseStream.Current.Message;
                }

            }
        }
    }

    public static class FakeLinq
    {
        public static async IAsyncEnumerable<TResult> ToEnumerable<TResult>(this IAsyncStreamReader<TResult> source, [EnumeratorCancellation]CancellationToken token = default)
        {
            while (await source.MoveNext(token))
            {
                yield return source.Current;
            }
        }

        public static async IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, TResult> func, [EnumeratorCancellation] CancellationToken token = default)
        {
            var i = 0;
            await foreach(var item in source)
            {
                if (token.IsCancellationRequested)
                    yield break;
                yield return func(item, i);
                i++;
            }

        }

           public static IAsyncEnumerable<TResult> Select<TSource, TResult>(IAsyncEnumerable<TSource> source, Func<TSource, TResult> func, CancellationToken token = default)
        {
            return source.Select((s, i) => func(s), token);
        }
    }
}

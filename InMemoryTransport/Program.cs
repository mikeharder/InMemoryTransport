using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace InMemoryTransport
{
    public class Program
    {
        private static readonly TimeSpan _duration = TimeSpan.FromSeconds(5);
        private const int _requests = 10000000;

        public static void Main(string[] args)
        {
            PrintVersions();

            //Console.WriteLine($"Benchmarking for {_duration}...");
            Console.WriteLine($"Benchmarking {_requests} requests...");

            var b = new InMemoryTransportBenchmark();
            b.GlobalSetupPlaintext();

            var token = new CancellationTokenSource(_duration).Token;

            var iterations = 0;
            var sw = Stopwatch.StartNew();
            var seconds = 0;

            var tasks = new Task[b.Connections.Count];
            for (var i = 0; i < tasks.Length; i++)
            {
                var index = i;
                tasks[index] = Task.Run(async () =>
                {
                    while (Interlocked.Increment(ref iterations) * InMemoryTransportBenchmark.PipelineDepth < _requests)
                    {
                        await b.Connections[index].SendRequestAsync(InMemoryTransportBenchmark._plaintextPipelinedRequest);
                        await b.Connections[index].GetResponseAsync(InMemoryTransportBenchmark._plaintextPipelinedExpectedResponseLength);

                        var newSeconds = sw.Elapsed.Seconds;
                        if (Interlocked.Exchange(ref seconds, newSeconds) != newSeconds)
                        {
                            Console.WriteLine($"[{sw.Elapsed}] {iterations * InMemoryTransportBenchmark.PipelineDepth} requests");
                        }
                    }
                });
            }
            Task.WaitAll(tasks);

            /*
            // while (!token.IsCancellationRequested)
            while (iterations * InMemoryTransportBenchmark.PipelineDepth < _requests)
            {
                // b.Plaintext();
                b.PlaintextPipelined();
                iterations++;
                if (sw.Elapsed.Seconds != seconds)
                {
                    Console.WriteLine($"[{sw.Elapsed}] {iterations * InMemoryTransportBenchmark.PipelineDepth} requests");
                    seconds = sw.Elapsed.Seconds;
                }
            }
            */

            sw.Stop();

            // var rps = iterations / sw.Elapsed.TotalSeconds;
            var rps = iterations * InMemoryTransportBenchmark.PipelineDepth / sw.Elapsed.TotalSeconds;

            // Plaintext
            // netcoreapp2.0:  78k RPS
            // netcoreapp2.1: 106k RPS

            // PlaintextPipelined
            // netcoreapp2.0: 130k RPS
            // netcoreapp2.1: 152k RPS

            // PlaintextPipelined, 256 parallel connections
            // netcoreapp2.0:  887k RPS
            // netcoreapp2.1: 1172k RPS

            Console.WriteLine($"{iterations * InMemoryTransportBenchmark.PipelineDepth} requests in {sw.Elapsed}");
            Console.WriteLine($"{Math.Round(rps)} requests / second");
        }

        private static void PrintVersions()
        {
#if NETCOREAPP2_1
            Console.WriteLine("TargetFramework: netcoreapp2.1");
#elif NETCOREAPP2_0
            Console.WriteLine("TargetFramework: netcoreapp2.0");
#else
#error Invalid TFM
#endif

            var kestrelVersion = FileVersionInfo.GetVersionInfo(
                typeof(Microsoft.AspNetCore.Hosting.WebHostBuilderKestrelExtensions).Assembly.Location).ProductVersion;
            Console.WriteLine($"Kestrel: {kestrelVersion}");

            Console.WriteLine();
        }
    }
}

using System;
using System.Diagnostics;
using System.Threading;

namespace InMemoryTransport
{
    public class Program
    {
        private static readonly TimeSpan _duration = TimeSpan.FromSeconds(5);

        public static void Main(string[] args)
        {
#if NETCOREAPP2_1
            Console.WriteLine("netcoreapp2.1" + Environment.NewLine);
#elif NETCOREAPP2_0
            Console.WriteLine("netcoreapp2.0" + Environment.NewLine);
#else
#error Invalid TFM
#endif

            Console.WriteLine($"Benchmarking for {_duration}...");

            var b = new InMemoryTransportBenchmark();
            b.GlobalSetupPlaintext();

            var token = new CancellationTokenSource(_duration).Token;

            var sw = Stopwatch.StartNew();
            var iterations = 0;
            while (!token.IsCancellationRequested)
            {
                b.PlaintextPipelined();
                iterations++;
            }
            sw.Stop();

            var rps = iterations * InMemoryTransportBenchmark.PipelineDepth / sw.Elapsed.TotalSeconds;

            // netcoreapp2.0:   7,875 RPS
            // netcoreapp2.1: 153,905 RPS
            Console.WriteLine($"{Math.Round(rps)} requests / second");
            Console.ReadLine();
        }
    }
}

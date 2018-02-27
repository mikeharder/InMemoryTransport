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
                // b.Plaintext();
                b.PlaintextPipelined();
                iterations++;
            }
            sw.Stop();

            // var rps = iterations / sw.Elapsed.TotalSeconds;
            var rps = iterations * InMemoryTransportBenchmark.PipelineDepth / sw.Elapsed.TotalSeconds;

            // Plaintext
            // netcoreapp2.0: 78k RPS
            // netcoreapp2.1: 106k RPS

            // PlaintextPipelined
            // netcoreapp2.0: 130k RPS
            // netcoreapp2.1: 152k RPS

            Console.WriteLine($"{Math.Round(rps)} requests / second");
        }
    }
}

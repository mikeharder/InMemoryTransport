using BenchmarkDotNet.Attributes;
using Benchmarks.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InMemoryTransport
{
    public class InMemoryTransportBenchmark
    {
        // Must use explicit line endings to ensure identical string on all platforms
        public static readonly byte[] _plaintextRequest = Encoding.UTF8.GetBytes(
            "GET /plaintext HTTP/1.1\r\n" +
            "Host: localhost\r\n" +
            "Accept: text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7\r\n" +
            "Connection: keep-alive\r\n" +
            "\r\n");

        public const int _plaintextExpectedResponseLength = 132;

        public const int PipelineDepth = 16;
        public static readonly byte[] _plaintextPipelinedRequest = Enumerable.Range(0, PipelineDepth).SelectMany(_ => _plaintextRequest).ToArray();
        public const int _plaintextPipelinedExpectedResponseLength = _plaintextExpectedResponseLength * PipelineDepth;

        private IWebHost _host;
        public IReadOnlyList<InMemoryTransportFactory.InMemoryConnection> Connections { get; private set; }

        [GlobalSetup(Target = nameof(Plaintext) + "," + nameof(PlaintextPipelined))]
        public void GlobalSetupPlaintext()
        {
            _host = new WebHostBuilder()
                .UseSetting("preventHostingStartup", "true")
                .UseKestrel()
                .UseUrls("http://localhost:5000")
                .ConfigureServices(services => services.AddSingleton<ITransportFactory>(new InMemoryTransportFactory()))
                .Configure(app => app.UseMiddleware<PlaintextMiddleware>())
                .Build();

            _host.Start();

            Connections = ((InMemoryTransportFactory)_host.Services.GetRequiredService<ITransportFactory>()).Connections.Values.First();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _host.Dispose();
        }

        [Benchmark]
        public void Plaintext()
        {
            Connections[0].SendRequestAsync(_plaintextRequest).Wait();
            Connections[0].GetResponseAsync(_plaintextExpectedResponseLength).Wait();
        }

        [Benchmark]
        public void PlaintextPipelined()
        {
            Connections[0].SendRequestAsync(_plaintextPipelinedRequest).Wait();
            Connections[0].GetResponseAsync(_plaintextPipelinedExpectedResponseLength).Wait();
        }
    }
}

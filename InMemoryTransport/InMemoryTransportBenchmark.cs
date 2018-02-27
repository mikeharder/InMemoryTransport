using BenchmarkDotNet.Attributes;
using Benchmarks.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Text;

namespace InMemoryTransport
{
    public class InMemoryTransportBenchmark
    {
        private static readonly byte[] _plaintextRequest = Encoding.UTF8.GetBytes(
@"GET /plaintext HTTP/1.1
Host: localhost
Accept: text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7
Connection: keep-alive

");
        private const int _plaintextExpectedResponseLength = 132;

        public const int PipelineDepth = 16;
        private static readonly byte[] _plaintextPipelinedRequest = Enumerable.Range(0, PipelineDepth).SelectMany(_ => _plaintextRequest).ToArray();
        private const int _plaintextPipelinedExpectedResponseLength = _plaintextExpectedResponseLength * PipelineDepth;

        private IWebHost _host;
        private InMemoryTransportFactory.InMemoryConnection _connection;

        [GlobalSetup(Target = nameof(Plaintext) + "," + nameof(PlaintextPipelined))]
        public void GlobalSetupPlaintext()
        {
            _host = new WebHostBuilder()
                .UseSetting("preventHostingStartup", "true")
                .UseKestrel()
                .UseUrls("http://localhost:5000")
                .ConfigureServices(services => services.AddSingleton<ITransportFactory, InMemoryTransportFactory>())
                .Configure(app => app.UseMiddleware<PlaintextMiddleware>())
                .Build();

            _host.Start();

            _connection = ((InMemoryTransportFactory)_host.Services.GetRequiredService<ITransportFactory>()).Connection;
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _host.Dispose();
        }

        [Benchmark]
        public void Plaintext()
        {
            _connection.SendRequestAsync(_plaintextRequest).Wait();
            _connection.GetResponseAsync(_plaintextExpectedResponseLength).Wait();
        }

        [Benchmark]
        public void PlaintextPipelined()
        {
            _connection.SendRequestAsync(_plaintextPipelinedRequest).Wait();
            _connection.GetResponseAsync(_plaintextPipelinedExpectedResponseLength).Wait();
        }
    }
}

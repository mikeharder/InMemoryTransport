using Benchmarks.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;

namespace InMemoryTransport
{
    public class Program
    {
        private static readonly byte[] _request = Encoding.UTF8.GetBytes("GET /plaintext HTTP/1.1\r\nHost: localhost:5000\r\n\r\n");

        public static void Main(string[] args)
        {
#if NETCOREAPP2_1
            Console.WriteLine("netcoreapp2.1" + Environment.NewLine);
#elif NETCOREAPP2_0
            Console.WriteLine("netcoreapp2.0" + Environment.NewLine);
#else
#error Invalid TFM
#endif

            var host = new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(services => services.AddSingleton<ITransportFactory, InMemoryTransportFactory>())
                .Configure(app => app.UsePlainText())
                .Build();

            host.Start();

            var connection = ((InMemoryTransportFactory)host.Services.GetRequiredService<ITransportFactory>()).Connection;
            connection.SendRequestAsync(_request).Wait();
            Console.WriteLine(Encoding.UTF8.GetString(connection.GetResponseAsync().Result));

            Console.ReadLine();
        }
    }
}

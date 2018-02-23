using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InMemoryTransport
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if NETCOREAPP2_1
            Console.WriteLine("netcoreapp2.1");
#elif NETCOREAPP2_0
            Console.WriteLine("netcoreapp2.0");
#else
#error Invalid TFM
#endif

            var host = new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(services => services.AddSingleton<ITransportFactory, InMemoryTransportFactory>())
                .Configure(app => app.Run(context =>
                {
                    return context.Response.WriteAsync("Hello World!");
                }))
                .Build();

            host.Start();

            var connection = ((InMemoryTransportFactory)host.Services.GetRequiredService<ITransportFactory>()).Connection;

            connection.SendRequestAsync("GET / HTTP/1.1\r\nHost: host:port\r\n\r\n").Wait();

            Console.WriteLine(connection.GetResponseAsync().Result);

            Console.ReadLine();
        }
    }
}

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

#if NETCOREAPP2_1
using System.Buffers;
using System.IO.Pipelines;
#elif NETCOREAPP2_0
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
#endif

namespace InMemoryTransport
{
    public class InMemoryTransportFactory : ITransportFactory
    {
        public InMemoryConnection Connection { get; } = new InMemoryConnection();

        public ITransport Create(IEndPointInformation endPointInformation, IConnectionHandler handler)
        {
            return new InMemoryTransport(handler, Connection);
        }

        private class InMemoryTransport : ITransport
        {
            private IConnectionHandler _handler;
            private InMemoryConnection _connection;

            public InMemoryTransport(IConnectionHandler handler, InMemoryConnection connection)
            {
                _handler = handler;
                _connection = connection;
            }

            public Task BindAsync()
            {
#if NETCOREAPP2_1
                _handler.OnConnection(_connection);
#elif NETCOREAPP2_0
                var connectionContext = _handler.OnConnection(_connection);
                _connection.ConnectionId = connectionContext.ConnectionId;
                _connection.Input = connectionContext.Input;
                _connection.Output = connectionContext.Output;
#endif
                return Task.CompletedTask;
            }

            public Task StopAsync()
            {
                return Task.CompletedTask;
            }

            public Task UnbindAsync()
            {
                return Task.CompletedTask;
            }
        }
#if NETCOREAPP2_1
        public class InMemoryConnection : TransportConnection
#elif NETCOREAPP2_0
        public class InMemoryConnection : IConnectionContext, IConnectionInformation
#endif
        {

#if NETCOREAPP2_0
            public string ConnectionId { get; set; }

            public IPipeWriter Input { get; set; }

            public IPipeReader Output { get; set; }

            public IPEndPoint RemoteEndPoint { get; set; }

            public IPEndPoint LocalEndPoint { get; set; }

            public PipeFactory PipeFactory { get; set; } = new PipeFactory();

            public IScheduler InputWriterScheduler { get; set; }

            public IScheduler OutputReaderScheduler { get; set; }
#endif

            public Task SendRequestAsync(byte[] request)
            {
                return Input.WriteAsync(request);
            }

            public async Task<byte[]> GetResponseAsync(int length)
            {
                while (true)
                {
                    var result = await Output.ReadAsync();

                    var bufferLength = result.Buffer.Length;
                    if (bufferLength >= length)
                    {
                        var responseBuffer = result.Buffer.Slice(0, length);
#if NETCOREAPP2_1
                        Output.AdvanceTo(responseBuffer.End, responseBuffer.End);
#elif NETCOREAPP2_0
                        Output.Advance(responseBuffer.End, responseBuffer.End);
#endif
                        return responseBuffer.ToArray();
                    }
                    else if (bufferLength < length)
                    {
#if NETCOREAPP2_1
                        Output.AdvanceTo(result.Buffer.Start, result.Buffer.End);
#elif NETCOREAPP2_0
                        Output.Advance(result.Buffer.Start, result.Buffer.End);
#endif
                    }
                }
            }

#if NETCOREAPP2_0
            public void OnConnectionClosed(Exception ex)
            {
            }

            public void Abort(Exception ex)
            {
            }
#endif
        }
    }
}

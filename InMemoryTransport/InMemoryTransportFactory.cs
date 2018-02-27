using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using System.Collections.Generic;
#if NETCOREAPP2_1
using System.IO.Pipelines;
#elif NETCOREAPP2_0
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using System;
using System.Net;
#endif

namespace InMemoryTransport
{
    public class InMemoryTransportFactory : ITransportFactory
    {
        private const int _numConnections = 256;

        private readonly Dictionary<IEndPointInformation, IReadOnlyList<InMemoryConnection>> _connections =
            new Dictionary<IEndPointInformation, IReadOnlyList<InMemoryConnection>>();

        public IReadOnlyDictionary<IEndPointInformation, IReadOnlyList<InMemoryConnection>> Connections => _connections;

        public ITransport Create(IEndPointInformation endPointInformation, IConnectionHandler handler)
        {
            var connections = new InMemoryConnection[_numConnections];
            for (var i=0; i < _numConnections; i++)
            {
                connections[i] = new InMemoryConnection();
            }

            _connections.Add(endPointInformation, connections);

            return new InMemoryTransport(handler, connections);
        }

        private class InMemoryTransport : ITransport
        {
            private readonly IConnectionHandler _handler;
            private readonly IReadOnlyList<InMemoryConnection> _connections;

            public InMemoryTransport(IConnectionHandler handler, IReadOnlyList<InMemoryConnection> connections)
            {
                _handler = handler;
                _connections = connections;
            }

            public Task BindAsync()
            {
                foreach (var connection in _connections)
                {
#if NETCOREAPP2_1
                    _handler.OnConnection(connection);
#elif NETCOREAPP2_0
                    var connectionContext = _handler.OnConnection(connection);
                    connection.ConnectionId = connectionContext.ConnectionId;
                    connection.Input = connectionContext.Input;
                    connection.Output = connectionContext.Output;
#endif
                }

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
                    var buffer = result.Buffer;
                    var consumed = buffer.Start;
                    var examined = buffer.End;

                    try
                    {
                        if (buffer.Length >= length)
                        {
                            var response = buffer.Slice(0, length);
                            consumed = response.End;
                            examined = response.End;
                            return response.ToArray();
                        }
                    }
                    finally
                    {
#if NETCOREAPP2_1
                        Output.AdvanceTo(consumed, examined);
#elif NETCOREAPP2_0
                        Output.Advance(consumed, examined);
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

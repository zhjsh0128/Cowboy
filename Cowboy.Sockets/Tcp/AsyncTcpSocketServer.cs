﻿using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cowboy.Buffer;
using Cowboy.Logging;

namespace Cowboy.Sockets
{
    public class AsyncTcpSocketServer
    {
        #region Fields

        private static readonly ILog _log = Logger.Get<AsyncTcpSocketServer>();
        private IBufferManager _bufferManager;
        private TcpListener _listener;
        private readonly ConcurrentDictionary<string, AsyncTcpSocketSession> _sessions = new ConcurrentDictionary<string, AsyncTcpSocketSession>();
        private readonly object _opsLock = new object();
        private readonly TcpSocketServerConfiguration _configuration;

        #endregion

        #region Constructors

        public AsyncTcpSocketServer(int listenedPort, TcpSocketServerConfiguration configuration = null)
            : this(IPAddress.Any, listenedPort, configuration)
        {
        }

        public AsyncTcpSocketServer(IPAddress listenedAddress, int listenedPort, TcpSocketServerConfiguration configuration = null)
            : this(new IPEndPoint(listenedAddress, listenedPort), configuration)
        {
        }

        public AsyncTcpSocketServer(IPEndPoint listenedEndPoint, TcpSocketServerConfiguration configuration = null)
        {
            if (listenedEndPoint == null)
                throw new ArgumentNullException("listenedEndPoint");

            this.ListenedEndPoint = listenedEndPoint;
            _configuration = configuration ?? new TcpSocketServerConfiguration();

            Initialize();
        }

        private void Initialize()
        {
            _bufferManager = new GrowingByteBufferManager(_configuration.InitialBufferAllocationCount, _configuration.ReceiveBufferSize);
        }

        #endregion

        #region Properties

        public IPEndPoint ListenedEndPoint { get; private set; }
        public bool Active { get; private set; }
        public int SessionCount { get { return _sessions.Count; } }

        #endregion

        #region Server

        public void Start()
        {
            lock (_opsLock)
            {
                if (Active)
                    return;

                try
                {
                    _listener = new TcpListener(this.ListenedEndPoint);
                    _listener.AllowNatTraversal(_configuration.AllowNatTraversal);
                    _listener.ExclusiveAddressUse = _configuration.ExclusiveAddressUse;

                    _listener.Start(_configuration.PendingConnectionBacklog);
                    Active = true;

                    Task.Run(async () =>
                    {
                        await Accept();
                    })
                    .Forget();
                }
                catch (Exception ex)
                {
                    if (ex is SocketException)
                    {
                        _log.Error(ex.Message, ex);
                    }
                    else throw;
                }
            }
        }

        public void Stop()
        {
            lock (_opsLock)
            {
                if (!Active)
                    return;

                try
                {
                    Active = false;
                    _listener.Stop();
                    _listener = null;
                }
                catch (Exception ex)
                {
                    if (ex is SocketException)
                    {
                        _log.Error(ex.Message, ex);
                    }
                    else throw;
                }
            }
        }

        public bool Pending()
        {
            lock (_opsLock)
            {
                if (!Active)
                    throw new InvalidOperationException("The TCP server is not active.");

                // determine if there are pending connection requests.
                return _listener.Pending();
            }
        }

        private async Task Accept()
        {
            while (Active)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync();
                var session = new AsyncTcpSocketSession(tcpClient, _configuration, _bufferManager);
                Task.Run(async () =>
                {
                    await Process(session);
                })
                .Forget();
            }
        }

        private async Task Process(AsyncTcpSocketSession session)
        {
            string sessionKey = session.RemoteEndPoint.ToString();
            if (_sessions.TryAdd(sessionKey, session))
            {
                try
                {
                    await session.Start();
                }
                finally
                {
                    AsyncTcpSocketSession throwAway;
                    _sessions.TryRemove(sessionKey, out throwAway);
                }
            }
        }

        #endregion
    }
}
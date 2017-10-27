﻿using Orchard.Logging;
using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace Orchard.Metrics {
    public class StatsDMetricsClientOptions : MetricsClientOptionsBase {
        public string ServerName { get; set; }
        public int Port { get; set; }
    }

    public class StatsDMetricsClient : IMetricsClient {
        private readonly object _lock = new object();
        private Socket _socket;
        private readonly IPEndPoint _endPoint;
        private readonly StatsDMetricsClientOptions _options;
        private readonly ILogger _logger;


        public StatsDMetricsClient(StatsDMetricsClientOptions options) {
            _options = options;
            if (options.LoggerFactory == null)
            {
                options.LoggerFactory = new NullLoggerFactory();
            }
            _logger = options.LoggerFactory.GetLogger(typeof(StatsDMetricsClient));
            _endPoint = new IPEndPoint(IPAddress.Parse(options.ServerName), options.Port);

            if (!String.IsNullOrEmpty(options.Prefix))
                options.Prefix = options.Prefix.EndsWith(".") ? options.Prefix : String.Concat(options.Prefix, ".");
        }

        public Task CounterAsync(string name, int value = 1) {
            Send(BuildMetric("c", name, value.ToString(CultureInfo.InvariantCulture)));
            return Task.CompletedTask;
        }

        public Task GaugeAsync(string name, double value) {
            Send(BuildMetric("g", name, value.ToString(CultureInfo.InvariantCulture)));
            return Task.CompletedTask;
        }

        public Task TimerAsync(string name, int milliseconds) {
            Send(BuildMetric("ms", name, milliseconds.ToString(CultureInfo.InvariantCulture)));
            return Task.CompletedTask;
        }

        private string BuildMetric(string type, string statName, string value) {
            return String.Concat(_options.Prefix, statName, ":", value, "|", type);
        }

        private void Send(string metric) {
            if (String.IsNullOrEmpty(metric))
                return;

            try {
                var data = Encoding.ASCII.GetBytes(metric);

                EnsureSocket();
                _socket?.SendTo(data, _endPoint);
            } catch (Exception ex) {
                _logger.ErrorFormat(ex,"An error occurred while sending the metrics: {0}", ex.Message);
                ResetUdpClient();
            }
        }

        private void EnsureSocket() {
            if (_socket != null)
                return;

            lock (_lock) {
                if (_socket == null)
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
        }

        private void ResetUdpClient() {
            if (_socket == null)
                return;

            lock (_lock) {
                if (_socket == null)
                    return;

                try {
                    _socket.Close();
                } catch (Exception ex) {
                    _logger.Error("An error occurred while calling Close() on the socket.",ex);
                } finally {
                    _socket = null;
                }
            }
        }

        public void Dispose() {
            ResetUdpClient();
        }
    }
}
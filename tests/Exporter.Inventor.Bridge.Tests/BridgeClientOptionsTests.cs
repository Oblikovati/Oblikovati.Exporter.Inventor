// SPDX-License-Identifier: GPL-2.0-only

using System;
using Oblikovati.Exporter.Inventor.Bridge;
using Xunit;

namespace Oblikovati.Exporter.Inventor.Bridge.Tests
{
    public sealed class BridgeClientOptionsTests
    {
        [Fact]
        public void Defaults_to_the_local_bridge_endpoint()
        {
            var options = new BridgeClientOptions();

            Assert.Equal("http://127.0.0.1:7800/mcp", options.Endpoint.ToString());
            Assert.Equal(BridgeClientOptions.DefaultEndpoint, options.Endpoint.ToString());
        }

        [Fact]
        public void Carries_a_client_name_and_a_positive_timeout()
        {
            var options = new BridgeClientOptions();

            Assert.False(string.IsNullOrWhiteSpace(options.ClientName));
            Assert.True(options.ConnectionTimeout > TimeSpan.Zero);
        }

        [Fact]
        public void Endpoint_is_overridable()
        {
            var options = new BridgeClientOptions { Endpoint = new Uri("http://localhost:9000/mcp") };

            Assert.Equal("http://localhost:9000/mcp", options.Endpoint.ToString());
        }
    }
}

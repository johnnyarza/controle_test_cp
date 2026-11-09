using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace ProbetaControl.Tests.Infrastructure
{
    /// <summary>One MySQL server from TEST_SERVERS (host:port).</summary>
    public sealed class TestServer
    {
        public TestServer(string host, uint port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; }

        public uint Port { get; }

        public static TestServer Parse(string value)
        {
            var parts = value.Split(':');
            if (parts.Length != 2 || !uint.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var port))
            {
                throw new FormatException($"TEST_SERVERS entry '{value}' must look like host:port.");
            }

            return new TestServer(parts[0], port);
        }

        /// <summary>Fixture arguments: one per configured server, or a single "not configured" marker.</summary>
        public static IEnumerable<TestFixtureData> FixtureSource()
        {
            var settings = TestSettings.Current;
            if (!settings.IsConfigured)
            {
                yield return new TestFixtureData(new object[] { null }).SetArgDisplayNames("not-configured");
                yield break;
            }

            foreach (var server in settings.Servers)
            {
                yield return new TestFixtureData(server).SetArgDisplayNames(server.ToString());
            }
        }

        public override string ToString() => $"{Host}:{Port}";
    }
}

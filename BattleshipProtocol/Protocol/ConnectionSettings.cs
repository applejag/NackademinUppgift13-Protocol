using System;
using System.Text;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public class ConnectionSettings
    {
        /// <summary>
        /// Address for connecting to host. Is ignored when hosting.
        /// Default: <see cref="string.Empty"/>
        /// </summary>
        [NotNull]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Port for connecting or hosting upon.
        /// Default: <code>8888</code>
        /// </summary>
        public ushort Port { get; set; } = 8888;

        /// <summary>
        /// Encoding used when receiving and sending packets.
        /// Default: <see cref="System.Text.Encoding.UTF8"/> with BOM emitting disabled.
        /// </summary>
        public Encoding Encoding { get; set; } = new UTF8Encoding(false);

        /// <summary>
        /// Enables auto detecting encoding from the initial BOM.
        /// Default: <code>true</code>
        /// </summary>
        public bool DetectEncodingFromBOM { get; set; } = true;
    }
}
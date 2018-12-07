using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace BattleshipProtocol.Protocol
{
    public class BattleStream : Stream
    {
        private readonly Stream _stream;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;
        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes the Battleship commands stream with encoding <see cref="P:System.Text.Encoding.UTF8" />
        /// </summary>
        /// <param name="stream">The stream to use when reading and writing data.</param>
        public BattleStream([NotNull] Stream stream)
            : this(stream, Encoding.UTF8)
        { }

        /// <inheritdoc />
        /// <summary>
        /// Initializes the Battleship commands stream with custom encoding.
        /// </summary>
        /// <param name="stream">The stream to use when reading and writing data.</param>
        /// <param name="encoding">The encoding to use when reading and writing data.</param>
        public BattleStream([NotNull] Stream stream, [NotNull] Encoding encoding)
        {
            _stream = stream;
            _reader = new StreamReader(stream, encoding);
            _writer = new StreamWriter(stream, encoding);
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                using (_stream)
                using (_reader)
                using (_writer)
                { }
            }
            base.Dispose(disposing);
        }
    }
}
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitHubProxy.Helper
{
    public sealed class NoneDispoableWriteStream : Stream
    {
        private readonly Stream _stream;

        public NoneDispoableWriteStream(Stream stream)
        {
            _stream = stream;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() => _stream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _stream.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => _stream.Write(buffer);
        public override void WriteByte(byte value) => _stream.WriteByte(value);
        public override int WriteTimeout { get => _stream.WriteTimeout; set => _stream.WriteTimeout = value; }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _stream.WriteAsync(buffer, offset, count, cancellationToken);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _stream.WriteAsync(buffer, cancellationToken);

    }
}

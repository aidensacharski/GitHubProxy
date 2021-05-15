using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace GitHubProxy.Helper
{
    public class LightweightBufferedPipeWriter
    {
        private const int DefaultBufferSize = 16384;

        private readonly PipeWriter _writer;

        private Memory<byte> _buffer;
        private int _bytesFilled;

        public LightweightBufferedPipeWriter(PipeWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public ValueTask WriteAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if (buffer.IsSingleSegment)
            {
                return WriteAsync(buffer.First, cancellationToken);
            }

            return new ValueTask(WriteSlowAsync(buffer, cancellationToken));
        }

        private async Task WriteSlowAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            foreach (ReadOnlyMemory<byte> segment in buffer)
            {
                await WriteAsync(segment, cancellationToken).ConfigureAwait(false);
            }
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if ((_buffer.Length - _bytesFilled) < buffer.Length)
            {
                if (!_buffer.IsEmpty || buffer.Length > DefaultBufferSize)
                {
                    return new ValueTask(WriteWithFlushAsync(buffer, cancellationToken));
                }

                _buffer = _writer.GetMemory(Math.Max(DefaultBufferSize, buffer.Length));
            }

            buffer.CopyTo(_buffer.Slice(_bytesFilled));
            _bytesFilled += buffer.Length;
            return default;
        }

        private async Task WriteWithFlushAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (!_buffer.IsEmpty)
            {
                int copySize = Math.Min(_buffer.Length - _bytesFilled, buffer.Length);
                buffer.Slice(0, copySize).CopyTo(_buffer.Slice(_bytesFilled));
                _writer.Advance(_bytesFilled + copySize);
                await _writer.FlushAsync(cancellationToken);
                buffer = buffer.Slice(copySize);
                _bytesFilled = 0;
            }

            while (buffer.Length >= DefaultBufferSize)
            {
                _buffer = _writer.GetMemory(DefaultBufferSize);
                int copySize = Math.Min(_buffer.Length, buffer.Length);
                buffer.Slice(0, copySize).CopyTo(_buffer);
                buffer = buffer.Slice(copySize);
                _writer.Advance(copySize);
                await _writer.FlushAsync(cancellationToken);
            }
            _buffer = default;

            if (!buffer.IsEmpty)
            {
                _buffer = _writer.GetMemory(Math.Max(DefaultBufferSize, buffer.Length));
                buffer.CopyTo(_buffer);
                _bytesFilled = buffer.Length;
            }
        }

        public async Task CompleteAsync(CancellationToken cancellationToken)
        {
            if (!_buffer.IsEmpty && _bytesFilled > 0)
            {
                _writer.Advance(_bytesFilled);
            }
            _buffer = default;
            _bytesFilled = 0;
            try
            {
                try
                {
                    await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    await _writer.CompleteAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                // Ignore error
            }
        }

    }
}

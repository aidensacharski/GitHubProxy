using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GitHubProxy.Helper
{
    public sealed class Utf8HtmlAttributeReplaceContent : HttpContent
    {
        private const string DefaultMediaType = "text/plain";

        private ILogger _logger = NullLogger.Instance;

        private readonly Stream _stream;
        private readonly Utf8StringReplaceDirective[] _directives;
        private readonly CancellationToken _cancellationToken;

        public Utf8HtmlAttributeReplaceContent(string content, Utf8StringReplaceDirective[] directives, string? mediaType, CancellationToken cancellationToken)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (directives is null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            PooledGrowableBufferHelper.PooledMemoryStream? ms = PooledGrowableBufferHelper.PooledMemoryStreamManager.Shared.GetStream();
            Encoding.UTF8.GetBytes(content.AsSpan(), ms);
            ms.Seek(0, SeekOrigin.Begin);
            _stream = ms;
            _directives = directives;
            _cancellationToken = cancellationToken;

            MediaTypeHeaderValue headerValue = new MediaTypeHeaderValue((mediaType is null) ? DefaultMediaType : mediaType);
            headerValue.CharSet = Encoding.UTF8.WebName;
            Headers.ContentType = headerValue;
        }

        public Utf8HtmlAttributeReplaceContent(Stream utf8Stream, Utf8StringReplaceDirective[] directives, string? mediaType, CancellationToken cancellationToken)
        {
            if (utf8Stream is null)
            {
                throw new ArgumentNullException(nameof(utf8Stream));
            }
            if (directives is null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            _stream = utf8Stream;
            _directives = directives;
            _cancellationToken = cancellationToken;

            MediaTypeHeaderValue headerValue = new MediaTypeHeaderValue((mediaType is null) ? DefaultMediaType : mediaType);
            headerValue.CharSet = Encoding.UTF8.WebName;
            Headers.ContentType = headerValue;
        }

        public void SetLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        private int CalculateHoldingSize()
        {
            int holdingSize = 0;
            foreach (Utf8StringReplaceDirective directive in _directives)
            {
                holdingSize = Math.Max(holdingSize, directive.OriginalString.Length);
            }
            return holdingSize;
        }

        private async Task SerializeToWriter(LightweightBufferedPipeWriter writer, CancellationToken cancellationToken)
        {
            int holdingSize = CalculateHoldingSize();
            PipeReader source = PipeReader.Create(_stream);
            try
            {
                ReadState state = ReadState.FindingNextTag;
                bool isScriptTag = false;

                while (true)
                {
                    ReadResult readResult = await source.ReadAsync(cancellationToken).ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = readResult.Buffer;

                    switch (state)
                    {
                        case ReadState.FindingNextTag:
                            {
                                if (buffer.IsEmpty)
                                {
                                    if (readResult.IsCompleted || readResult.IsCanceled)
                                    {
                                        return;
                                    }
                                }

                                SequencePosition? pos = buffer.PositionOf((byte)'<');
                                ReadOnlySequence<byte> bytesToFlush;
                                if (pos.HasValue)
                                {
                                    pos = buffer.Slice(pos.GetValueOrDefault()).Slice(1).Start;
                                    bytesToFlush = buffer.Slice(0, pos.GetValueOrDefault());
                                    state = ReadState.ProcessingTagName;
                                }
                                else
                                {
                                    bytesToFlush = buffer;
                                    pos = buffer.End;
                                }

                                await writer.WriteAsync(bytesToFlush, cancellationToken).ConfigureAwait(false);
                                source.AdvanceTo(pos.GetValueOrDefault());
                            }
                            break;
                        case ReadState.ProcessingTagName:
                            {
                                if (buffer.Length >= 3)
                                {
                                    if (IsXmlTagStart(buffer))
                                    {
                                        buffer = buffer.Slice(0, 3);
                                        await writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                                        source.AdvanceTo(buffer.End);
                                        state = ReadState.FindingXmlTagEnd;
                                        break;
                                    }
                                }

                                SequencePosition? spacePos = buffer.PositionOf((byte)' ');
                                SequencePosition? endPos = buffer.PositionOf((byte)'>');

                                if (!spacePos.HasValue && !endPos.HasValue)
                                {
                                    if (readResult.IsCompleted || readResult.IsCanceled)
                                    {
                                        await writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                                        return;
                                    }
                                    source.AdvanceTo(buffer.Start, buffer.End);
                                    break;
                                }
                                if (spacePos.HasValue && endPos.HasValue)
                                {
                                    if (buffer.Slice(0, spacePos.GetValueOrDefault()).Length > buffer.Slice(0, endPos.GetValueOrDefault()).Length)
                                    {
                                        spacePos = endPos;
                                    }
                                }
                                else if (!spacePos.HasValue)
                                {
                                    spacePos = endPos;
                                }

                                Debug.Assert(spacePos.HasValue);
                                ReadOnlySequence<byte> tagBuffer = buffer.Slice(0, spacePos.GetValueOrDefault());

                                ParseTagName(tagBuffer, out isScriptTag);

                                await writer.WriteAsync(tagBuffer, cancellationToken).ConfigureAwait(false);
                                source.AdvanceTo(tagBuffer.End);
                                state = ReadState.FindingAttributeOrTagEnd;
                            }
                            break;
                        case ReadState.FindingAttributeOrTagEnd:
                            {
                                if (buffer.IsEmpty)
                                {
                                    if (readResult.IsCompleted || readResult.IsCanceled)
                                    {
                                        return;
                                    }
                                }

                                SequencePosition? quotationMarkPos = buffer.PositionOf((byte)'"');
                                SequencePosition? endPos = buffer.PositionOf((byte)'>');
                                if (!quotationMarkPos.HasValue && !endPos.HasValue)
                                {
                                    await writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                                    source.AdvanceTo(buffer.End);
                                    break;
                                }
                                if (quotationMarkPos.HasValue && endPos.HasValue)
                                {
                                    if (buffer.Slice(0, quotationMarkPos.GetValueOrDefault()).Length > buffer.Slice(0, endPos.GetValueOrDefault()).Length)
                                    {
                                        quotationMarkPos = null;
                                    }
                                }
                                if (quotationMarkPos.HasValue)
                                {
                                    quotationMarkPos = buffer.Slice(quotationMarkPos.GetValueOrDefault()).Slice(1).Start;
                                    buffer = buffer.Slice(0, quotationMarkPos.GetValueOrDefault());

                                    await writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                                    source.AdvanceTo(quotationMarkPos.GetValueOrDefault());
                                    state = ReadState.ProcessingAttribute;
                                    break;
                                }
                                Debug.Assert(endPos.HasValue);

                                endPos = buffer.Slice(endPos.GetValueOrDefault()).Slice(1).Start;
                                buffer = buffer.Slice(0, endPos.GetValueOrDefault());

                                await writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                                source.AdvanceTo(endPos.GetValueOrDefault());
                                state = isScriptTag ? ReadState.FindingScriptTagEnd : ReadState.FindingNextTag;
                            }
                            break;
                        case ReadState.ProcessingAttribute:
                            {
                                if (buffer.IsEmpty)
                                {
                                    if (readResult.IsCompleted || readResult.IsCanceled)
                                    {
                                        return;
                                    }

                                    source.AdvanceTo(buffer.End);
                                    break;
                                }

                                if (buffer.FirstSpan[0] == '"')
                                {
                                    buffer = buffer.Slice(0, 1);
                                    await writer.WriteAsync(buffer.First, cancellationToken).ConfigureAwait(false);
                                    source.AdvanceTo(buffer.End);
                                    state = ReadState.FindingAttributeOrTagEnd;
                                    break;
                                }

                                ReadOnlySequence<byte> searchSpace = buffer;
                                SequencePosition? quotationMarkPos = buffer.PositionOf((byte)'"');
                                if (quotationMarkPos.HasValue)
                                {
                                    searchSpace = buffer.Slice(0, quotationMarkPos.GetValueOrDefault());
                                }
                                else
                                {
                                    if (searchSpace.Length <= holdingSize)
                                    {
                                        if (readResult.IsCompleted || readResult.IsCanceled)
                                        {
                                            await ProcessReplace(_directives, holdingSize, writer, searchSpace, true, cancellationToken);
                                            return;
                                        }
                                        source.AdvanceTo(searchSpace.Start, searchSpace.End);
                                        break;
                                    }
                                }

                                SequencePosition consumed = await ProcessReplace(_directives, holdingSize, writer, searchSpace, quotationMarkPos.HasValue, cancellationToken);
                                source.AdvanceTo(consumed, quotationMarkPos ?? buffer.End);
                            }
                            break;
                        case ReadState.FindingScriptTagEnd:
                            {
                                SequencePosition? endPos = buffer.PositionOf((byte)'<');
                                if (!endPos.HasValue)
                                {
                                    if (buffer.IsEmpty)
                                    {
                                        if (readResult.IsCompleted || readResult.IsCanceled)
                                        {
                                            return;
                                        }
                                    }

                                    await writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                                    source.AdvanceTo(buffer.End);
                                    break;
                                }

                                ReadOnlySequence<byte> bytesToFlush = buffer.Slice(0, endPos.GetValueOrDefault());
                                buffer = buffer.Slice(endPos.GetValueOrDefault());
                                if (!bytesToFlush.IsEmpty)
                                {
                                    await writer.WriteAsync(bytesToFlush, cancellationToken).ConfigureAwait(false);
                                }

                                // </script>
                                if (buffer.Length < 9)
                                {
                                    source.AdvanceTo(buffer.Start, buffer.End);
                                    break;
                                }

                                buffer = buffer.Slice(0, 9);
                                if (ParseScriptEndTag(buffer))
                                {
                                    await writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                                    source.AdvanceTo(buffer.End);
                                    state = ReadState.FindingNextTag;
                                    isScriptTag = false;
                                    break;
                                }

                                buffer = buffer.Slice(0, 1);
                                await writer.WriteAsync(buffer.First, cancellationToken).ConfigureAwait(false);
                                source.AdvanceTo(buffer.End);
                            }
                            break;
                        case ReadState.FindingXmlTagEnd:
                            {
                                bool isFinal = readResult.IsCompleted || readResult.IsCanceled;
                                int pos = IndexOfXmlCommentEndTag(buffer, isFinal);
                                if (pos < 0)
                                {
                                    SequencePosition examined = buffer.End;
                                    long length = Math.Max(0, buffer.Length - (isFinal ? 0 : 3));
                                    buffer = buffer.Slice(0, length);
                                    await writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                                    source.AdvanceTo(buffer.End, examined);
                                    break;
                                }

                                buffer = buffer.Slice(0, pos + 3);
                                await writer.WriteAsync(buffer, cancellationToken);
                                source.AdvanceTo(buffer.End);
                                state = ReadState.FindingNextTag;
                            }
                            break;
                    }
                }
            }
            finally
            {
                await writer.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            var writer = new LightweightBufferedPipeWriter(PipeWriter.Create(new NoneDispoableWriteStream(stream)));
            await SerializeToWriter(writer, _cancellationToken).ConfigureAwait(false);
        }

        protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
        {
            var pipe = new Pipe();
            var writer = new LightweightBufferedPipeWriter(pipe.Writer);
            Task.Run(async () =>
            {
                try
                {
                    await SerializeToWriter(writer, _cancellationToken);
                }
                catch (Exception ex)
                {
                    if (ex is not OperationCanceledException)
                    {
                        _logger.LogWarning(ex, "Transform failed.");
                    }
                }
            }, cancellationToken);
            return Task.FromResult(pipe.Reader.AsStream());
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
            => CreateContentReadStreamAsync(CancellationToken.None);

        private enum ReadState
        {
            FindingNextTag,
            ProcessingTagName,
            FindingAttributeOrTagEnd,
            ProcessingAttribute,
            FindingScriptTagEnd,
            FindingXmlTagEnd,
        }

        private static int Copy(ReadOnlySpan<byte> source, Span<char> destination)
        {
            ref byte sourceRef = ref MemoryMarshal.GetReference(source);
            ref char destinationRef = ref MemoryMarshal.GetReference(destination);
            int copySize = Math.Min(source.Length, destination.Length);
            for (int i = 0; i < copySize; i++)
            {
                Unsafe.Add(ref destinationRef, i) = (char)Unsafe.Add(ref sourceRef, i);
            }
            return copySize;
        }

        private static void Copy(in ReadOnlySequence<byte> source, Span<char> destination)
        {
            if (source.IsSingleSegment)
            {
                Copy(source.FirstSpan, destination);
            }
            else
            {
                foreach (ReadOnlyMemory<byte> segment in source)
                {
                    if (destination.IsEmpty)
                    {
                        break;
                    }

                    int bytesCopied = Copy(segment.Span, destination);
                    destination = destination.Slice(bytesCopied);
                }
            }
        }

        private static bool IsXmlTagStart(in ReadOnlySequence<byte> tag)
        {
            if (tag.Length < 3)
            {
                return false;
            }

            Span<char> charBuffer = stackalloc char[3];

            Copy(tag, charBuffer);
            return "!--".AsSpan().Equals(charBuffer.Slice(0, 3), StringComparison.OrdinalIgnoreCase);
        }

        private static void ParseTagName(in ReadOnlySequence<byte> tag, out bool isScriptTag)
        {
            Span<char> charBuffer = stackalloc char[8];

            if (tag.Length == 6)
            {
                Copy(tag, charBuffer);
                if ("script".AsSpan().Equals(charBuffer.Slice(0, 6), StringComparison.OrdinalIgnoreCase))
                {
                    isScriptTag = true;
                }
            }

            isScriptTag = false;
        }

        private static bool ParseScriptEndTag(in ReadOnlySequence<byte> tag)
        {
            // </script>
            Debug.Assert(tag.Length >= 9);
            Span<char> charBuffer = stackalloc char[9];

            Copy(tag.Slice(0, 9), charBuffer);
            return "</script>".AsSpan().Equals(charBuffer, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ParseXmlCommentEndTag(in ReadOnlySequence<byte> tag)
        {
            // -->
            Debug.Assert(tag.Length >= 3);
            Span<char> charBuffer = stackalloc char[3];

            Copy(tag.Slice(0, 3), charBuffer);
            return "-->".AsSpan().Equals(charBuffer, StringComparison.OrdinalIgnoreCase);
        }

        private static int IndexOfXmlCommentEndTag(ReadOnlySequence<byte> searchSpace, bool isFinal)
        {
            ReadOnlySpan<byte> xmlEndTag = new byte[] { (byte)'-', (byte)'-', (byte)'>' };

            const int holdingSize = 3;
            Span<byte> holdingSpace = stackalloc byte[2 * holdingSize];
            int stoppingSize = isFinal ? 0 : holdingSize;

            int offset = 0;
            while (searchSpace.Length > stoppingSize)
            {
                ReadOnlySpan<byte> firstSegment = searchSpace.FirstSpan;
                if (firstSegment.Length > holdingSize)
                {
                    int index = firstSegment.IndexOf(xmlEndTag);
                    if (index == -1)
                    {
                        int skipSize = firstSegment.Length - stoppingSize;
                        searchSpace = searchSpace.Slice(skipSize);
                        offset += skipSize;
                        continue;
                    }

                    return offset + index;
                }
                else
                {
                    int tempSize = (int)Math.Min(searchSpace.Length, 2 * holdingSize);
                    searchSpace.Slice(0, tempSize).CopyTo(holdingSpace);

                    int index = holdingSpace.Slice(0, tempSize).IndexOf(xmlEndTag);
                    if (index == -1)
                    {
                        int skipSize = tempSize - stoppingSize;
                        searchSpace = searchSpace.Slice(skipSize);
                        offset += skipSize;
                        continue;
                    }

                    return offset + index;
                }
            }

            return -1;
        }

        private static async Task<SequencePosition> ProcessReplace(Utf8StringReplaceDirective[] directives, int holdingSize, LightweightBufferedPipeWriter destination, ReadOnlySequence<byte> searchSpace, bool isFinal, CancellationToken cancellationToken)
        {
            byte[] holdingSpace = ArrayPool<byte>.Shared.Rent(2 * holdingSize);
            try
            {
                int stoppingSize = isFinal ? 0 : holdingSize;
                while (searchSpace.Length > stoppingSize)
                {
                    ReadOnlyMemory<byte> firstSegment = searchSpace.First;
                    if (firstSegment.Length > holdingSize || (isFinal && searchSpace.IsSingleSegment))
                    {
                        (Utf8StringReplaceDirective? directive, int index) = FindStringToReplace(directives, firstSegment);
                        if (directive is null)
                        {
                            int copySize = firstSegment.Length - stoppingSize;
                            await destination.WriteAsync(firstSegment.Slice(0, copySize), cancellationToken).ConfigureAwait(false);
                            searchSpace = searchSpace.Slice(copySize);
                            continue;
                        }

                        if (index > 0)
                        {
                            await destination.WriteAsync(firstSegment.Slice(0, index), cancellationToken).ConfigureAwait(false);
                        }

                        await destination.WriteAsync(directive.NewString.AsMemory(), cancellationToken).ConfigureAwait(false);
                        searchSpace = searchSpace.Slice(index + directive.OriginalString.Length);
                    }
                    else
                    {
                        int tempSize = (int)Math.Min(searchSpace.Length, 2 * holdingSize);
                        searchSpace.Slice(0, tempSize).CopyTo(holdingSpace);

                        (Utf8StringReplaceDirective? directive, int index) = FindStringToReplace(directives, holdingSpace.AsMemory(0, tempSize));
                        if (directive is null)
                        {
                            int copySize = tempSize - stoppingSize;
                            await destination.WriteAsync(holdingSpace.AsMemory(0, copySize), cancellationToken).ConfigureAwait(false);
                            searchSpace = searchSpace.Slice(copySize);
                            continue;
                        }

                        if (index > 0)
                        {
                            await destination.WriteAsync(holdingSpace.AsMemory(0, index), cancellationToken).ConfigureAwait(false);
                        }

                        await destination.WriteAsync(directive.NewString.AsMemory(), cancellationToken).ConfigureAwait(false);
                        searchSpace = searchSpace.Slice(index + directive.OriginalString.Length);
                    }
                }

                return searchSpace.Start;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(holdingSpace);
            }
        }

        private static (Utf8StringReplaceDirective? Directive, int Index) FindStringToReplace(Utf8StringReplaceDirective[] directives, ReadOnlyMemory<byte> searchSpace)
        {
            Utf8StringReplaceDirective? directive = null;
            int index = 0;

            ReadOnlySpan<byte> span = searchSpace.Span;
            foreach (Utf8StringReplaceDirective item in directives)
            {
                int pos = span.IndexOf(item.OriginalString);
                if (pos >= 0)
                {
                    if (directive is null || pos < index)
                    {
                        directive = item;
                        index = pos;
                    }
                }
            }

            return (directive, index);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}

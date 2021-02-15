using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;

namespace Synfonia.Backend
{
    public class PartialHttpStream : Stream, IDisposable
{
    private const int CacheLen = 8192;

    // Cache for short requests.
    private readonly byte[] cache;
    private readonly int cacheLen;
    private Stream stream;
    private WebResponse response;
    private long position = 0;
    private long? length;
    private long cachePosition;
    private int cacheCount;
    private string bearerToken;

    public PartialHttpStream(string url, string bearerToken, int cacheLen = CacheLen)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("url empty");
        if (cacheLen <= 0)
            throw new ArgumentException("cacheLen must be greater than 0");

        Url = url;
        this.cacheLen = cacheLen;
        cache = new byte[cacheLen];
    }

    public string Url { get; private set; }

    public override bool CanRead { get { return true; } }
    public override bool CanWrite { get { return false; } }
    public override bool CanSeek { get { return true; } }

    public override long Position {get; set;}

    /// <summary>
    /// Lazy initialized length of the resource.
    /// </summary>
    public override long Length
    {
        get
        {
            if (length == null)
                length = HttpGetLength();
            return length.Value;
        }
    }

    /// <summary>
    /// Count of HTTP requests. Just for statistics reasons.
    /// </summary>
    public int HttpRequestsCount { get; private set; }

    public override void SetLength(long value)
    { throw new NotImplementedException(); }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));
        if (offset < 0 || offset >= buffer.Length)
            throw new ArgumentException(nameof(offset));
        if (count < 0 || offset + count > buffer.Length)
            throw new ArgumentException(nameof(count));

        long curPosition = Position;
        Position += ReadFromCache(buffer, ref offset, ref count);
        //if (Position > curPosition)
        //    Console.WriteLine($"Cache hit {Position - curPosition}");
        if (count > cacheLen)
        {
            // large request, do not cache
            Position += HttpRead(buffer, offset, count);
        }
        else if (count > 0)
        {
            // read to cache
            cachePosition = Position;
            cacheCount = HttpRead(cache, 0, cacheLen);
            Position += ReadFromCache(buffer, ref offset, ref count);
        }

        return (int)(Position - curPosition);
    }

    public override void Write(byte[] buffer, int offset, int count)
    { throw new NotImplementedException(); }

    public override long Seek(long pos, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.End:
                Position = Length + pos;
                break;

            case SeekOrigin.Begin:
                Position = pos;
                break;

            case SeekOrigin.Current:
                Position += pos;
                break;
        }
        return Position;
    }

    public override void Flush()
    {
    }

    private int ReadFromCache(byte[] buffer, ref int offset, ref int count)
    {
        if (cachePosition > Position || (cachePosition + cacheCount) <= Position)
            return 0; // cache miss
        int ccOffset = (int)(Position - cachePosition);
        int ccCount = Math.Min(cacheCount - ccOffset, count);
        Array.Copy(cache, ccOffset, buffer, offset, ccCount);
        offset += ccCount;
        count -= ccCount;
        return ccCount;
    }

    private int HttpRead(byte[] buffer, int offset, int count)
    {
        HttpRequestsCount++;
        HttpWebRequest req = HttpWebRequest.CreateHttp(Url);

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            req.Headers.Add("Authorization", "Bearer " + bearerToken);
        }

        req.AddRange(Position, Position + count - 1);
        try
        {
            response = req.GetResponse();
        }
        catch (WebException ex)
        {
            var response = (HttpWebResponse)ex.Response;
            if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                return 0;
            throw ex;
        }
        int nread;
        using (Stream stream = response.GetResponseStream())
            nread = stream.Read(buffer, offset, count);
        return nread;
    }

    private long HttpGetLength()
    {
        HttpRequestsCount++;
        HttpWebRequest request = HttpWebRequest.CreateHttp(Url);
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Add("Authorization", "Bearer " + bearerToken);
        }
        request.Method = "HEAD";
        return request.GetResponse().ContentLength;
    }

    private new void Dispose()
    {
        base.Dispose();
        if (stream != null)
        {
            stream.Dispose();
            stream = null;
        }
        if (response != null)
        {
            response.Dispose();
            response = null;
        }
    }
}
    
    public class ReadSeekableStream : Stream
    {
        private long _underlyingPosition;
        private readonly byte[] _seekBackBuffer;
        private int _seekBackBufferCount;
        private int _seekBackBufferIndex;
        private readonly Stream _underlyingStream;

        public ReadSeekableStream(Stream underlyingStream, int seekBackBufferSize)
        {
            if (!underlyingStream.CanRead)
                throw new Exception("Provided stream " + underlyingStream + " is not readable");
            _underlyingStream = underlyingStream;
            _seekBackBuffer = new byte[seekBackBufferSize];
            
            
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int copiedFromBackBufferCount = 0;
            if (_seekBackBufferIndex < _seekBackBufferCount)
            {
                copiedFromBackBufferCount = Math.Min(count, _seekBackBufferCount - _seekBackBufferIndex);
                Buffer.BlockCopy(_seekBackBuffer, _seekBackBufferIndex, buffer, offset, copiedFromBackBufferCount);
                offset += copiedFromBackBufferCount;
                count -= copiedFromBackBufferCount;
                _seekBackBufferIndex += copiedFromBackBufferCount;
            }

            int bytesReadFromUnderlying = 0;
            if (count > 0)
            {
                bytesReadFromUnderlying = _underlyingStream.Read(buffer, offset, count);
                if (bytesReadFromUnderlying > 0)
                {
                    _underlyingPosition += bytesReadFromUnderlying;

                    var copyToBufferCount = Math.Min(bytesReadFromUnderlying, _seekBackBuffer.Length);
                    var copyToBufferOffset = Math.Min(_seekBackBufferCount, _seekBackBuffer.Length - copyToBufferCount);
                    var bufferBytesToMove = Math.Min(_seekBackBufferCount - 1, copyToBufferOffset);

                    if (bufferBytesToMove > 0)
                        Buffer.BlockCopy(_seekBackBuffer, _seekBackBufferCount - bufferBytesToMove, _seekBackBuffer, 0,
                            bufferBytesToMove);
                    Buffer.BlockCopy(buffer, offset, _seekBackBuffer, copyToBufferOffset, copyToBufferCount);
                    _seekBackBufferCount = Math.Min(_seekBackBuffer.Length, _seekBackBufferCount + copyToBufferCount);
                    _seekBackBufferIndex = _seekBackBufferCount;
                }
            }

            return copiedFromBackBufferCount + bytesReadFromUnderlying;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.End)
                return SeekFromEnd((int) Math.Max(0, -offset));

            var relativeOffset = origin == SeekOrigin.Current
                ? offset
                : offset - Position;

            if (relativeOffset == 0)
                return Position;
            else if (relativeOffset > 0)
                return SeekForward(relativeOffset);
            else
                return SeekBackwards(-relativeOffset);
        }

        private long SeekForward(long origOffset)
        {
            long offset = origOffset;
            var seekBackBufferLength = _seekBackBuffer.Length;

            int backwardSoughtBytes = _seekBackBufferCount - _seekBackBufferIndex;
            int seekForwardInBackBuffer = (int) Math.Min(offset, backwardSoughtBytes);
            offset -= seekForwardInBackBuffer;
            _seekBackBufferIndex += seekForwardInBackBuffer;

            if (offset > 0)
            {
                // first completely fill seekBackBuffer to remove special cases from while loop below
                if (_seekBackBufferCount < seekBackBufferLength)
                {
                    var maxRead = seekBackBufferLength - _seekBackBufferCount;
                    if (offset < maxRead)
                        maxRead = (int) offset;
                    var bytesRead = _underlyingStream.Read(_seekBackBuffer, _seekBackBufferCount, maxRead);
                    _underlyingPosition += bytesRead;
                    _seekBackBufferCount += bytesRead;
                    _seekBackBufferIndex = _seekBackBufferCount;
                    if (bytesRead < maxRead)
                    {
                        if (_seekBackBufferCount < offset)
                            throw new NotSupportedException("Reached end of stream seeking forward " + origOffset +
                                                            " bytes");
                        return Position;
                    }

                    offset -= bytesRead;
                }

                // now alternate between filling tempBuffer and seekBackBuffer
                bool fillTempBuffer = true;
                var tempBuffer = new byte[seekBackBufferLength];
                while (offset > 0)
                {
                    var maxRead = offset < seekBackBufferLength ? (int) offset : seekBackBufferLength;
                    var bytesRead = _underlyingStream.Read(fillTempBuffer ? tempBuffer : _seekBackBuffer, 0, maxRead);
                    _underlyingPosition += bytesRead;
                    var bytesReadDiff = maxRead - bytesRead;
                    offset -= bytesRead;
                    if (bytesReadDiff > 0 /* reached end-of-stream */ || offset == 0)
                    {
                        if (fillTempBuffer)
                        {
                            if (bytesRead > 0)
                            {
                                Buffer.BlockCopy(_seekBackBuffer, bytesRead, _seekBackBuffer, 0, bytesReadDiff);
                                Buffer.BlockCopy(tempBuffer, 0, _seekBackBuffer, bytesReadDiff, bytesRead);
                            }
                        }
                        else
                        {
                            if (bytesRead > 0)
                                Buffer.BlockCopy(_seekBackBuffer, 0, _seekBackBuffer, bytesReadDiff, bytesRead);
                            Buffer.BlockCopy(tempBuffer, bytesRead, _seekBackBuffer, 0, bytesReadDiff);
                        }

                        if (offset > 0)
                            throw new NotSupportedException("Reached end of stream seeking forward " + origOffset +
                                                            " bytes");
                    }

                    fillTempBuffer = !fillTempBuffer;
                }
            }

            return Position;
        }

        private long SeekBackwards(long offset)
        {
            var intOffset = (int) offset;
            if (offset > int.MaxValue || intOffset > _seekBackBufferIndex)
                throw new NotSupportedException("Cannot currently seek backwards more than " + _seekBackBufferIndex +
                                                " bytes");
            _seekBackBufferIndex -= intOffset;
            return Position;
        }

        private long SeekFromEnd(long offset)
        {
            var intOffset = (int) offset;
            var seekBackBufferLength = _seekBackBuffer.Length;
            if (offset > int.MaxValue || intOffset > seekBackBufferLength)
                throw new NotSupportedException("Cannot seek backwards from end more than " + seekBackBufferLength +
                                                " bytes");

            // first completely fill seekBackBuffer to remove special cases from while loop below
            if (_seekBackBufferCount < seekBackBufferLength)
            {
                var maxRead = seekBackBufferLength - _seekBackBufferCount;
                var bytesRead = _underlyingStream.Read(_seekBackBuffer, _seekBackBufferCount, maxRead);
                _underlyingPosition += bytesRead;
                _seekBackBufferCount += bytesRead;
                _seekBackBufferIndex = Math.Max(0, _seekBackBufferCount - intOffset);
                if (bytesRead < maxRead)
                {
                    if (_seekBackBufferCount < intOffset)
                        throw new NotSupportedException("Could not seek backwards from end " + intOffset + " bytes");
                    return Position;
                }
            }
            else
            {
                _seekBackBufferIndex = _seekBackBufferCount;
            }

            // now alternate between filling tempBuffer and seekBackBuffer
            bool fillTempBuffer = true;
            var tempBuffer = new byte[seekBackBufferLength];
            while (true)
            {
                var bytesRead = _underlyingStream.Read(fillTempBuffer ? tempBuffer : _seekBackBuffer, 0,
                    seekBackBufferLength);
                _underlyingPosition += bytesRead;
                var bytesReadDiff = seekBackBufferLength - bytesRead;
                if (bytesReadDiff > 0) // reached end-of-stream
                {
                    if (fillTempBuffer)
                    {
                        if (bytesRead > 0)
                        {
                            Buffer.BlockCopy(_seekBackBuffer, bytesRead, _seekBackBuffer, 0, bytesReadDiff);
                            Buffer.BlockCopy(tempBuffer, 0, _seekBackBuffer, bytesReadDiff, bytesRead);
                        }
                    }
                    else
                    {
                        if (bytesRead > 0)
                            Buffer.BlockCopy(_seekBackBuffer, 0, _seekBackBuffer, bytesReadDiff, bytesRead);
                        Buffer.BlockCopy(tempBuffer, bytesRead, _seekBackBuffer, 0, bytesReadDiff);
                    }

                    _seekBackBufferIndex -= intOffset;
                    return Position;
                }

                fillTempBuffer = !fillTempBuffer;
            }
        }

        public override long Position
        {
            get { return _underlyingPosition - (_seekBackBufferCount - _seekBackBufferIndex); }
            set { Seek(value, SeekOrigin.Begin); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _underlyingStream.Close();
            base.Dispose(disposing);
        }

        public override bool CanTimeout
        {
            get { return _underlyingStream.CanTimeout; }
        }

        public override bool CanWrite
        {
            get { return _underlyingStream.CanWrite; }
        }

        public override long Length
        {
            get { return _seekBackBuffer.Length; }
        }

        public override void SetLength(long value)
        {
            _underlyingStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _underlyingStream.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            _underlyingStream.Flush();
        }
    }
}
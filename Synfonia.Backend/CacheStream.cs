using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Espresso3389.HttpStream
{
    public delegate void DispatcherInvoker(Action action);

    /// <summary>
    /// A <see cref="Stream"/> implementation, which provides caching mechanism for slow streams.
    /// </summary>
    public abstract class CacheStream : Stream
    {
        /// <summary>
        /// Creates a new <see cref="CacheStream"/> object.
        /// </summary>
        /// <param name="cacheStream"><see cref="Stream"/> to cache the file.</param>
        /// <param name="ownCacheStream"><c>true</c> to instruct the object to close <paramref name="cacheStream"/> on its cleanup.</param>
        /// <param name="cachePageSize">Cache page size. The default is 32K.</param>
        public CacheStream(Stream cacheStream, bool ownCacheStream, int cachePageSize = 32 * 1024)
            : this(cacheStream, ownCacheStream, cachePageSize, null, null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CacheStream"/> object.
        /// </summary>
        /// <param name="cacheStream"><see cref="Stream"/> to cache the file.</param>
        /// <param name="ownCacheStream"><c>true</c> to instruct the object to close <paramref name="cacheStream"/> on its cleanup.</param>
        /// <param name="cachePageSize">Cache page size.</param>
        /// <param name="cached">Cached flags for the pages in packed bits if any; otherwise it can be <c>null</c>.</param>
        /// <param name="dispatcherInvoker">Function called on every call to synchronous <see cref="CacheStream.Read(byte[], int, int)"/> call to invoke <see cref="CacheStream.ReadAsync(byte[], int, int, CancellationToken)"/>.</param>
        public CacheStream(Stream cacheStream, bool ownCacheStream, int cachePageSize, byte[] cached, DispatcherInvoker dispatcherInvoker)
        {
            if (cacheStream == null)
            {
                cacheStream = new MemoryStream();
                ownCacheStream = true;
            }

            if (!cacheStream.CanSeek || !cacheStream.CanRead || !cacheStream.CanWrite)
                throw new ArgumentException("cacheStream should be seekable/readable/writeable.", "cacheStream");

            _cacheStream = cacheStream;
            _ownStream = ownCacheStream;
            _cached = cached;
            _cachePageSize = cachePageSize;
            DispatcherInvoker = dispatcherInvoker;
            if (dispatcherInvoker != null)
                _event = new ManualResetEventSlim();
        }

        Stream _cacheStream;
        bool _ownStream;
        byte[] _cached;
        int _cachePageSize;
        bool _isFullyCached;
        ManualResetEventSlim _event;

        protected DispatcherInvoker DispatcherInvoker { get; }

        protected ConfiguredTaskAwaitable<T> WrapTask<T>(Task<T> task) => task.ConfigureAwait(DispatcherInvoker != null);

        protected T Wait<T>(Func<Task<T>> func)
        {
            if (DispatcherInvoker == null)
            {
                return func().Result;
            }

            T ret = default(T);
            Exception ex = null;

            _event.Reset();
            DispatcherInvoker(async () =>
            {
                try
                {
                    ret = await func();
                }
                catch (Exception e)
                {
                    ex = e;
                }
                finally
                {
                    _event.Set();
                }
            });
            _event.Wait();

            if (ex != null)
                throw ex;
            return ret;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_ownStream && _cacheStream != null)
                {
                    _cacheStream.Dispose();
                    _cacheStream = null;
                }
                if (_event != null)
                {
                    _event.Dispose();
                    _event = null;
                }
            }
        }

        #region implemented abstract members of Stream

        public override void Flush()
        {
            // Nothing to do; we only supports read accesses.
        }

        public override int Read(byte[] buffer, int offset, int count) => Wait(() => ReadAsync(buffer, offset, count));

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }
            return Position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => Wait(() => GetLengthAsync());

        public async Task<long> GetLengthAsync()
        {
            var length = GetStreamLengthOrDefault(long.MaxValue);
            if (length == long.MaxValue)
            {
                await WrapTask(readAsync(0L, new byte[1], 0, 1, CancellationToken.None));
                length = GetStreamLengthOrDefault(long.MaxValue);
            }
            return length;
        }

        public override long Position { get; set; }

        #endregion

        /// <summary>
        /// Whether the file is fully cached to the cache stream or not.
        /// </summary>
        public bool IsFullyCached
        {
            get
            {
                if (_isFullyCached)
                    return true;
                if (_cached == null)
                    return false;

                var pages = getNumberOfPages();

                var len = (pages + 7) / 8;
                if (_cached.Length < len)
                    return false;

                var last = pages / 8;
                var fract = pages & 7;
                for (var i = 0; i < last; i++)
                    if (_cached[i] != 0xff)
                        return false;

                if (fract == 0)
                {
                    _isFullyCached = true;
                    return true;
                }

                if (_cached[last] == ((0xff00 >> fract) & 0xff))
                {
                    _isFullyCached = true;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// The ratio of the file is actually cached. 1.0 if the file is fully cached.
        /// </summary>
        public double CachedRatio
        {
            get
            {
                if (_isFullyCached)
                    return 1.0;
                if (_cached == null)
                    return 0.0;

                int count = 0;
                for (var i = 0; i < _cached.Length; i++)
                    count += BITS_COUNT_TABLE[_cached[i]];

                return (double)count / getNumberOfPages();
            }
        }

        static int[] BITS_COUNT_TABLE = {
            0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4,
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
            4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 7, 6, 7, 7, 8,
        };

        int getNumberOfPages()
        {
            var size = GetStreamLengthOrDefault(long.MaxValue);
            var pagesLong = (size + _cachePageSize - 1) / _cachePageSize;

            // PageSize=32K case, the maximum is 128TB (= 4GB * 32K).
            if (pagesLong > int.MaxValue)
                throw new InvalidDataException("The file size is too large.");

            return(int)pagesLong;
        }

        bool isPageCached(int page)
        {
            if (_isFullyCached)
                return true;
            if (_cached == null)
                return false;

            var i = page / 8;
            var mask = 0x80 >> (page & 7);

            if (i >= _cached.Length)
                return false;
            return (_cached[i] & mask) == mask;
        }

        void setPageCached(int page)
        {
            var i = page / 8;
            var mask = (byte)(0x80 >> (page & 7));

            if (_cached == null || i >= _cached.Length)
                Array.Resize(ref _cached, i + 1);

            _cached[i] |= mask;
        }

        /// <summary>
        /// Get the length of the stream.
        /// </summary>
        /// <param name="defValue">The value to be returned if the actual length is not available.</param>
        /// <returns>The length of the stream.</returns>
        public abstract long GetStreamLengthOrDefault(long defValue);

        /// <summary>
        /// Whether the stream length is available or not.
        /// </summary>
        public abstract bool IsStreamLengthAvailable { get; protected set; }

        /// <summary>
        /// Load a portion of file and write to a stream.
        /// </summary>
        /// <param name="stream">Stream to write on.</param>
        /// <param name="offset">The offset of the data to download.</param>
        /// <param name="length">The length of the data to download.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The byte range actually loaded. It may be larger than the requested range.</returns>
        protected abstract Task<int> LoadAsync(Stream stream, int offset, int length, CancellationToken cancellationToken);

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = await readAsync(Position, buffer, offset, count, cancellationToken);
            Position += bytesRead;
            return bytesRead;
        }

        async Task<int> readAsync(long position, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count == 0)
                return 0;

            int bytesRead = 0;
            var pos = position;
            var end = pos + count;
            var fileSize = GetStreamLengthOrDefault(long.MaxValue);
            if (end > fileSize)
                end = fileSize;

            var firstPage = (int)(pos / _cachePageSize);
            var lastPage = (int)((end - 1) / _cachePageSize);

            for (int i = firstPage; i <= lastPage;)
            {
                int bytes2Read, pagesRead;
                var pageOffset = (long)i * _cachePageSize;

                if (!isPageCached(i))
                {
                    var pagesNotCached = lastPage - i + 1;
                    for (var j = i + 1; j <= lastPage; j++)
                    {
                        if (isPageCached(j))
                        {
                            pagesNotCached = j - i;
                            break;
                        }
                    }

                    var offsetToLoad = i * _cachePageSize;
                    var sizeToLoad = pagesNotCached * _cachePageSize;

                    var sizeLoaded = await WrapTask(LoadAsync(_cacheStream, offsetToLoad, sizeToLoad, cancellationToken));
                    if (fileSize < long.MaxValue && offsetToLoad + sizeToLoad <= fileSize && sizeLoaded < sizeToLoad)
                    {
                        // the block tried to read is a part of the actual file (we know the file size!)
                        // but we cannot read the block :(
                        throw new IOException(
                            $"Could not read all of the requested bytes: offset={offsetToLoad}, filesize={fileSize}, remain={fileSize - offsetToLoad}, requested={sizeToLoad}, read={sizeLoaded}");
                    }

                    for (var j = 0; j < pagesNotCached; j++)
                        setPageCached(i + j);

                    var e = Math.Min(pageOffset + sizeLoaded, end);
                    bytes2Read = (int)(e - pos);
                    pagesRead = pagesNotCached;
                }
                else
                {
                    var e = Math.Min(pageOffset + _cachePageSize, end);
                    bytes2Read = (int)(e - pos);
                    pagesRead = 1;
                }

                _cacheStream.Position = pos;
                var bytes = await WrapTask(_cacheStream.ReadAsync(buffer, offset, bytes2Read, cancellationToken));
                if (fileSize < long.MaxValue && pos + bytes2Read <= fileSize && bytes < bytes2Read)
                {
                    // the block tried to read is a part of the actual file (we know the file size!)
                    // but we cannot read the block :(
                    throw new IOException(
                        $"Could not read all of the requested bytes from the cache: offset={pos}, filesize={fileSize}, remain={fileSize - pos}, requested={bytes2Read}, read={bytes}");
                }

                pos += bytes2Read;
                offset += bytes2Read;
                bytesRead += bytes2Read;
                i += pagesRead;
            }
            return bytesRead;
        }

        public override Task FlushAsync(System.Threading.CancellationToken cancellationToken) => throw new NotSupportedException();

        public override Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}


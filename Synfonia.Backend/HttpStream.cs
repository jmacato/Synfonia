using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace Espresso3389.HttpStream
{
    /// <summary>
    /// Implements randomly accessible <see cref="Stream"/> on HTTP 1.1 transport.
    /// </summary>
    public class HttpStream : CacheStream
    {
        Uri _uri;
        HttpClient _httpClient;
        bool _ownHttpClient;
        int _bufferingSize;

        /// <summary>
        /// Size in bytes of the file data downloaded so far if available; otherwise it returns <see cref="long.MaxValue"/>.
        /// <seealso cref="FileSizeAvailable"/>
        /// <seealso cref="GetStreamLengthOrDefault"/>
        /// </summary>
        public long StreamLength { get; private set; }

        /// <summary>
        /// Whether file properties, like file size and last modified time is correctly inspected.
        /// </summary>
        public bool InspectionFinished { get; private set; }
        /// <summary>
        /// When the file is last modified.
        /// </summary>
        public DateTime LastModified { get; private set; }
        /// <summary>
        /// Content type of the file.
        /// </summary>
        public string ContentType { get; private set; }
        /// <summary>
        /// Buffering size for downloading the file.
        /// </summary>
        public int BufferingSize
        {
            get => _bufferingSize;
            set
            {
                if (value == 0 || bitCount(value) != 1)
                    throw new ArgumentOutOfRangeException("BufferingSize should be 2^n.");
                _bufferingSize = value;
            }
        }

        static int bitCount(int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        /// <summary>
        /// Creates a new HttpStream with the specified URI.
        /// The file will be cached on memory.
        /// </summary>
        /// <param name="uri">URI of the file to download.</param>
        public HttpStream(Uri uri) : this(uri, new MemoryStream(), true)
        {
        }

        /// <summary>
        /// Default cache page size; 32KB.
        /// </summary>
        const int DefaultCachePageSize = 32 * 1024;

        /// <summary>
        /// Creates a new HttpStream with the specified URI.
        /// </summary>
        /// <param name="uri">URI of the file to download.</param>
        /// <param name="cache">Stream, on which the file will be cached. It should be seekable, readable and writeable.</param>
        /// <param name="ownStream"><c>true</c> to dispose <paramref name="cache"/> on HttpStream's cleanup.</param>
        public HttpStream(Uri uri, Stream cache, bool ownStream) : this(uri, cache, ownStream, DefaultCachePageSize, null)
        {
        }

        /// <summary>
        /// Creates a new HttpStream with the specified URI.
        /// </summary>
        /// <param name="uri">URI of the file to download.</param>
        /// <param name="cache">Stream, on which the file will be cached. It should be seekable, readable and writeable.</param>
        /// <param name="ownStream"><c>true</c> to dispose <paramref name="cache"/> on HttpStream's cleanup.</param>
        public HttpStream(Uri uri, Stream cache, bool ownStream, int cachePageSize, byte[] cached)
            : this(uri, cache, ownStream, cachePageSize, cached, null)
        {
        }

        /// <summary>
        /// Creates a new HttpStream with the specified URI.
        /// </summary>
        /// <param name="uri">URI of the file to download.</param>
        /// <param name="cache">Stream, on which the file will be cached. It should be seekable, readable and writeable.</param>
        /// <param name="ownStream"><c>true</c> to dispose <paramref name="cache"/> on HttpStream's cleanup.</param>
        /// <param name="cachePageSize">Cache page size.</param>
        /// <param name="cached">Cached flags for the pages in packed bits if any; otherwise it can be <c>null</c>.</param>
        /// <param name="httpClient"><see cref="HttpClient"/> to use on creating HTTP requests.</param>
        public HttpStream(Uri uri, Stream cache, bool ownStream, int cachePageSize, byte[] cached, HttpClient httpClient)
            : base(cache, ownStream, cachePageSize, cached, null)
        {
            StreamLength = long.MaxValue;
            _uri = uri;
            _httpClient = httpClient;
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
                _ownHttpClient = true;
            }
            BufferingSize = cachePageSize;
        }

        /// <summary>
        /// Creates a new HttpStream with the specified URI.
        /// </summary>
        /// <param name="uri">URI of the file to download.</param>
        /// <param name="cache">Stream, on which the file will be cached. It should be seekable, readable and writeable.</param>
        /// <param name="ownStream"><c>true</c> to dispose <paramref name="cache"/> on HttpStream's cleanup.</param>
        /// <param name="cachePageSize">Cache page size.</param>
        /// <param name="cached">Cached flags for the pages in packed bits if any; otherwise it can be <c>null</c>.</param>
        /// <param name="httpClient"><see cref="HttpClient"/> to use on creating HTTP requests.</param>
        /// <param name="dispatcherInvoker">Function called on every call to synchronous <see cref="HttpStream.Read(byte[], int, int)"/> call to invoke <see cref="HttpStream.ReadAsync(byte[], int, int, CancellationToken)"/>.</param>
        public HttpStream(Uri uri, Stream cache, bool ownStream, int cachePageSize, byte[] cached, HttpClient httpClient, DispatcherInvoker dispatcherInvoker)
            : base(cache, ownStream, cachePageSize, cached, dispatcherInvoker)
        {
            StreamLength = long.MaxValue;
            _uri = uri;
            _httpClient = httpClient;
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
                _ownHttpClient = true;
            }
            BufferingSize = cachePageSize;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && _ownHttpClient)
            {
                _httpClient.Dispose();
            }
        }

        /// <summary>
        /// Size in bytes of the file downloaing if available.
        /// </summary>
        /// <param name="defValue">If the file is not available, the value is returned.</param>
        /// <seealso cref="StreamLength"/>
        /// <seealso cref="FileSizeAvailable"/>
        /// <returns>The file size.</returns>
        public override long GetStreamLengthOrDefault(long defValue) => IsStreamLengthAvailable ? StreamLength : defValue;

        /// <summary>
        /// Determine whether stream length is determined or not.
        /// </summary>
        public override bool IsStreamLengthAvailable { get; protected set; }

        /// <summary>
        /// Last HTTP status code.
        /// </summary>
        public System.Net.HttpStatusCode LastHttpStatusCode { get; private set; }

        /// <summary>
        /// Last reason phrase obtained with <see cref="LastHttpStatusCode"/>.
        /// </summary>
        public string LastReasonPhrase { get; private set; }

        /// <summary>
        /// Download a portion of file and write to a stream.
        /// </summary>
        /// <param name="stream">Stream to write on.</param>
        /// <param name="offset">The offset of the data to download.</param>
        /// <param name="length">The length of the data to download.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The byte range actually downloaded. It may be larger than the requested range.</returns>
        protected override async Task<int> LoadAsync(Stream stream, int offset, int length, CancellationToken cancellationToken)
        {
            if (length == 0)
                return 0;

            long endPos = offset + length;
            if (IsStreamLengthAvailable && endPos > StreamLength)
                endPos = StreamLength;

            var req = new HttpRequestMessage(HttpMethod.Get, _uri);
            // Use "Range" header to sepcify the data offset and size
            req.Headers.Add("Range", $"bytes={offset}-{endPos - 1}");

            // post the request
            var res = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            LastHttpStatusCode = res.StatusCode;
            LastReasonPhrase = res.ReasonPhrase;
            if (!res.IsSuccessStatusCode)
                throw new Exception($"HTTP Status: {res.StatusCode} for bytes={offset}-{endPos - 1}");

            // retrieve the resulting Content-Range
            bool getRanges = true;
            long begin = 0, end = long.MaxValue;
            long size = long.MaxValue;
            if (!actionIfFound(res, "Content-Range", range =>
            {
                // 206
                var m = Regex.Match(range, @"bytes\s+([0-9]+)-([0-9]+)/(\w+)");
                begin = long.Parse(m.Groups[1].Value);
                end = long.Parse(m.Groups[2].Value);
                size = end - begin + 1;

                if (!IsStreamLengthAvailable)
                {
                    var sz = m.Groups[3].Value;
                    if (sz != "*")
                    {
                        StreamLength = long.Parse(sz);
                        IsStreamLengthAvailable = true;
                    }
                }
            }))
            {
                // In some case, there's no Content-Range but Content-Length
                // instead.
                getRanges = false;
                begin = 0;
                actionIfFound(res, "Content-Length", v =>
                {
                    StreamLength = end = size = long.Parse(v);
                    IsStreamLengthAvailable = true;
                });
            }

            actionIfFound(res, "Content-Type", v =>
            {
                ContentType = v;
            });

            actionIfFound(res, "Last-Modified", v =>
            {
                LastModified = parseDateTime(v);
            });

            InspectionFinished = true;

            var s = await res.Content.ReadAsStreamAsync().ConfigureAwait(false);

            int size32 = (int)size;
            stream.Position = begin;
            var buf = new byte[BufferingSize];
            var copied = 0;
            while (size32 > 0)
            {
                var bytes2Read = Math.Min(size32, BufferingSize);
                var bytesRead = await s.ReadAsync(buf, 0, bytes2Read, cancellationToken).ConfigureAwait(false);
                if (bytesRead <= 0)
                    break;

                await stream.WriteAsync(buf, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                size32 -= bytesRead;
                copied += bytesRead;
            }

            if (!IsStreamLengthAvailable && !getRanges)
            {
                StreamLength = copied;
                IsStreamLengthAvailable = true;
            }

            if (RangeDownloaded != null)
                RangeDownloaded(this, new RangeDownloadedEventArgs { Offset = begin, Length = copied });
            return copied;
        }

        bool actionIfFound(HttpResponseMessage res, string name, Action<string> action)
        {
            IEnumerable<string> strs;
            if (res.Content.Headers.TryGetValues(name, out strs))
            {
                action(strs.First());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Invoked when a new range is downloaded.
        /// </summary>
        public event EventHandler<RangeDownloadedEventArgs> RangeDownloaded;

        static DateTime parseDateTime(string dateTime)
        {
            if (dateTime.EndsWith(" UTC"))
            {
                return DateTime.ParseExact(dateTime,
                    "ddd, dd MMM yyyy HH:mm:ss 'UTC'",
                    CultureInfo.InvariantCulture.DateTimeFormat,
                    DateTimeStyles.AssumeUniversal);
            }
            return DateTime.ParseExact(dateTime,
                "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                CultureInfo.InvariantCulture.DateTimeFormat,
                DateTimeStyles.AssumeUniversal);
        }
    }

    /// <summary>
    /// Used by <see cref="HttpStream.RangeDownloaded"/> event.
    /// </summary>
    public class RangeDownloadedEventArgs : EventArgs
    {
        /// <summary>
        /// The offset of the data downloaded.
        /// </summary>
        public long Offset { get; set; }
        /// <summary>
        /// The length of the data downloaded.
        /// </summary>
        public long Length { get; set; }
    }
}


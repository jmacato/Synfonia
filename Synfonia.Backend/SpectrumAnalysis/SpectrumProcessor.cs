using System;
using System.Numerics;
using System.Threading;
using SharpAudio.Codec;

namespace Synfonia.Backend.SpectrumAnalysis
{
    public class SpectrumProcessor : ISoundSinkReceiver
    {
        private const double ShortDivisor = short.MaxValue;
        private const double MinDbValue = -90;
        private const double MaxDbValue = 0;
        private const double DbScale = MaxDbValue - MinDbValue;
        private readonly int _binaryExp;

        private readonly int _fftLength = 256;
        private readonly object _latesSampleLock = new object();
        private readonly TimeSpan _sampleWait = TimeSpan.FromMilliseconds(20);
        private readonly int _totalCh = 2;
        private bool _hasSpectrumData;
        private volatile bool _isDisposed;
        private byte[] _latestSample;

        public SpectrumProcessor()
        {
            _binaryExp = (int)Math.Log(_fftLength, 2.0);

            var spectrumThread = new Thread(SpectrumLoop);
            spectrumThread.Start();
        }

        public void Dispose()
        {
            FftDataReady = null;
            _isDisposed = true;
        }

        public void Receive(byte[] data)
        {

            lock (_latesSampleLock)
            {
                _latestSample = data;
                _hasSpectrumData = true;
            }
            
        }

        public event EventHandler<double[,]> FftDataReady;

        private double[,] Fft2Double(Complex[,] fftResults, int ch, int fftLength)
        {
            // Only return the N/2 bins since that's the nyquist limit.
            var n = fftLength / 2;
            var processedFft = new double[ch, n];

            for (var c = 0; c < ch; c++)
            for (var i = 0; i < n; i++)
            {
                var complex = fftResults[c, i];

                var magnitude = complex.Magnitude;
                if (Math.Abs(magnitude) < double.Epsilon) continue;

                // decibel
                var result = (20 * Math.Log10(magnitude) - MinDbValue) / DbScale * 1;

                processedFft[c, i] = Math.Max(0, result);
            }

            return processedFft;
        }
 
        private void SpectrumLoop()
        {
            // Assuming 16 bit PCM, Little-endian, 2 Channels.
            var specSamples = _fftLength * _totalCh * sizeof(short);
            var curChByteRaw = 0;
            var tempBuf = new byte[specSamples];
            var samplesDouble = new double[_totalCh, _fftLength];
            var channelCounters = new int[_totalCh];
            var complexSamples = new Complex[_totalCh, _fftLength];
            var cachedWindowVal = new double[_fftLength];

            for (var i = 0; i < _fftLength; i++) cachedWindowVal[i] = FastFourierTransform.HammingWindow(i, _fftLength);

            while (!_isDisposed)
            {
                Thread.Sleep(_sampleWait);

                if (FftDataReady is null) continue;

                var gotData = false;

                lock (_latesSampleLock)
                {
                    if (_hasSpectrumData)
                    {
                        _hasSpectrumData = false;

                        if (_latestSample.Length < tempBuf.Length)
                        {
                            Array.Clear(tempBuf, 0, tempBuf.Length);
                            Buffer.BlockCopy(_latestSample, 0, tempBuf, 0, _latestSample.Length);
                        }
                        else
                        {
                            tempBuf = _latestSample;
                        }

                        gotData = true;
                    }
                }

                if (!gotData) continue;

                var rawSamplesShort = tempBuf.AsMemory().AsShorts().Slice(0, _fftLength * _totalCh);

                // Channel de-interleaving
                for (var i = 0; i < rawSamplesShort.Length; i++)
                {
                    samplesDouble[curChByteRaw, channelCounters[curChByteRaw]] = rawSamplesShort.Span[i] / ShortDivisor;
                    channelCounters[curChByteRaw]++;
                    curChByteRaw++;
                    curChByteRaw %= _totalCh;
                }

                Array.Clear(channelCounters, 0, channelCounters.Length);

                // Process FFT for each channel.
                for (var curCh = 0; curCh < _totalCh; curCh++)
                {
                    for (var i = 0; i < _fftLength; i++)
                    {
                        var windowed_sample = samplesDouble[curCh, i] * cachedWindowVal[i];
                        complexSamples[curCh, i] = new Complex(windowed_sample, 0);
                    }

                    FastFourierTransform.ProcessFFT(true, _binaryExp, complexSamples, curCh);
                }

                FftDataReady?.Invoke(this, Fft2Double(complexSamples, _totalCh, _fftLength));

                Array.Clear(samplesDouble, 0, samplesDouble.Length);
            }
        }
        
    }
}
using System.Security.Cryptography.X509Certificates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using SharpAudio;
using SharpAudio.Codec;
using Id3;
using Avalonia.Media.Imaging;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Collections.ObjectModel;

namespace Symphony.ViewModels
{
    public static class TagExtensions
    {
        public static IBitmap LoadAlbumCover(this Id3Tag tag)
        {
            var cover = tag.Pictures.FirstOrDefault(x => x.PictureType == Id3.Frames.PictureType.FrontCover);

            if (cover != null)
            {
                using (var ms = new MemoryStream(cover.PictureData))
                {
                    try
                    {
                        return new Bitmap(ms);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }

            return null;
        }

    }


    public class Track
    {
        public string Title { get; set; }

        public Album Album { get; set; }

        public string Path { get; set; }
    }

    public class Album : IComparable<Album>
    {
        public Album()
        {
            Tracks = new List<Track>();
        }

        public string Title { get; set; }

        public List<Track> Tracks { get; set; }

        public IBitmap Bitmap { get; set; }

        public int CompareTo([AllowNull] Album other)
        {
            return Title.CompareTo(other.Title);
        }
    }

    public class MainWindowViewModel : ViewModelBase
    {
        private TrackStatusViewModel _trackStatus;
        private Dictionary<string, Album> _albums;
        private ObservableCollection<IBitmap> _albumCovers;

        public ObservableCollection<IBitmap> AlbumCovers
        {
            get { return _albumCovers; }
            set { this.RaiseAndSetIfChanged(ref _albumCovers, value); }
        }

        public MainWindowViewModel()
        {
            _albums = new Dictionary<string, Album>();
            TrackStatus = new TrackStatusViewModel();
            AlbumCovers = new ObservableCollection<IBitmap>();

            this._audioEngine = AudioEngine.CreateDefault();

            if (_audioEngine == null)
            {
                throw new Exception("Failed to create an audio backend!");
            }

            this.WhenAnyValue(x => x.TargetFile)
                 .DistinctUntilChanged()
                .Subscribe(x => this.TargetFile = x);

            //TargetFile = @"C:\Users\danwa\OneDrive\Music\Music\Elton John\Greatest Hits (1970-2002) [3CD]\01 your song.mp3";
            TargetFile = @"C:\Users\danwa\OneDrive\Music\Music\Arctic Monkeys\Arctic Monkeys  Essentials\1-15 Black Treacle.mp3";

            PlayCommand = ReactiveCommand.CreateFromTask(DoPlay);

            ScanMusicFolder(@"C:\Users\danwa\OneDrive\Music\Music\");
        }

        public ReactiveCommand<Unit, Unit> PlayCommand { get; }

        public async Task ScanMusicFolder(string path)
        {
            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                await ScanMusicFolder(directory);
            }

            foreach (var file in Directory.EnumerateFiles(path, "*.mp3"))
            {
                Console.WriteLine($"Processing file: {file}");

                using (var mp3 = new Mp3(file))
                {
                    var tag = mp3.GetTag(Id3TagFamily.Version2X);

                    if (tag is null)
                    {
                        continue;
                    }

                    if (!_albums.ContainsKey(tag.Album))
                    {
                        var album = new Album();

                        album.Title = tag.Album;
                        album.Tracks.Add(new Track
                        {
                            Album = album,
                            Path = file,
                            Title = tag.Title
                        });

                        album.Bitmap = tag.LoadAlbumCover();

                        _albums[tag.Album] = album;

                        AlbumCovers.Add(album.Bitmap);
                    }
                }
            }
        }

        public async Task DoPlay()
        {
            _soundStream = new SoundStream(File.OpenRead(TargetFile), _audioEngine);

            TrackStatus.LoadTrack(_soundStream, TargetFile);

            _soundStream.Play();
        }

        public void SliderChangedManually(double value)
        {
            if (_soundStream is null) return;
            if (!_soundStream.IsPlaying) return;

            var x = ValidValuesOnly(value);
            //var z = TimeSpan.FromSeconds(x * Duration.TotalSeconds);
            //_soundStream.TrySeek(z);
        }

        private string _targetFile;

        public string TargetFile
        {
            get => _targetFile;
            set => this.RaiseAndSetIfChanged(ref _targetFile, value, nameof(TargetFile));
        }

        public bool SliderClicked
        {
            get => _sliderClicked;
            set => this.RaiseAndSetIfChanged(ref _sliderClicked, value, nameof(SliderClicked));
        }

        private AudioEngine _audioEngine;
        private SoundStream _soundStream;

        public double SeekPosition
        {
            get => _seekPosition;
            set { SliderChangedManually(value); }
        }

        private double ValidValuesOnly(double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
            {
                return 0;
            }
            else return Math.Clamp(value, 0d, 1000d);
        }

        private bool _sliderClicked;
        private double _seekPosition;



        public TrackStatusViewModel TrackStatus
        {
            get { return _trackStatus; }
            set { this.RaiseAndSetIfChanged(ref _trackStatus, value); }
        }
    }
}

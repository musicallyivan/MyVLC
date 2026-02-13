using LibVLCSharp.Shared;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace MyVLC
{
    public partial class MainWindow : Window
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private DispatcherTimer _timer;
        private bool _isFullscreen = false;
        private WindowState _prevState;
        private WindowStyle _prevStyle;
        private ResizeMode _prevResize;

        private bool _isDraggingSlider = false;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) => this.Focus();

            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);

            VideoView.MediaPlayer = _mediaPlayer;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(250);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            SliderVolume.Value = 80;
            _mediaPlayer.Volume = 80;

            _mediaPlayer.EndReached += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    BtnPlayPause.Content = "▶";
                });
            };
        }

        private void VideoView_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                ToggleFullscreen();
        }
        private void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                _prevState = WindowState;
                _prevStyle = WindowStyle;
                _prevResize = ResizeMode;

                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;

                _isFullscreen = true;
            }
            else
            {
                WindowStyle = _prevStyle;
                ResizeMode = _prevResize;
                WindowState = _prevState;

                _isFullscreen = false;
            }
        }
        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Vídeos|*.mp4;*.mkv;*.avi;*.mov;*.webm|Todos|*.*";

            if (dialog.ShowDialog() == true)
                OpenMedia(dialog.FileName);
        }

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null) return;

            if (_mediaPlayer.Media == null)
            {
                BtnOpen_Click(sender, e);
                return;
            }

            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                BtnPlayPause.Content = "▶";
            }
            else
            {
                _mediaPlayer.Play();
                BtnPlayPause.Content = "⏸";
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_mediaPlayer == null) return;
            if (_mediaPlayer.Media == null) return;

            var length = _mediaPlayer.Length;
            var time = _mediaPlayer.Time;

            if (length <= 0) return;

            if (!_isDraggingSlider)
                SliderProgress.Value = (time * 100.0) / length;

            TxtTime.Text = FormatTime(time);
            TxtTotal.Text = FormatTime(length);
        }

        private void SliderProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer == null) return;
            if (_mediaPlayer.Media == null) return;

            if (SliderProgress.IsMouseCaptureWithin)
            {
                _isDraggingSlider = true;
                return;
            }

            if (_isDraggingSlider)
            {
                _isDraggingSlider = false;

                var length = _mediaPlayer.Length;
                if (length > 0)
                {
                    long newTime = (long)(length * (SliderProgress.Value / 100.0));
                    _mediaPlayer.Time = newTime;
                }
            }
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer == null) return;
            _mediaPlayer.Volume = (int)SliderVolume.Value;
        }

        private string FormatTime(long ms)
        {
            var t = TimeSpan.FromMilliseconds(ms);

            // Si dura más de 1 hora, mostramos horas
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";

            return $"{t.Minutes:00}:{t.Seconds:00}";
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            base.OnClosed(e);
        }
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0) return;

            OpenMedia(files[0]);
        }
        private void OpenMedia(string filePath)
        {
            if (!File.Exists(filePath)) return;

            TxtNowPlaying.Text = Path.GetFileName(filePath);

            var media = new Media(_libVLC, new Uri(filePath));
            _mediaPlayer.Play(media);

            BtnPlayPause.Content = "⏸";
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_mediaPlayer == null) return;

            if (e.Key == System.Windows.Input.Key.Space)
            {
                BtnPlayPause_Click(null, null);
                e.Handled = true;
            }

            if (e.Key == System.Windows.Input.Key.F)
            {
                ToggleFullscreen();
                e.Handled = true;
            }

            if (e.Key == System.Windows.Input.Key.Right)
            {
                _mediaPlayer.Time += 5000; // +5s
                e.Handled = true;
            }

            if (e.Key == System.Windows.Input.Key.Left)
            {
                _mediaPlayer.Time -= 5000; // -5s
                e.Handled = true;
            }

            if (e.Key == System.Windows.Input.Key.Up)
            {
                SliderVolume.Value = Math.Min(100, SliderVolume.Value + 5);
                e.Handled = true;
            }

            if (e.Key == System.Windows.Input.Key.Down)
            {
                SliderVolume.Value = Math.Max(0, SliderVolume.Value - 5);
                e.Handled = true;
            }
        }

    }
}
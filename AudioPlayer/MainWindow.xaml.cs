using AudioPlayer.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace AudioPlayer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool mediaPlayerIsPlaying = false;
        private bool userIsDraggingSlider = false;

        List<Song> Playlist = new List<Song>();
        Song CurrentPlaying;
        string filePath;

        public MainWindow()
        {
            InitializeComponent();
            mePlayer.MediaOpened += mePlayer_MediaOpened;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void Row_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Ensure row was clicked and not empty space
            DataGridRow row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null) return;
            Song song = Playlist[row.GetIndex()];
            CurrentPlaying = song;
            mePlayer.Source = new Uri(song.Name);
            mePlayer.Play();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if ((mePlayer.Source != null) && (mePlayer.NaturalDuration.HasTimeSpan) && (!userIsDraggingSlider))
            {
                sliProgress.Minimum = 0;
                sliProgress.Maximum = mePlayer.NaturalDuration.TimeSpan.TotalSeconds;
                sliProgress.Value = mePlayer.Position.TotalSeconds;
            }
        }

        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Media files (*.mp3;*.mpg;*.mpeg)|*.mp3;*.mpg;*.mpeg";
            if (openFileDialog.ShowDialog() == true)
            {
                mePlayer.Source = new Uri(openFileDialog.FileName);
                filePath = openFileDialog.FileName;
                CurrentPlaying = new Song(openFileDialog.FileName, "");
                mePlayer.Play();
                mediaPlayerIsPlaying = true;
            }
        }

        void mePlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (!Playlist.Exists(elem => elem.Name == CurrentPlaying.Name))
            {
                CurrentPlaying = new Song(filePath, mePlayer.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss"));
                Playlist.Add(CurrentPlaying);
                PlaylistGrid.ItemsSource = null;
                PlaylistGrid.ItemsSource = Playlist;
            }
           
        }

        private void Play_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (mePlayer != null) && (mePlayer.Source != null);
        }

        private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mePlayer.Play();
            mediaPlayerIsPlaying = true;
        }

        private void Pause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = mediaPlayerIsPlaying;
        }

        private void Pause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mePlayer.Pause();
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = mediaPlayerIsPlaying;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mePlayer.Stop();
            mediaPlayerIsPlaying = false;
        }

        private void NextTrack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (mePlayer != null) && (mePlayer.Source != null);
        }

        private void NextTrack_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int index = Playlist.IndexOf(CurrentPlaying);
            if (index == Playlist.Count - 1) return;
            if (index >= 0)
            {
                mePlayer.Pause();
                CurrentPlaying = Playlist[index + 1];
                mePlayer.Source = new Uri(CurrentPlaying.Name);
                mePlayer.Play();
                mediaPlayerIsPlaying = true;

            }
        }

        private void PreviousTrack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (mePlayer != null) && (mePlayer.Source != null);
        }

        private void PreviousTrack_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int index = Playlist.IndexOf(CurrentPlaying);
            if (index <= 0) return;
            if (index > 0)
            {
                mePlayer.Pause();
                CurrentPlaying = Playlist[index - 1];
                mePlayer.Source = new Uri(CurrentPlaying.Name);
                mePlayer.Play();
                mediaPlayerIsPlaying = true;

            }
        }

        private void sliProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            userIsDraggingSlider = true;
        }

        private void sliProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            userIsDraggingSlider = false;
            mePlayer.Position = TimeSpan.FromSeconds(sliProgress.Value);
        }

        private void sliProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblProgressStatus.Text = TimeSpan.FromSeconds(sliProgress.Value).ToString(@"hh\:mm\:ss");
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            mePlayer.Volume += (e.Delta > 0) ? 0.1 : -0.1;
        }
    }
}

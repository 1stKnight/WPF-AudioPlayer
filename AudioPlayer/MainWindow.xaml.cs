using AudioPlayer.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Runtime.InteropServices;

namespace AudioPlayer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IMMDeviceEnumerator deviceEnumerator = null;
        IMMDevice speakers = null; //текущее аудиоустройство
        uint this_pid; //идентификатор процесса

        private bool mediaPlayerIsPlaying = false;
        private bool userIsDraggingSlider = false;

        List<Song> Playlist = new List<Song>();
        Song CurrentPlaying;
        string filePath;

        public MainWindow()
        {
            InitializeComponent();

            System.Diagnostics.Process pr = System.Diagnostics.Process.GetCurrentProcess();
            using (pr)
            {
                this_pid = (uint)pr.Id;
            }

            // get default audio device
            deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

            mePlayer.MediaOpened += mePlayer_MediaOpened;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        //считывает текущее значение уровней звука и обновляет UI
        public void UpdateVisualizer()
        {
            if (speakers == null) return;

            IAudioSessionManager2 mgr = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionControl ctl = null;
            IAudioSessionControl2 ctl2 = null;
            IAudioMeterInformation meter = null;

            try
            {

                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device            
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                float max_val = 0.0f; //максимальное значение уровня звука для всех сессий
                int h_min = 50, h_max = 120;//макс. и мин. значение высоты для эллипса

                int hr;
                uint pid = 0;
                float val = 0.0f;

                for (int i = 0; i < count; i++)
                {
                    if (ctl != null) { Marshal.ReleaseComObject(ctl); ctl = null; }
                    if (ctl2 != null) { Marshal.ReleaseComObject(ctl2); ctl2 = null; }
                    if (meter != null) { Marshal.ReleaseComObject(meter); meter = null; }

                    //получаем WASAPI-сессию
                    hr = sessionEnumerator.GetSession(i, out ctl);
                    if (hr != 0) continue;

                    ctl2 = (IAudioSessionControl2)ctl;
                    pid = 0;
                    ctl2.GetProcessId(out pid);
                    if (pid != this_pid) continue; //интересуют только сессии текущего процесса

                    meter = (IAudioMeterInformation)ctl;
                    hr = meter.GetPeakValue(out val);//получаем уровень звука
                    if (hr != 0) { continue; }
                    if (val > max_val) max_val = val;

                }

                //изменяем высоту эллипса в соответствии со значением максимального уровня звука
                ellVisualizer.Height = h_min + max_val * (h_max - h_min);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString());
            }
            finally
            {
                //очистка ресурсов
                if (sessionEnumerator != null) { Marshal.ReleaseComObject(sessionEnumerator); sessionEnumerator = null; }
                if (mgr != null) { Marshal.ReleaseComObject(mgr); mgr = null; }

                if (ctl != null) { Marshal.ReleaseComObject(ctl); ctl = null; }
                if (ctl2 != null) { Marshal.ReleaseComObject(ctl2); ctl2 = null; }
                if (meter != null) { Marshal.ReleaseComObject(meter); meter = null; }
            }
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
                UpdateVisualizer();
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


    // *** COM Objects declarations ***
    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    internal enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
        EDataFlow_enum_count
    }

    internal enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
        ERole_enum_count
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        int NotImpl1();

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);

        // the rest is not implemented
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        // the rest is not implemented
    }

    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager2
    {
        int NotImpl1();
        int NotImpl2();

        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

        // the rest is not implemented
    }

    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int SessionCount);

        [PreserveSig]
        int GetSession(int SessionCount, out IAudioSessionControl Session);
    }

    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionControl
    {
        int NotImpl1();

        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        // the rest is not implemented
    }

    //Источник: https://github.com/maindefine/volumecontrol/blob/master/C%23/CoreAudioApi/Interfaces/IAudioSessionControl2.cs
    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionControl2
    {
        //IAudioSession functions
        [PreserveSig]
        int GetState(out object state);
        [PreserveSig]
        int GetDisplayName(out IntPtr name);
        [PreserveSig]
        int SetDisplayName(string value, Guid EventContext);
        [PreserveSig]
        int GetIconPath(out IntPtr Path);
        [PreserveSig]
        int SetIconPath(string Value, Guid EventContext);
        [PreserveSig]
        int GetGroupingParam(out Guid GroupingParam);
        [PreserveSig]
        int SetGroupingParam(Guid Override, Guid Eventcontext);
        [PreserveSig]
        int RegisterAudioSessionNotification(object NewNotifications);
        [PreserveSig]
        int UnregisterAudioSessionNotification(object NewNotifications);
        //IAudioSession2 functions
        [PreserveSig]
        int GetSessionIdentifier(out IntPtr retVal);
        [PreserveSig]
        int GetSessionInstanceIdentifier(out IntPtr retVal);
        [PreserveSig]
        int GetProcessId(out UInt32 retvVal);
        [PreserveSig]
        int IsSystemSoundsSession();
        [PreserveSig]
        int SetDuckingPreference(bool optOut);


    }

    //Источник: https://github.com/maindefine/volumecontrol/blob/master/C%23/CoreAudioApi/Interfaces/IAudioMeterInformation.cs
    [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioMeterInformation
    {
        [PreserveSig]
        int GetPeakValue(out float pfPeak);
        [PreserveSig]
        int GetMeteringChannelCount(out int pnChannelCount);
        [PreserveSig]
        int GetChannelsPeakValues(int u32ChannelCount, [In]   IntPtr afPeakValues);
        [PreserveSig]
        int QueryHardwareSupport(out int pdwHardwareSupportMask);
    };
}

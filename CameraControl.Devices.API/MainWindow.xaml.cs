using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Forms;
using CameraControl.Devices.Classes;
using System.Net;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using Microsoft.Extensions.DependencyInjection;
using System.Timers;
using System.Drawing;
using CameraControl.Devices.Nikon;
using System.Reflection;
using MaterialDesignThemes.Wpf;
using System.Linq.Expressions;
using System.Windows.Forms; // Aggiungi questo namespace
using MessageBox = System.Windows.MessageBox; // Per evitare conflitti con System.Windows.Forms.MessageBox


namespace CameraControl.Devices.API
{

    public partial class Settings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _savePath;
        private string _serverPort;
        private int _downloadMode;


        public string savePath
        {

            get
            {
                return _savePath;
            }
            set
            {
                _savePath = value;
                OnPropertyChanged();
            }
        }

        public int downloadMode
        {

            get
            {
                return _downloadMode;
            }
            set
            {
                _downloadMode = value;
                OnPropertyChanged();
            }
        }


        public string serverPort
        {
            get
            {
                return _serverPort;
            }
            set
            {
                _serverPort = value;
                OnPropertyChanged();
            }
        }



        public Settings()
        {

            savePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\Camera1";
            serverPort = "7000";
            downloadMode = 1;

        }
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }



    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CameraApiServer _apiServer;
        public CameraDeviceManager DeviceManager { get; set; }
        public Settings settings;
        private Logger _logger;
        private List<LiveViewWindow> liveViewWindows;

      
        private readonly PaletteHelper _paletteHelper = new PaletteHelper();


    public MainWindow()
        {

            _logger = new Logger(Path.GetDirectoryName(Assembly.GetExecutingAssembly().FullName) + "log.txt");
            settings = new Settings();
            DeviceManager = new CameraDeviceManager();
            DeviceManager.UseExperimentalDrivers = true;

            //DeviceManager.AddFakeCamera();

            DeviceManager.CameraSelected += DeviceManager_CameraSelected;
            DeviceManager.CameraConnected += DeviceManager_CameraConnected;
            DeviceManager.PhotoCaptured += DeviceManager_PhotoCaptured;
            DeviceManager.CameraDisconnected += DeviceManager_CameraDisconnected;

            // For experimental Canon driver support- to use canon driver the canon sdk files should be copied in application folder
            DeviceManager.UseExperimentalDrivers = true;
            DeviceManager.DisableNativeDrivers = false;

            InitializeComponent();

            Log.LogError += Log_LogDebug;
            Log.LogDebug += Log_LogDebug;
            Log.LogInfo += Log_LogDebug;


            listBox1.DataContext = DeviceManager.ConnectedDevices;

            serverLogTextBox.DataContext = _logger;
            portTextBox.DataContext = settings;
            downloadComboBox.DataContext = settings;
            savePathTextBox.DataContext = settings;


            DeviceManager.ConnectToCamera();

            StartServer();

        }



        void Log_LogDebug(LogEventArgs e)
        {
            // Metodo che aggiorna l'interfaccia utente
            Action method = delegate
            {
                LogTextBox.AppendText((string)e.Message);
                if (e.Exception != null)
                    LogTextBox.AppendText((string)e.Exception.StackTrace);
                LogTextBox.AppendText(Environment.NewLine);
            };

            // Controlla se il codice è nel thread dell'interfaccia utente (UI thread)
            if (LogTextBox.Dispatcher.CheckAccess())
            {
                // Se siamo già nel thread UI, invoca direttamente il metodo
                method.Invoke();
            }
            else
            {
                // Se siamo in un thread diverso, usa Dispatcher per eseguire il metodo nel UI thread
                LogTextBox.Dispatcher.Invoke(method);
            }
        }

        void DeviceManager_CameraSelected(ICameraDevice oldcameraDevice, ICameraDevice newcameraDevice)
        {

        }


        void DeviceManager_CameraConnected(ICameraDevice cameraDevice)
        {
            // RefreshDisplay();
            if (_apiServer != null)
            {
                _apiServer.generateRouteHandlers(DeviceManager);
            }

        }


        void DeviceManager_CameraDisconnected(ICameraDevice cameraDevice)
        {
            // RefreshDisplay();

        }

        public static string SanitizeFileName(string fileName)
        {
            // Ottieni i caratteri non validi per i percorsi file
            char[] invalidChars = Path.GetInvalidFileNameChars();

            // Rimuovi i caratteri non validi
            StringBuilder sanitizedFileName = new StringBuilder(fileName);

            foreach (char c in invalidChars)
            {
                sanitizedFileName.Replace(c.ToString(), string.Empty);
            }

            // Rimuovi spazi finali o iniziali
            return sanitizedFileName.ToString().Trim();
        }

        public static string SanitizeFilePath(string filePath)
        {
            // Ottieni i caratteri non validi per i percorsi di file e directory
            char[] invalidPathChars = Path.GetInvalidPathChars();

            List<char> invalidPathList = new List<char>(invalidPathChars);
            invalidPathList.Add(' '); //avoid spaces
            invalidPathChars = invalidPathList.ToArray();

            // Sanifica il percorso sostituendo i caratteri non validi
            StringBuilder sanitizedPath = new StringBuilder(filePath);

            // Rimuovi i caratteri non validi nei percorsi
            foreach (char c in invalidPathChars)
            {
                sanitizedPath.Replace(c.ToString(), "_"); // Puoi sostituire con un trattino o altro
            }


            // Rimuovi spazi finali o iniziali
            return sanitizedPath.ToString().Trim();
        }

        private void PhotoCaptured(object o)
        {
            PhotoCapturedEventArgs eventArgs = o as PhotoCapturedEventArgs;
            if (eventArgs == null)
                return;
            try
            {
                // string test = SanitizeFilePath(Path.Combine(settings.savePath, "\\", eventArgs.CameraDevice.DeviceName, "_", eventArgs.CameraDevice.SerialNumber, "\\", Path.GetFileName(eventArgs.FileName)));
                string filePath = settings.savePath + "\\" + SanitizeFilePath(eventArgs.CameraDevice.DeviceName + "_" + eventArgs.CameraDevice.SerialNumber + "\\");
                string fileName = SanitizeFileName(Path.GetFileName(eventArgs.FileName));

                fileName = filePath + fileName;

                // if file exist try to generate a new filename to prevent file lost. 
                // This useful when camera is set to record in ram the the all file names are same.
                if (File.Exists(fileName))
                    fileName =
                      StaticHelper.GetUniqueFilename(
                        Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "_", 0,
                        Path.GetExtension(fileName));

                // check the folder of filename, if not found create it
                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }



                if (settings.downloadMode !=  2)
                {
                    eventArgs.CameraDevice.TransferFile(eventArgs.Handle, fileName);
                    // the IsBusy may used internally, if file transfer is done should set to false  
                    eventArgs.CameraDevice.IsBusy = false;
                }

            }
            catch (Exception exception)
            {
                eventArgs.CameraDevice.IsBusy = false;
                //   MessageBox.Show("Error download photo from camera :Environment.NewLine" + exception.Message);
            }
        }
        void DeviceManager_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
        {
            // to prevent UI freeze start the transfer process in a new thread
            Thread thread = new Thread(PhotoCaptured);
            thread.Start(eventArgs);
        }




        private void stopServerBtn_Click(object sender, RoutedEventArgs e)
        {
            _apiServer.Stop();
        }

        private void StartServer() {

            _apiServer = new CameraApiServer(DeviceManager, this, _logger);
            _apiServer.generateRouteHandlers(DeviceManager);
            _apiServer.Start();
            ServerRunningChkBox.DataContext = _apiServer;
            startServerBtn.DataContext = _apiServer;
            stopServerBtn.DataContext = _apiServer;

        }
        private void startServerBtn_Click(object sender, RoutedEventArgs e)
        {
            StartServer();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {/*
            base.OnClosing(e);
            _apiServer?.Stop(); // Interrompe il server se attivo*/
        }

        private async void FocusNear100_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {
                await Task.Run(() =>
                {
                    if (deviceReady(device, 5))
                        device.Focus(-100);
                });
            }
        }

        private async void FocusFar100_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {
                await Task.Run(() =>
                {
                    if (deviceReady(device, 5))
                        device.Focus(100);
                });
            }
        }

        private async void FocusNear10_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {
                await Task.Run(() =>
                {
                    if (deviceReady(device, 5))
                        device.Focus(-10);
                });
            }
        }

        private async void FocusNear1_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {
                await Task.Run(() =>
                {
                    if (deviceReady(device, 5))
                        device.Focus(-1);
                });
            }
        }

        private async void FocusFar1_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {
                await Task.Run(() =>
                {
                    if (deviceReady(device, 5))
                        device.Focus(1);
                });
            }
        }

        private async void FocusFar10_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {
                await Task.Run(() =>
                {
                    if (deviceReady(device, 5))
                        device.Focus(10);
                });
            }
        }

        private async void CaptureBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {

                await Task.Run(() =>
                {
                    if (deviceReady(device, 5))
                        device.CapturePhoto();
                                });


            }
        }

        private async void BulbStartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {
                await Task.Run(() =>
                {
                    if (deviceReady(device, 5))
                    {

                        device.IsBusy = true;
                        device.LockCamera();
                        device.StartBulbMode();
                    }

                });


            }


        }

        private void BulbEndBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {
                // Ottieni il valore dal Tag
                //int parameter = int.Parse(button.Tag.ToString());
                device.EndBulbMode();

            }
        }

        private async void LockBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {
                // Ottieni il valore dal Tag
                //int parameter = int.Parse(button.Tag.ToString());
                await Task.Run(() =>
                {
                    if (deviceReady(device, 5))
                        device.LockCamera();
                });

                

            }
        }

        private async void UnlockBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {
                await Task.Run(() =>
                {
                    if (deviceReady(device, 5))
                        device.UnLockCamera();
                });

            }
        }

        private void BulbCustomBtn_Click(object sender, RoutedEventArgs e)
        {

            BulbCapture(sender, e, Convert.ToInt32(BulbTimeTextBox.Text));

        }

        private async void BulbCapture(object sender, RoutedEventArgs e, int time)
        {

            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {
                
                if (deviceReady(device, 5))
                {
                    device.StartBulbMode();
                    await Task.Delay(time);
                    device.EndBulbMode();
                }
            }

        }

        private void NumericTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Consente solo numeri e simboli (es. per i numeri decimali)
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        private async void LVModeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {

                if (liveViewWindows == null || liveViewWindows.Count == 0)
                {
                    liveViewWindows = new List<LiveViewWindow>();
                    LiveViewWindow lv = new LiveViewWindow(device,liveViewWindows);
                    liveViewWindows.Add(lv);
                    lv.Show();
                }

                else {

                    foreach (LiveViewWindow lv in liveViewWindows) {

                        if (device == lv.device) {
                            
                            lv.Activate();
                        
                        }
                    
                    }
                
                
                }


            }
        }
 

        private void LVModeStopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is ICameraDevice device)
            {

                foreach (LiveViewWindow lv in liveViewWindows)
                {

                    if (device == lv.device)
                    {
                        closeLiveView(lv);
                        break;
                    }

                }

            }
        }

        public void closeLiveView(LiveViewWindow liveViewWindow) { 
        
            liveViewWindow.Close();
            liveViewWindow.device.StopLiveView();
            liveViewWindows.Remove(liveViewWindow);
            
        }

        private bool deviceReady(ICameraDevice device, int tries)
        {

            for (int i = 0; i < tries; i++)
            {
                if (!device.IsBusy) return true;
                Thread.Sleep(100);
            }

            return false;

        }



        private void darkThemeChkBox_Click(object sender, RoutedEventArgs e)
        {
            ITheme theme = _paletteHelper.GetTheme();

            if (darkThemeChkBox.IsChecked == true)
            {

                theme.SetBaseTheme(Theme.Dark);
                _paletteHelper.SetTheme(theme);
            }

            else

            {

                theme.SetBaseTheme(Theme.Light);
                _paletteHelper.SetTheme(theme);
            }
        }

        private void downloadComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            try
            {
                foreach (ICameraDevice device in DeviceManager.ConnectedDevices)
                {

                    switch (downloadComboBox.SelectedIndex)
                    {

                        case 0:

                            device.CaptureInSdRam = true;

                            break;
                        case 1:
                            device.CaptureInSdRam = false;
                            break;
                        case 2:
                            device.CaptureInSdRam = false;
                            break;
                        default:
                            device.CaptureInSdRam = true;
                            break;

                    }

                }


            }
            catch { }
            

        }

        private void savePathBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog();
        }

        public void OpenFolderDialog()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Seleziona una cartella";
                dialog.ShowNewFolderButton = true; // Mostra il pulsante per creare una nuova cartella
                dialog.RootFolder = Environment.SpecialFolder.Desktop; // Cartella iniziale

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;
                    settings.savePath = selectedPath;
                }
            }
        }

    }

}


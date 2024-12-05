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
using System.Windows.Shapes;
using System.Windows.Forms;
using CameraControl.Devices.Classes;


namespace CameraControl.Devices.API
{

    public partial class Settings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _Path1;
        private string _Path2;

        private int _Port1;
        private int _Port2;


        public string Path1
        {

            get
            {
                return _Path1;
            }
            set
            {
                _Path1 = value;
                OnPropertyChanged();
            }
        }

        public string Path2
        {

            get
            {
                return _Path2;
            }
            set
            {
                _Path2 = value;
                OnPropertyChanged();
            }
        }

        public int Port1
        {
            get
            {
                return _Port1;
            }
            set
            {
                _Port1 = value;
                OnPropertyChanged();
            }
        }
        public int Port2
        {

            get
            {
                return _Port2;
            }
            set
            {
                _Port2 = value;
                OnPropertyChanged();
            }
        }


        public Settings()
        {

            Path1 = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\Camera1";
            Path2 = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\Camera2";
            Port1 = 7000;
            Port2 = 7001;

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
        public CameraDeviceManager DeviceManager1 { get; set; }
      //  public CameraDeviceManager DeviceManager2 { get; set; }

        public Settings settings;

        public MainWindow()
        {
            settings = new Settings();
            this.DataContext = settings;

            DeviceManager1 = new CameraDeviceManager();
            DeviceManager1.CameraSelected += DeviceManager_CameraSelected;
            DeviceManager1.CameraConnected += DeviceManager_CameraConnected;
            DeviceManager1.PhotoCaptured += DeviceManager_PhotoCaptured;
            DeviceManager1.CameraDisconnected += DeviceManager_CameraDisconnected;
            // For experimental Canon driver support- to use canon driver the canon sdk files should be copied in application folder
            DeviceManager1.UseExperimentalDrivers = true;
            DeviceManager1.DisableNativeDrivers = false;
            /*
            DeviceManager2 = new CameraDeviceManager();
            DeviceManager2.CameraSelected += DeviceManager_CameraSelected;
            DeviceManager2.CameraConnected += DeviceManager_CameraConnected;
            DeviceManager2.PhotoCaptured += DeviceManager_PhotoCaptured;
            DeviceManager2.CameraDisconnected += DeviceManager_CameraDisconnected;
            // For experimental Canon driver support- to use canon driver the canon sdk files should be copied in application folder
            DeviceManager2.UseExperimentalDrivers = true;
            DeviceManager2.DisableNativeDrivers = false;
            */
            InitializeComponent();
            Log.LogError += Log_LogDebug;
            Log.LogDebug += Log_LogDebug;
            Log.LogInfo += Log_LogDebug;

            InitializeComponent();

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

        private void RefreshDisplay()
        {
            // Metodo che aggiorna l'interfaccia utente
            Action method = delegate
            {
                // Supponiamo che cmb_cameras sia un ComboBox
                DevicesComboBox1.Items.Clear();
                DevicesComboBox2.Items.Clear();

                foreach (ICameraDevice cameraDevice in DeviceManager1.ConnectedDevices)
                {
                    DevicesComboBox1.Items.Add(cameraDevice);
                    DevicesComboBox2.Items.Add(cameraDevice);
                }

                DevicesComboBox1.DisplayMemberPath = "DeviceName"; // WPF usa DisplayMemberPath invece di DisplayMember
                DevicesComboBox1.SelectedItem = DeviceManager1.SelectedCameraDevice;

                //DevicesComboBox2.DisplayMemberPath = "DeviceName"; // WPF usa DisplayMemberPath invece di DisplayMember
                //DevicesComboBox2.SelectedItem = DeviceManager2.SelectedCameraDevice;

               /* if (DeviceManager1.SelectedCameraDevice != null)
                {
                    DeviceManager1.SelectedCameraDevice.CaptureInSdRam = true;
                    // Verifica se la fotocamera supporta il live view
                    btn_liveview.IsEnabled = DeviceManager1.SelectedCameraDevice.GetCapability(CapabilityEnum.LiveView);
                }*/
            };

            // Verifica se siamo nel thread dell'interfaccia utente
            if (DevicesComboBox1.Dispatcher.CheckAccess())
            {
                // Se siamo nel thread UI, esegui direttamente il metodo
                method.Invoke();
            }
            else
            {
                // Se siamo in un thread diverso, invoca il metodo nel UI thread
                DevicesComboBox1.Dispatcher.Invoke(method);
            }

            if (DevicesComboBox2.Dispatcher.CheckAccess())
            {
                // Se siamo nel thread UI, esegui direttamente il metodo
                method.Invoke();
            }
            else
            {
                // Se siamo in un thread diverso, invoca il metodo nel UI thread
                DevicesComboBox2.Dispatcher.Invoke(method);
            }
        }

        void DeviceManager_CameraSelected(ICameraDevice oldcameraDevice, ICameraDevice newcameraDevice)
        {
            // Metodo che aggiorna l'interfaccia utente
           /* Action method = delegate
            {
                // Supponiamo che tu abbia un pulsante chiamato btnLiveView
                btnLiveView.IsEnabled = newcameraDevice.GetCapability(CapabilityEnum.LiveView);
            };

            // Verifica se siamo nel thread dell'interfaccia utente
            if (btnLiveView.Dispatcher.CheckAccess())
            {
                // Se siamo nel thread UI, esegui direttamente il metodo
                method.Invoke();
            }
            else
            {
                // Se siamo in un thread diverso, invoca il metodo nel UI thread
                btnLiveView.Dispatcher.Invoke(method);
            }

            */
        }




        void DeviceManager_CameraConnected(ICameraDevice cameraDevice)
        {
            RefreshDisplay();
        }


        void DeviceManager_CameraDisconnected(ICameraDevice cameraDevice)
        {
            RefreshDisplay();
        }


        private void PhotoCaptured(object o)
        {/*
            PhotoCapturedEventArgs eventArgs = o as PhotoCapturedEventArgs;
            if (eventArgs == null)
                return;
            try
            {
                string fileName = Path.Combine(FolderForPhotos, Path.GetFileName(eventArgs.FileName));
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
                eventArgs.CameraDevice.TransferFile(eventArgs.Handle, fileName);
                // the IsBusy may used internally, if file transfer is done should set to false  
                eventArgs.CameraDevice.IsBusy = false;
                img_photo.ImageLocation = fileName;
            }
            catch (Exception exception)
            {
                eventArgs.CameraDevice.IsBusy = false;
                MessageBox.Show("Error download photo from camera :\n" + exception.Message);
            }*/
        }
        void DeviceManager_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
        {
            // to prevent UI freeze start the transfer process in a new thread
            Thread thread = new Thread(PhotoCaptured);
            thread.Start(eventArgs);
        }


        private void DevicesComboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeviceManager1.SelectedCameraDevice = DeviceManager1.ConnectedDevices[DevicesComboBox1.SelectedIndex];
        }

        private void DevicesComboBox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           // DeviceManager2.SelectedCameraDevice = DeviceManager2.ConnectedDevices[DevicesComboBox2.SelectedIndex];
        }

        private void FocusFar2_Click(object sender, RoutedEventArgs e)
        {
            DeviceManager1.ConnectedDevices[0].Focus(100);
        }

        private void FocusClose2_Click(object sender, RoutedEventArgs e)
        {
            DeviceManager1.ConnectedDevices[0].Focus(-100);
        }
    }
}


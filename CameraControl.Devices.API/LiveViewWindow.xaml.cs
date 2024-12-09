using CameraControl.Devices.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
using System.Windows.Shapes;

namespace CameraControl.Devices.API
{
    /// <summary>
    /// Logica di interazione per LiveViewWindow.xaml
    /// </summary>
    public partial class LiveViewWindow : Window
    {

        private ICameraDevice _device;
        public LiveViewWindow(ICameraDevice device)
        {
            this.Title = device.DeviceName + device.SerialNumber;
            _device = device;
            StartLiveView(_device);

            InitializeComponent();
        }

        private async void StartLiveView(ICameraDevice device)
        {
            await Task.Run(() =>
            {
                bool retry;
                do
                {
                    retry = false;
                    try
                    {
                        device.StartLiveView();
                    }
                    catch (DeviceException exception)
                    {
                        if (exception.ErrorCode == ErrorCodes.MTP_Device_Busy || exception.ErrorCode == ErrorCodes.ERROR_BUSY)
                        {
                            // this may cause infinite loop
                            Thread.Sleep(100);
                            retry = true;
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Error occurred: " + exception.Message);
                        }
                    }

                } while (retry);

                UpdateImage(device);


            });

        }

        private async void UpdateImage(ICameraDevice device)
        {
            await Task.Run(() =>
            {
                LiveViewData liveViewData = null;

                while (true)
                {
                    try
                    {
                        liveViewData = device.GetLiveViewImage();
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    if (liveViewData != null && liveViewData.ImageData != null)
                    {
                        try
                        {
                            Bitmap b = new Bitmap(new MemoryStream(liveViewData.ImageData,
                                                                   liveViewData.ImageDataPosition,
                                                                   liveViewData.ImageData.Length -
                                                                   liveViewData.ImageDataPosition));

                            // Usa il Dispatcher per aggiornare l'UI
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                ImageBox.Source = BitmapToImageSource(b);
                            });

                            Task.Delay(1000 / 15).Wait();
                        }
                        catch (Exception)
                        {
                            // Gestisci eventuali eccezioni
                        }
                    }
                }
            });
        }


        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _device.StopLiveView();
        }
    }
}

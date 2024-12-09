using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CameraControl.Devices.API
{
    public class Logger : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private String _logHistory = "";
        private String _logFilePath;

        public Logger(string logFilePath)
        {
           
           _logFilePath = logFilePath;
        }

        public void Log(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";

            // Aggiorna la TextBox
            logHistory = logHistory + logMessage + Environment.NewLine;

            // Scrive il log sul file
            File.AppendAllText(_logFilePath, logMessage + Environment.NewLine, Encoding.UTF8);
        }

        public string logHistory
        {
            get
            {
                return _logHistory;
            }
            private set
            {
                _logHistory = value;
                OnPropertyChanged();
            }
        }


        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


    }
}

using CameraControl.Devices.Classes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CameraControl.Devices.API
{
    public class Commands
    {

        public List<string> deviceGetSingleCommands { get; private set; } = new List<string>();
        public List<string> deviceGetMultiCommands { get; private set; } = new List<string>();

        public List<string> deviceSetCommands { get; private set; } = new List<string>();
        public List<string> deviceActionCommands { get; private set; } = new List<string>();

        public List<string> genericCommands { get; private set; } = new List<string>();
        public string current = "current";
        public string available = "available";
        public string valuestring = "value";


        public Commands()
        {
            genericCommands.Add("Get-AvailableCommands");
            genericCommands.Add("Get-Devices");

            deviceGetSingleCommands.Add("Get-CameraInfo");
            deviceGetSingleCommands.Add("Get-Battery");


            deviceGetMultiCommands.Add("Get-Mode");
            deviceGetMultiCommands.Add("Get-ShutterSpeed");
            deviceGetMultiCommands.Add("Get-FNumber");
            deviceGetMultiCommands.Add("Get-IsoNumber");
            deviceGetMultiCommands.Add("Get-FocusMode");


            //Set commands requires ?value=xxx on url
            deviceSetCommands.Add("Set-ShutterSpeed");
            deviceSetCommands.Add("Set-FNumber");
            deviceSetCommands.Add("Set-IsoNumber");
            deviceSetCommands.Add("Set-FocusMode");
            deviceSetCommands.Add("Set-Focus");
            deviceSetCommands.Add("Set-FocusRoutine");

            deviceActionCommands.Add("Action-Capture");
            deviceActionCommands.Add("Action-CaptureBulb");
            deviceActionCommands.Add("Action-BulbStart");
            deviceActionCommands.Add("Action-BulbEnd");
            deviceActionCommands.Add("Action-Lock");
            deviceActionCommands.Add("Action-Unlock");
        }
    }

    public class CameraApiServer : INotifyPropertyChanged
    {
        private CancellationTokenSource _cancellationTokenSource;
        public event PropertyChangedEventHandler PropertyChanged;
        private CameraDeviceManager _deviceManager;
        private HttpListener _listener;
        private bool _isRunning;
        private bool _isNotRunning;
        public Commands commands;
        public string listener_url;
        Dictionary<string, Action<HttpListenerContext>> routeHandlers;
        private const string html_ct = "text/html";
        private const string plain_ct = "text/plain";
        private Logger _logger;


        public CameraApiServer(CameraDeviceManager deviceManager, MainWindow mainWindow, Logger logger)
        {
            _logger = logger;
            commands = new Commands();
            listener_url = "http://localhost:" + mainWindow.portTextBox.Text + "/";
            _deviceManager = deviceManager;
            generateRouteHandlers(deviceManager);
        }

        public bool isRunning
        {

            get { return _isRunning; }

            private set
            {
                _isRunning = value;
                isNotRunning = !value;
                OnPropertyChanged();
            }

        }

        public bool isNotRunning
        {
            get
            {
                return _isNotRunning;
            }

            private protected set
            {
                _isNotRunning = value;
                OnPropertyChanged();
            }
        }


        public void generateRouteHandlers(CameraDeviceManager deviceManager)
        {
            List<string> validURLs = new List<string>();

            foreach (string command in commands.genericCommands)
            {
                validURLs.Add(SanitizeUrl("/" + command));

            }

            foreach (ICameraDevice device in deviceManager.ConnectedDevices)
            {

                foreach (string command in commands.deviceGetSingleCommands)
                {
                    validURLs.Add("/" + device.SerialNumber + "/" + command);

                }


                foreach (string command in commands.deviceGetMultiCommands)
                {
                    validURLs.Add("/" + device.SerialNumber + "/" + command + "/" + commands.current);
                    validURLs.Add("/" + device.SerialNumber + "/" + command + "/" + commands.available);

                }

                foreach (string command in commands.deviceSetCommands)
                {

                    validURLs.Add("/" + device.SerialNumber + "/" + command);

                }

                foreach (string command in commands.deviceActionCommands)
                {
                    validURLs.Add("/" + device.SerialNumber + "/" + command);

                }


            }

            // Inizializza il dizionario
            routeHandlers = new Dictionary<string, Action<HttpListenerContext>>
            {
                { "/", context => HandleRootRequest(context) } // Route predefinita
             };

            // Aggiungi dinamicamente handler per i comandi generici
            foreach (string route in validURLs)
            {
                // Assicurati che il percorso inizi con una "/" se necessario
                string formattedRoute = route.StartsWith("/") ? route : "/" + route;

                // Aggiungi l'handler per ogni route
                routeHandlers[formattedRoute] = context => HandleCommand(context, formattedRoute);
            }
        }

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add(listener_url);  // Indirizzo base
            _listener.Start();
            isRunning = true;

            _logger.Log("Server started and listening on: " + listener_url);

            // Avvia un task invece di un thread
            Task.Run(() => ListenForRequests(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            if (_listener != null && _listener.IsListening)
            {
                isRunning = false;
                _cancellationTokenSource.Cancel(); // Ferma il ciclo
                _listener.Stop(); // Ferma il listener
                _listener.Close(); // Rilascia le risorse
                _logger.Log("Server stopped");
            }
        }
        private void ListenForRequests(CancellationToken cancellationToken)
        {
            while (isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext(); // Attende una richiesta
                    string requestedUrl = context.Request.Url.AbsolutePath;

                    _logger.Log(requestedUrl + " from client " + context.Request.RemoteEndPoint.ToString());

                    // Controlla se c'è un handler per la rotta richiesta
                    if (routeHandlers.ContainsKey(requestedUrl))
                    {
                        routeHandlers[requestedUrl](context); // Esegui l'handler per la rotta
                    }
                    else
                    {
                        // Se non c'è un handler per la rotta, invia una risposta di errore
                        SendErrorResponse(context, 404, "Route not found: " + requestedUrl);
                    }
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995) // Codice 995: I/O cancellato
                {
                    _logger.Log("Listener stopped gracefully.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log("Error: " + ex.Message);
                }
            }
        }

        private string href(string url)
        {

            string base_url = "";
            string path = "";

            if (listener_url.EndsWith("/"))
            {
                base_url = listener_url.Substring(0, listener_url.Length - 1);
            }
            else
            {
                base_url = listener_url;
            }


            if (url.StartsWith("/"))
            {
                path = url;
            }
            else
            {
                path = "/" + url;
            }


            return "<a href =\"" + base_url + path + "\">" + url + "</a>";

        }

        // Esempio di gestione della richiesta per la root
        private void HandleRootRequest(HttpListenerContext context)
        {

            String responseString = "digiCamControlAPI server is running<br>Go to " + href(commands.genericCommands[0]) + " for command list";

            SendResponse(context, responseString, html_ct);
        }

        // Esempio di gestione dei comandi generici
        private void HandleCommand(HttpListenerContext context, string command)
        {

            try
            {
                const string ok_string = "OK";
                const string not_ready = "Device not ready";
                string[] urlSegments = context.Request.Url.AbsolutePath.Split('/');


                //Get-AvailableCommands
                if (urlSegments[1] == commands.genericCommands[0])
                {

                    string responseString = "";

                    foreach (string s in routeHandlers.Keys.ToList())
                    {
                        responseString = responseString + href(s) + "<br>";

                    }

                    SendResponse(context, responseString, html_ct);
                    return;

                }

                //Get-Devices
                if (urlSegments[1] == commands.genericCommands[1])
                {

                    string responseString = "";

                    foreach (ICameraDevice device in _deviceManager.ConnectedDevices)
                    {
                        responseString = responseString + device.SerialNumber + Environment.NewLine;

                    }

                    SendResponse(context, responseString, plain_ct);
                    return;

                }

                // process device commands
                foreach (ICameraDevice device in _deviceManager.ConnectedDevices)
                {


                    if (urlSegments[1] == device.SerialNumber)
                    {

                        //Get-CameraInfo
                        if (urlSegments[2] == commands.deviceGetSingleCommands[0])
                        {
                            string responseString = device.Manufacturer + Environment.NewLine + device.DeviceName + Environment.NewLine + device.SerialNumber;
                            SendResponse(context, responseString, plain_ct);
                            return;
                        }

                        //Get-Battery
                        if (urlSegments[2] == commands.deviceGetSingleCommands[1])
                        {
                            string responseString = Convert.ToString(device.Battery);
                            SendResponse(context, responseString, plain_ct);
                            return;
                        }

                        //Get-Mode
                        if (urlSegments[2] == commands.deviceGetMultiCommands[0])
                        {
                            if (urlSegments[3] == commands.current)
                            {
                                string responseString = device.Mode.Value;
                                SendResponse(context, responseString, plain_ct);
                                return;
                            }
                            if (urlSegments[3] == commands.available)
                            {
                                string responseString = string.Join(Environment.NewLine, device.Mode.Values);

                                SendResponse(context, responseString, plain_ct);
                                return;
                            }

                        }


                        //Get-ShutterSpeed
                        if (urlSegments[2] == commands.deviceGetMultiCommands[1])
                        {
                            if (urlSegments[3] == commands.current)
                            {
                                string responseString = sanitizeShutterSpeed(device.ShutterSpeed.Value);
                                SendResponse(context, responseString, plain_ct);
                                return;
                            }
                            if (urlSegments[3] == commands.available)
                            {
                                string responseString = sanitizeShutterSpeed(string.Join(Environment.NewLine, device.ShutterSpeed.Values));

                                SendResponse(context, responseString, plain_ct);
                                return;
                            }

                        }


                        //Get-FNumber
                        if (urlSegments[2] == commands.deviceGetMultiCommands[2])
                        {
                            if (urlSegments[3] == commands.current)
                            {
                                string responseString = device.FNumber.Value;
                                SendResponse(context, responseString, plain_ct);
                                return;
                            }
                            if (urlSegments[3] == commands.available)
                            {
                                string responseString = string.Join(Environment.NewLine, device.FNumber.Values);

                                SendResponse(context, responseString, plain_ct);
                                return;
                            }

                        }

                        //Get-IsoNumber
                        if (urlSegments[2] == commands.deviceGetMultiCommands[3])
                        {
                            if (urlSegments[3] == commands.current)
                            {
                                string responseString = device.IsoNumber.Value;
                                SendResponse(context, responseString, plain_ct);
                                return;
                            }
                            if (urlSegments[3] == commands.available)
                            {
                                string responseString = string.Join(Environment.NewLine, device.IsoNumber.Values);

                                SendResponse(context, responseString, plain_ct);
                                return;
                            }

                        }

                        //Get-FocusMode
                        if (urlSegments[2] == commands.deviceGetMultiCommands[4])
                        {
                            if (urlSegments[3] == commands.current)
                            {
                                string responseString = device.FocusMode.Value;
                                SendResponse(context, responseString, plain_ct);
                                return;
                            }
                            if (urlSegments[3] == commands.available)
                            {
                                string responseString = string.Join(Environment.NewLine, device.FocusMode.Values);

                                SendResponse(context, responseString, plain_ct);
                                return;
                            }


                        }

                        //Set-Shutterspeed
                        if (urlSegments[2] == commands.deviceSetCommands[0])
                        {

                            string value = deSanitizeShutterSpeed(context.Request.QueryString[commands.valuestring]);

                            int value_index = device.ShutterSpeed.Values.IndexOf(value);

                            if (deviceReady(device))
                            {
                                device.ShutterSpeed.Value = device.ShutterSpeed.Values[value_index];
                                SendResponse(context, ok_string, plain_ct);
                                return;
                            }

                            SendResponse(context, not_ready, plain_ct);
                            return;
                        }

                        //Set-FNumber
                        if (urlSegments[2] == commands.deviceSetCommands[1])
                        {

                            string value = context.Request.QueryString[commands.valuestring];

                            int value_index = device.FNumber.Values.IndexOf(value);

                            if (deviceReady(device))
                            {
                                device.FNumber.Value = device.FNumber.Values[value_index];
                                SendResponse(context, ok_string, plain_ct);
                                return;
                            }

                            SendResponse(context, not_ready, plain_ct);
                            return;
                        }

                        //Set-IsoNumber
                        if (urlSegments[2] == commands.deviceSetCommands[2])
                        {

                            string value = context.Request.QueryString[commands.valuestring];

                            int value_index = device.IsoNumber.Values.IndexOf(value);

                            if (deviceReady(device))
                            {
                                device.IsoNumber.Value = device.IsoNumber.Values[value_index];
                                SendResponse(context, ok_string, plain_ct);
                                return;
                            }

                            SendResponse(context, not_ready, plain_ct);
                            return;
                        }

                        //Set-FocusMode
                        if (urlSegments[2] == commands.deviceSetCommands[3])
                        {

                            string value = context.Request.QueryString[commands.valuestring];

                            int value_index = device.FocusMode.Values.IndexOf(value);

                            if (deviceReady(device))
                            {
                                device.FocusMode.Value = device.FocusMode.Values[value_index];
                                SendResponse(context, ok_string, plain_ct);
                                return;
                            }

                            SendResponse(context, not_ready, plain_ct);
                            return;
                        }

                        //Set-Focus
                        if (urlSegments[2] == commands.deviceSetCommands[4])
                        {

                            int value = Convert.ToInt16(context.Request.QueryString[commands.valuestring]);
                            value = Math.Max(-10000, Math.Min(10000, value));


                            if (deviceReady(device))
                            {
                                device.Focus(value);
                                SendResponse(context, ok_string, plain_ct);
                                return;
                            }

                            SendResponse(context, not_ready, plain_ct);
                            return;
                        }

                        //Set-FocusRoutine
                        if (urlSegments[2] == commands.deviceSetCommands[5])
                        {

                            int value = Convert.ToInt16(context.Request.QueryString[commands.valuestring]);
                            value = Math.Max(-100, Math.Min(100, value));

                            FocusRoutine(device, value);
                            SendResponse(context, ok_string, plain_ct);
                            return;
                        }
                    }


                    //Action-Capture
                    if (urlSegments[2] == commands.deviceActionCommands[0])
                    {

                        if (deviceReady(device))
                        {
                            device.CapturePhoto();
                            SendResponse(context, ok_string, plain_ct);
                            return;
                        }

                        SendResponse(context, not_ready, plain_ct);
                        return;
                    }


                    //Action-CaptureBulb
                    if (urlSegments[2] == commands.deviceActionCommands[1])
                    {

                        Int32 value = Convert.ToInt32(context.Request.QueryString[commands.valuestring]);
                        value = Math.Max(0, value);


                        if (deviceReady(device))
                        {

                            BulbCapture(device, value);

                            SendResponse(context, ok_string, plain_ct);
                            return;
                        }

                        SendResponse(context, not_ready, plain_ct);
                        return;
                    }

                    //Action-BulbStart
                    if (urlSegments[2] == commands.deviceActionCommands[2])
                    {

                        if (deviceReady(device))
                        {
                            if (device.ShutterSpeed.Value == "Bulb")
                            {
                                device.StartBulbMode();
                                SendResponse(context, ok_string, plain_ct);
                                return;
                            }

                        }

                        SendResponse(context, not_ready, plain_ct);
                        return;
                    }

                    //Action-BulbEnd
                    if (urlSegments[2] == commands.deviceActionCommands[3])
                    {


                        if (deviceReady(device))
                        {
                            device.EndBulbMode();
                            SendResponse(context, ok_string, plain_ct);
                            return;
                        }

                        SendResponse(context, not_ready, plain_ct);
                        return;
                    }

                    //Action-Lock
                    if (urlSegments[2] == commands.deviceActionCommands[4])
                    {

                        if (deviceReady(device))
                        {
                            device.LockCamera();
                            SendResponse(context, ok_string, plain_ct);
                            return;
                        }

                        SendResponse(context, not_ready, plain_ct);
                        return;
                    }

                    //Action-UnLock
                    if (urlSegments[2] == commands.deviceActionCommands[5])
                    {

                        if (deviceReady(device))
                        {
                            device.UnLockCamera();
                            SendResponse(context, ok_string, plain_ct);
                            return;
                        }

                        SendResponse(context, not_ready, plain_ct);
                        return;
                    }

                }


                SendErrorResponse(context, 404, "Not found");


            }

            catch (Exception e) { SendErrorResponse(context, 500, "Error<br>" + e); }



        }

        private void FocusRoutine(ICameraDevice device, int value)
        {

            // await Task.Run(() =>
            // {
            try
            {

                device.IsBusy = true;


                if (device.Manufacturer.StartsWith("Nikon"))
                {


                    string oldFocusMode = "MF"; //force back to MF for astro work
                                                //device.LockCamera();

                    if (deviceReady(device))
                    {
                        device.StartLiveView();
                        Thread.Sleep(500);

                        // oldFocusMode = device.FocusMode.Value;

                        device.FocusMode.Value = "MF"; //force update don't know if needed
                        Thread.Sleep(500);

                        device.FocusMode.Value = "AF-S";
                        Thread.Sleep(500);

                        device.Focus(value);
                        Thread.Sleep(500);

                        device.FocusMode.Value = oldFocusMode;
                        Thread.Sleep(500);
                        device.StopLiveView();

                        //    device.UnLockCamera();
                        device.IsBusy = false;
                    }
                }

                if (device.Manufacturer.StartsWith("Canon"))
                {


                    if (deviceReady(device))
                    {
                        device.StartLiveView();
                        Thread.Sleep(500);

                        device.Focus(value);
                        Thread.Sleep(500);

                        device.StopLiveView();

                        //    device.UnLockCamera();
                        device.IsBusy = false;
                    }
                }

            }
            catch (Exception e)
            {
                _logger.Log(e.ToString());

            }

            //  });

        }



        private async void BulbCapture(ICameraDevice device, Int32 time)
        {
            await Task.Run(() =>
            {

                if (deviceReady(device))
                {
                    device.StartBulbMode();

                    Thread.Sleep(time);
                    device.EndBulbMode();
                }

            });
        }

        private string sanitizeShutterSpeed(string s)
        {

            return s.Replace('/', '_');

        }

        private string deSanitizeShutterSpeed(string s)
        {

            return s.Replace('_', '/');

        }


        private bool deviceReady(ICameraDevice device)
        {
            /*
            int tries = 5;
            for (int i = 0; i < tries; i++)
            {
                if (!device.IsBusy) return true;
                Thread.Sleep(100);
            }

            return false;*/
            return true;

        }



        // Funzione di invio della risposta HTTP
        private void SendResponse(HttpListenerContext context, string responseString, string contentType)
        {
            context.Response.ContentType = contentType;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        // Funzione per inviare una risposta di errore con codice di stato
        private void SendErrorResponse(HttpListenerContext context, int statusCode, string errorMessage)
        {
            _logger.Log("Error " + statusCode + " " + errorMessage);

            context.Response.StatusCode = statusCode;
            context.Response.StatusDescription = GetStatusDescription(statusCode);
            context.Response.ContentType = "text/html";

            string errorResponse = $"<html><body><h1>Error {statusCode}</h1><p>{errorMessage}</p></body></html>";

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(errorResponse);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        // Funzione per ottenere una descrizione del codice di stato HTTP
        private string GetStatusDescription(int statusCode)
        {
            switch (statusCode)
            {
                case 400: return "Bad Request";
                case 404: return "Not Found";
                case 500: return "Internal Server Error";
                default: return "Unknown Error";
            }
        }


        // Funzione per sanificare un URL
        public static string SanitizeUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            // Rimuove eventuali spazi bianchi all'inizio e alla fine dell'URL
            url = url.Trim();

            // Verifica se l'URL è valido prima di procedere
            Uri uriResult;
            bool isValidUrl = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uriResult) && uriResult.IsWellFormedOriginalString();

            if (!isValidUrl)
            {
                throw new ArgumentException("L'URL fornito non è valido.");
            }

            // Decodifica eventuali sequenze di escape nell'URL
            url = Uri.UnescapeDataString(url);

            // Codifica di nuovo l'URL per garantire che tutti i caratteri speciali siano correttamente codificati
            Uri sanitizedUri = new Uri("http://temp" + url);  // Usa una base temporanea per permettere la creazione dell'oggetto Uri
            string sanitizedUrl = sanitizedUri.AbsolutePath;  // Ottieni solo il path (senza schema o host)

            // Rimuove eventuali doppie barre (//) e normalizza la formattazione
            sanitizedUrl = sanitizedUrl.Replace("//", "/").Trim('/');

            return sanitizedUrl;
        }


        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

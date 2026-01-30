using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text;
using System.Diagnostics; // Necesario para los Logs

#if ANDROID
using Android.Bluetooth;
using Java.Util;
#endif

namespace SaludPlus.ViewModels
{
    public partial class MainPageModel : ObservableObject
    {
#if ANDROID
        private BluetoothSocket _socket = null;
#endif
        private StringBuilder _dataBuffer = new StringBuilder();
        private CancellationTokenSource _cts;

        [ObservableProperty]
        private string heartRate = "--";

        [ObservableProperty]
        private string temperature = "--";

        [ObservableProperty]
        private bool isConnected = false;

        [ObservableProperty]
        private string connectionText = "Desconectado";

        [ObservableProperty]
        private bool isConnecting;

        [RelayCommand]
        private async Task ToggleConnection()
        {
            if (IsConnecting)
            {
                ConnectionText = "Cancelando...";
                await DesconectarBluetooth();
                return;
            }

            if (IsConnected)
                await DesconectarBluetooth();
            else
                await ConectarBluetooth();
        }

        private async Task ConectarBluetooth()
        {
            try
            {
                IsConnecting = true;
                ConnectionText = "Buscando PC...";
                Debug.WriteLine("--- INICIANDO CONEXIÓN ---");

                _cts = new CancellationTokenSource();
                
                var status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Bluetooth>();
                    if (status != PermissionStatus.Granted)
                    {
                        ConnectionText = "Permiso denegado";
                        IsConnecting = false;
                        return;
                    }
                }

                await IniciarEscuchaAndroid(_cts.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR FATAL AL CONECTAR: {ex.Message}");
                ConnectionText = "Error al iniciar";
                IsConnecting = false;
            }
        }

        private async Task DesconectarBluetooth()
        {
            Debug.WriteLine("--- DESCONECTANDO ---");
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            IsConnected = false;
            IsConnecting = false;
            ConnectionText = "Desconectado";
            HeartRate = "--";
            Temperature = "--";

            await Task.Run(() =>
            {
                try
                {
#if ANDROID
                    if (_socket != null)
                    {
                        _socket.Close();
                        _socket.Dispose();
                        _socket = null;
                    }
#endif
                }
                catch { }
            });
        }

        private async Task IniciarEscuchaAndroid(CancellationToken token)
        {
#if ANDROID
            BluetoothAdapter adapter = BluetoothAdapter.DefaultAdapter;
            var device = adapter?.BondedDevices?.FirstOrDefault(d => d.Name == "ALDO");

            if (device == null)
            {
                ConnectionText = "PC no encontrada";
                Debug.WriteLine("ERROR: No se encontró el dispositivo 'ALDO'");
                IsConnecting = false;
                return;
            }

            UUID uuid = UUID.FromString("00001101-0000-1000-8000-00805f9b34fb");

            try
            {
                _socket = device.CreateRfcommSocketToServiceRecord(uuid);
                var connectTask = _socket.ConnectAsync();
                var completedTask = await Task.WhenAny(connectTask, Task.Delay(-1, token));

                if (completedTask != connectTask) throw new OperationCanceledException();

                await connectTask;

                if (_socket.IsConnected)
                {
                    ConnectionText = "Conectado";
                    IsConnected = true;
                    IsConnecting = false;
                    _dataBuffer.Clear();
                    Debug.WriteLine("--- SOCKET CONECTADO: ESCUCHANDO DATOS ---");

                    _ = Task.Run(async () => await LeerDatos(token), token);
                }
            }
            catch (OperationCanceledException)
            {
                if (IsConnecting) ConnectionText = "Tiempo agotado";
                IsConnecting = false;
                _socket?.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR DE SOCKET: {ex.Message}");
                ConnectionText = "Fallo de conexión";
                IsConnected = false;
                IsConnecting = false;
                _socket?.Close();
            }
#endif
        }

        private async Task LeerDatos(CancellationToken token)
        {
#if ANDROID
            var stream = _socket.InputStream;
            byte[] buffer = new byte[1024];

            while (!token.IsCancellationRequested && _socket != null && _socket.IsConnected)
            {
                try
                {
                    if (stream.IsDataAvailable())
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                        if (bytesRead > 0)
                        {
                            string chunk = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                            _dataBuffer.Append(chunk);
                            ProcesarBuffer();
                        }
                    }
                    else
                    {
                        await Task.Delay(5000, token);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR LECTURA: {ex.Message}");
                    break;
                }
            }

            if (IsConnected && !token.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(async () => await DesconectarBluetooth());
            }
#endif
        }

        private void ProcesarBuffer()
        {
            string datosActuales = _dataBuffer.ToString();
            if (datosActuales.Contains("\n"))
            {

                string[] lineas = datosActuales.Split('\n');

                for (int i = 0; i < lineas.Length - 1; i++)
                {
                    string lineaLimpia = lineas[i].Trim();
                    if (!string.IsNullOrEmpty(lineaLimpia))
                    {
                        if (!IsConnected) return;
                        MainThread.BeginInvokeOnMainThread(() => InterpretarLinea(lineaLimpia));
                    }
                }

                _dataBuffer.Clear();
                _dataBuffer.Append(lineas[lineas.Length - 1]);
            }
        }

        private void InterpretarLinea(string linea)
        {
            if (!IsConnected) return;

            
            try
            {
                var partes = linea.Split(',');

                if (partes.Length >= 2)
                {
                    string temp = partes[0].Trim();
                    string bpm = partes[1].Trim();

                    Temperature = temp;
                    HeartRate = bpm;
                }
                else
                {
                    Debug.WriteLine($"[FALLO PARSEO]: La línea no tiene comas o suficientes partes. Partes: {partes.Length}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EXCEPCIÓN PARSEO]: {ex.Message}");
            }
        }
    }
}
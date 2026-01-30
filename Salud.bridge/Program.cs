using System.IO.Ports;
using System.Net;
using System.Net.Sockets;

// CONFIGURACIÓN
string puertoCom = "COM3"; // El que viene de Proteus vía VSPD
int puertoTcp = 8080;      // El que escuchará el teléfono

try
{
    using SerialPort sp = new SerialPort(puertoCom, 9600);
    sp.Open();
    Console.WriteLine($"[SERIAL] Leyendo {puertoCom}...");

    TcpListener server = new TcpListener(IPAddress.Any, puertoTcp);
    server.Start();
    Console.WriteLine($"[TCP] Servidor listo en puerto {puertoTcp}. Esperando App...");

    while (true)
    {
        using TcpClient client = server.AcceptTcpClient();
        Console.WriteLine("¡Teléfono conectado por USB!");
        using NetworkStream netStream = client.GetStream();
        using StreamWriter writer = new StreamWriter(netStream) { AutoFlush = true };

        while (client.Connected)
        {
            if (sp.BytesToRead > 0)
            {
                string data = sp.ReadLine(); // Recibe de Proteus
                writer.WriteLine(data);      // Envía al Teléfono
                Console.WriteLine("Dato enviado: " + data);
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("ERROR: " + ex.Message);
    Console.ReadLine();
}
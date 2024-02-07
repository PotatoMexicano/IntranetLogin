using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Intranet;

class Program
{
    private static readonly string login = Environment.UserName;

    private static readonly HttpClient client = new HttpClient();
    private static readonly IPAddress dnsAllowed = IPAddress.Parse("192.168.245.xx");
    private static readonly string urlEnpoint = "http://192.168.245.xx/auth";

    static void Main(string[] args)
    {

        var hasConnection = CheckConnection();

        if (hasConnection)
        {
            Console.WriteLine("Usuário já conectado !");
            Environment.Exit(0);
        }

        // Vamos fazer a requisição.
        var request = new HttpRequestMessage(HttpMethod.Post, urlEnpoint)
        {
            Content = GenerateContent()
        };

        var response = client.Send(request);

        switch (response.StatusCode)
        {
            case HttpStatusCode.InternalServerError:
                Console.WriteLine("Recusado pelo servidor !");
                Console.ReadLine();
                Environment.Exit(0);
            break;

            case HttpStatusCode.OK:
                if (response.Content.ReadAsStringAsync().Result.Contains("bemvindo"))
                {
                    Console.WriteLine($"Usuário {login} conectado !");
                    Console.WriteLine("Sucesso !");
                    Environment.Exit(0);
                }

                if (response.Content.ReadAsStringAsync().Result.Contains("index"))
                {
                    Console.WriteLine("Informações incorretas !");
                    Console.ReadLine();
                    Environment.Exit(0);
                }
            break;
        }
    }

    private static bool CheckConnection()
    {
        return new Ping().Send("www.google.com").Status == IPStatus.Success;
    }
    
    private static MultipartFormDataContent GenerateContent()
    {
        var content = new MultipartFormDataContent();

        var ipAddress = GetLocalIpAddress();
        var macAddress = GetLocalMacAddress() ?? throw new Exception("Não identificado MAC-ADDRESS!");

        content.Add(new StringContent(login), "login");
        content.Add(new StringContent(login), "senha");
        content.Add(new StringContent("0"), "mac_liberado");
        content.Add(new StringContent(ipAddress), "ip");
        content.Add(new StringContent(macAddress), "mac");

        return content;
    }

    private static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach(var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("Sem endereço IPV4 encontrado na máquina!");
    }

    private static string? GetLocalMacAddress()
    {
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach(NetworkInterface adapter in interfaces)
        {

            Console.Write($"Adaptador {adapter.Name}: ");

            if (adapter.OperationalStatus != OperationalStatus.Up)
            {
                Console.WriteLine($" Não está ligado.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(adapter.GetPhysicalAddress().ToString()))
            {
                Console.WriteLine($" Não tem MAC-ADDRESS.");
                continue;
            }

            if (!adapter.GetIPProperties().DnsAddresses.ToList().Any(dns => IPAddress.Equals(dns, dnsAllowed)))
            {
                Console.WriteLine(" Não possui DNS autorizado.");
            }

            Console.WriteLine(" OK");

            return adapter.GetPhysicalAddress().ToString();

        }

        return null;
    }

}

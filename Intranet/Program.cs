using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using CommandLine;

namespace Intranet;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly IPAddress dnsAllowed = IPAddress.Parse("192.168.245.13");
    private static readonly IPAddress maskAllowed = IPAddress.Parse("255.255.255.0");
    private static readonly string urlEnpoint = "http://192.168.245.11/auth.php";

    static void Main(string[] args)
    {

        string login = "";
        string senha = "";

        Parser.Default.ParseArguments<CommandLineOptions>(args)
        .WithParsed<CommandLineOptions>(o => {
            login = string.IsNullOrEmpty(o.Login) ? Environment.UserName : o.Login.ToString();
            senha = string.IsNullOrEmpty(o.Senha) ? Environment.UserName : o.Senha.ToString();
        });

        // Vamos fazer a requisição.
        var request = new HttpRequestMessage(HttpMethod.Post, urlEnpoint)
        {
            Content = GenerateContent(login, senha)
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
    
    private static MultipartFormDataContent GenerateContent(string login, string senha)
    {
        var content = new MultipartFormDataContent();

        var ipAddress = GetLocalIpAddress();
        var macAddress = GetLocalMacAddress() ?? throw new Exception("Não identificado MAC-ADDRESS!");

        content.Add(new StringContent(login), "login");
        content.Add(new StringContent(senha), "senha");
        content.Add(new StringContent("0"), "mac_liberado");
        content.Add(new StringContent(ipAddress), "ip");
        content.Add(new StringContent(macAddress), "mac");

        return content;
    }

    public static string FormatMacAddress(string macAddress)
    {
        if (macAddress.Length != 12)
        {
            throw new ArgumentException("MAC-ADDRESS no formato incompatível");
        }

        string formattedMacAddress = "";
        for (int i = 0; i < macAddress.Length; i += 2)
        {
            formattedMacAddress += macAddress.Substring(i, 2);

            if (i < macAddress.Length - 2)
            {
                formattedMacAddress += ":";
            }
        }

        return formattedMacAddress;
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

        var listInterfaces = interfaces.Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback).ToList();

        foreach(NetworkInterface adapter in listInterfaces)
        {
            if (!adapter.GetIPProperties().UnicastAddresses.Where(x => IPAddress.Equals(x.IPv4Mask, maskAllowed)).Any())
            {
                continue;
            }

            if (adapter.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(adapter.GetPhysicalAddress().ToString()))
            {
                continue;
            }

            if (!adapter.GetIPProperties().DnsAddresses.ToList().Any(dns => IPAddress.Equals(dns, dnsAllowed)))
            {
                continue;
            }
            
            Console.WriteLine($"Adaptador {adapter.Name}: OK");

            return FormatMacAddress(adapter.GetPhysicalAddress().ToString());

        }

        return null;
    }

    private static void OpenPrograms(string user)
    {
        Process.Start(@$"C:\Users\{user}\AppData\Roaming\Spotify\Spotify.exe");
    }
}

class CommandLineOptions
{
    [Option('l', "login", Required = false, HelpText = "Nome do usuário")]
    public string? Login {get; set;}

    [Option('p', "senha", Required = false, HelpText = "Senha do usuário")]
    public string? Senha {get; set;}
}
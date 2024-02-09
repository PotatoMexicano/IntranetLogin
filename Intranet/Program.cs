using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using CommandLine;

namespace Intranet;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly IPAddress dnsAllowed = IPAddress.Parse("192.168.245.13");
    private static readonly string urlEnpoint = "http://192.168.245.11/auth";

    static void Main(string[] args)
    {

        string login = "";
        string senha = "";

        Parser.Default.ParseArguments<CommandLineOptions>(args)
        .WithParsed<CommandLineOptions>(o => {
            login = string.IsNullOrEmpty(o.login) ? Environment.UserName : o.login.ToString();
            senha = string.IsNullOrEmpty(o.senha) ? Environment.UserName : o.senha.ToString();
        });

        var hasConnection = CheckConnection();

        if (hasConnection)
        {
            Console.WriteLine("Usuário já conectado !");
            Environment.Exit(0);
        }

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

    private static bool CheckConnection()
    {
        return new Ping().Send("www.google.com").Status == IPStatus.Success;
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

class CommandLineOptions
{
    [Option('l', "login", Required = false, HelpText = "Nome do usuário")]
    public string login {get; set;}

    [Option('p', "senha", Required = false, HelpText = "Senha do usuário")]
    public string senha {get; set;}
}
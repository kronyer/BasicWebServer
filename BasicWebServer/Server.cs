using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BasicWebServer;

public static class Server
{
    private static HttpListener _listener;
    public static int maxSimultaneousConnections = 20;
    private static Semaphore sem = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);
    
    private static List<IPAddress> GetLocalHostIPs()
    {
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        List<IPAddress> ret = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();
        return ret;
    }

    private static HttpListener InitializeListener(List<IPAddress> localHostIPs)
    {
        HttpListener listener = new HttpListener();
        localHostIPs.ForEach(ip =>
        {
            Console.WriteLine($"Listening on {ip}.");
            listener.Prefixes.Add($"http://{ip}:8611/");
        });
        return listener;
    }


    private static void Start(HttpListener listener)
    {
        listener.Start();
        Task.Run(() => RunServer(listener));
    }

    private static void RunServer(HttpListener listener)
    {
        while (true)
        {
            sem.WaitOne();
            StartConnectionListener(listener);
        }
    }

    private static async void StartConnectionListener(HttpListener listener)
    {
        HttpListenerContext context = await listener.GetContextAsync();
        //we need to encode with html
        string response = @"<html><head><meta http-equiv='content-type' content='text/html; charset=utf-8'/>
                          </head>Hello Browser!</html>";
        byte[] encoded = Encoding.UTF8.GetBytes(response);
        context.Response.ContentLength64 = encoded.Length;
        context.Response.OutputStream.Write(encoded, 0, encoded.Length);
        context.Response.OutputStream.Close();
        sem.Release();
        Log(context.Request);

    }

    public static void Start()
    {
        List<IPAddress> localHostIPs = GetLocalHostIPs();
        HttpListener listener = InitializeListener(localHostIPs);
        Start(listener);
    }
    
    public static void Log(HttpListenerRequest request)
    {
        string[] parts = request.Url.AbsoluteUri.Split('/');
        string path = string.Join("/", parts.Skip(3));
        Console.WriteLine(request.RemoteEndPoint + " " + request.HttpMethod + " /" + path);
    }
}


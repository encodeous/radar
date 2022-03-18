using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Web;
using System.Xml.Serialization;
using AutoMapper;
using clockwork;
using clockwork.Attributes;
using Microsoft.EntityFrameworkCore;
using SpeedTest;
using SpeedTest.Models;

public class Program
{
    static IMapper mp;
    private static List<string> cities;
    private static SpeedTestClient client;
    static int index = 0;
    public static async Task Main(string[] args)
    {
        var tmp = new RadarContext();
        await tmp.Database.MigrateAsync();
        await tmp.DisposeAsync();

        mp = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TestServer, Server>();
            cfg.ReplaceMemberName("url", "Url");
            cfg.ReplaceMemberName("lat", "Latitude");
            cfg.ReplaceMemberName("lon", "Longitude");
            cfg.ReplaceMemberName("distance", "Distance");
            cfg.ReplaceMemberName("name", "Name");
            cfg.ReplaceMemberName("country", "Country");
            cfg.ReplaceMemberName("sponsor", "Sponsor");
            cfg.ReplaceMemberName("id", "Id");
            cfg.ReplaceMemberName("host", "Host");
        }).CreateMapper();

        cities = new List<string>();

        cities.Add("New York");
        cities.Add("Toronto");
        cities.Add("Vancouver");
        cities.Add("Frankfurt");
        cities.Add("London");
        cities.Add("Paris");
        cities.Add("Las Vegas");
        cities.Add("Hong Kong");

        client = new SpeedTestClient();

        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();

        app.MapGet("/", async (g) =>
        {
            using var ctx = new RadarContext();
            await g.Response.WriteAsJsonAsync(ctx.Results.AsAsyncEnumerable());
        });

        Clockwork.Wind();

        app.Lifetime.ApplicationStopping.Register(() =>
        {
            Clockwork.Default.StopSchedulerAsync().GetAwaiter().GetResult();
        });

        app.Run();

    }
    
    [Future(0, 1000 * 60 * 7)]
    public static async Task RunSpeedTests()
    {
        using var ctx = new RadarContext();
        var res = await RunTestFor(cities[index]);
        ctx.Results.Add(res);
        await ctx.SaveChangesAsync();
        index = (index + 1) % cities.Count;
        Console.WriteLine($"Test Completed: {res}");
    }

    static async ValueTask<TestResult> RunTestFor(string city)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMinutes(5));
        var server = await QueryServer(city);
        try
        {
            var res = Task.Run(async () =>
            {
                var dl = client.TestDownloadSpeed(server);
                var ul = client.TestUploadSpeed(server);
                var lt = client.TestServerLatency(server);
                return new TestResult(server.Country, server.Latitude, server.Longitude, server.Name, 
                    lt, ul, dl, true, DateTime.UtcNow);
            }, cts.Token);
            return await res;
        }
        catch
        {
            // ignored
        }

        return new TestResult(server.Country, server.Latitude, server.Longitude, 
            server.Name, 10000, 0, 0, false, DateTime.UtcNow);
    }
    
    static async ValueTask<Server> QueryServer(string query)
    {
        var hc = new HttpClient();
        var tServer = JsonSerializer.Deserialize<TestServer[]>(await hc.GetStringAsync(
            $"https://www.speedtest.net/api/js/servers?engine=js&search={query}&https_functional=true&limit=1"))[0];
        return mp.Map<Server>(tServer);
    }
}
public class RadarContext : DbContext
{
    public DbSet<TestResult> Results { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite(@"Data Source=Records.db");
}
public class TestServer
{
    public string url { get; set; }
    public string lat { get; set; }
    public string lon { get; set; }
    public int distance { get; set; }
    public string name { get; set; }
    public string country { get; set; }
    public string cc { get; set; }
    public string sponsor { get; set; }
    public string id { get; set; }
    public int preferred { get; set; }
    public int https_functional { get; set; }
    public string host { get; set; }
}
public record TestResult(string Country, double Lat, double Long, string Name, double Ping, double Upload, double Download, bool HasSucceeded, [property: Key] DateTime RanAt);
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleUI
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            BuildConfig(builder);

            IHost host = Host.CreateDefaultBuilder()
                             .ConfigureServices((context, services) => { ConfigureServices(services); })
                             .Build();

            var instance = ActivatorUtilities.CreateInstance<Tetris>(host.Services);

            if (instance.SetupConsole(120, 40)) {
                instance.Start();
            } else {
                Console.WriteLine("Error during console construction.");
                Console.WriteLine("Press eny key to exit.");

                Console.ReadKey();
            }
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            /*services.Configure<ScreenOptions>(o => {
                o.Width = 120;
                o.Height = 40;
            });*/

            services.AddSingleton<Tetris>();
        }

        private static void BuildConfig(ConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", false, true)
                   .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true, true)
                   .AddEnvironmentVariables();
        }
    }
}
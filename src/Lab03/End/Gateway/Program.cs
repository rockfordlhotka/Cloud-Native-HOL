using Gateway.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gateway
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      // Add services to the container.
      builder.Services.AddRazorPages();
      builder.Services.AddServerSideBlazor();
      builder.Services.AddSingleton<WeatherForecastService>();

      builder.Services.AddSingleton<Services.IWorkInProgress>((e) => new Services.WorkInProgress());
      builder.Services.AddHostedService<Services.SandwichmakerListener>();
      builder.Services.AddSingleton<Services.ISandwichRequestor>((e) =>
          new Services.SandwichRequestor(
              e.GetService<IConfiguration>(),
              e.GetService<Services.IWorkInProgress>()));

      var app = builder.Build();

      // Configure the HTTP request pipeline.
      if (!app.Environment.IsDevelopment())
      {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      app.UseHttpsRedirection();

      app.UseStaticFiles();

      app.UseRouting();

      app.MapBlazorHub();
      app.MapFallbackToPage("/_Host");

      app.Run();
    }
  }
}
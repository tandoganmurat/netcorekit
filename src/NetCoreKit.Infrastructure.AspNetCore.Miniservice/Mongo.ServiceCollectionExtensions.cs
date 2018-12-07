using System;
using BeatPulse.Core;
using BeatPulse.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreKit.Domain;
using NetCoreKit.Infrastructure.AspNetCore.CleanArch;
using NetCoreKit.Infrastructure.AspNetCore.OpenApi;
using NetCoreKit.Infrastructure.Features;
using NetCoreKit.Infrastructure.Mongo;

namespace NetCoreKit.Infrastructure.AspNetCore.Miniservice
{
  public static partial class ServiceCollectionExtensions
  {
    public static IServiceCollection AddMongoMiniService(this IServiceCollection services,
      Action<BeatPulseContext> beatPulseCtx = null,
      Action<IServiceCollection> preDbWorkHook = null,
      Action<IServiceCollection, IServiceProvider> postDbWorkHook = null)
    {
      services.AddFeatureToggle();

      using (var scope = services.BuildServiceProvider().GetService<IServiceScopeFactory>().CreateScope())
      {
        var svcProvider = scope.ServiceProvider;
        var config = svcProvider.GetRequiredService<IConfiguration>();
        var env = svcProvider.GetRequiredService<IHostingEnvironment>();
        var feature = svcProvider.GetRequiredService<IFeature>();

        preDbWorkHook?.Invoke(services);

        if (feature.IsEnabled("Mongo"))
        {
          if (feature.IsEnabled("EfCore"))
            throw new Exception("Please turn off EfCore settings.");
          services.AddMongoDb();
        }

        postDbWorkHook?.Invoke(services, svcProvider);

        services.AddRestClientCore();

        services.AddSingleton<IDomainEventBus, MemoryDomainEventBus>();

        if (feature.IsEnabled("CleanArch"))
          services.AddCleanArch(config.LoadFullAssemblies());

        services.AddCacheCore();

        if (feature.IsEnabled("ApiVersion"))
          services.AddApiVersionCore(config);

        services.AddMvcCore(config);

        services.AddDetailExceptionCore();

        if (feature.IsEnabled("AuthN"))
          services.AddAuthNCore(config, env);

        if (feature.IsEnabled("OpenApi"))
          services.AddOpenApiCore(config, feature);

        services.AddCorsCore();

        services.AddHeaderForwardCore(env);

        if (feature.IsEnabled("OpenApi:Profiler"))
          services.AddApiProfilerCore();

        services.AddBeatPulse(beatPulseCtx);

        if (feature.IsEnabled("HealthUI"))
          services.AddBeatPulseUI();
      }

      return services;
    }
  }
}
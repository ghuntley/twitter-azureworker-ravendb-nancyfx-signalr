using System;
using Autofac;
using Autofac.Core;
using BoxKite.Twitter.Models;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Serilog;
using WeWillRideWithYouWeb.Hubs;

namespace WeWillRideWithYouWeb
{
    public class Bootstrapper : AutofacNancyBootstrapper
    {
        /// <summary>
        /// No registrations should be performed in here, however you may resolve things that are needed during application startup.
        /// </summary>
        protected override void ApplicationStartup(ILifetimeScope container, IPipelines pipelines)
        {
        }

        /// <summary>
        /// Perform registration that should have an application lifetime
        /// </summary>
        protected override void ConfigureApplicationContainer(ILifetimeScope existingContainer)
        {
            var builder = new ContainerBuilder();

            // configure serilog
            var logger = new LoggerConfiguration()
                .Destructure.UsingAttributes()
                .MinimumLevel.Debug()
                .WriteTo.Trace()
                .WriteTo.Logentries(AppSettings.LogEntriesToken)
                .CreateLogger();

            // configure our ILogger as the static Log methods if they are ever used.
            Log.Logger = logger;

            builder.Register(x => logger)
                .As<ILogger>()
                .SingleInstance();

            // configure RavenDB
            var store = new DocumentStore {ConnectionStringName = "RavenDB"}.Initialize();

            builder.Register(x => store)
                .As<IDocumentStore>()
                .SingleInstance();

            // create our twitter hub immediately instead of on first use.
            builder.RegisterInstance(new TwitterHub(store));

            builder.Build();
        }

        /// <summary>
        /// Perform registrations that should have a request lifetime
        /// </summary>
        protected override void ConfigureRequestContainer(ILifetimeScope container, NancyContext context)
        {
        }

        /// <summary>
        /// No registrations should be performed in here, however you may resolve things that are needed during request startup.
        /// </summary>
        protected override void RequestStartup(ILifetimeScope container, IPipelines pipelines, NancyContext context)
        {
        }
    }
}
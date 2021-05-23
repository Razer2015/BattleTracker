using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tracker.DAL;
using Tracker.Services;

namespace Tracker
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tracker", Version = "v1" });
            });

            // MySQL Connection string
            services.AddDbContext<TrackerContext>(options => options
                    .UseMySql(Variables.MYSQL_CONNECTION_STRING, ServerVersion.AutoDetect(Variables.MYSQL_CONNECTION_STRING)));

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Variables.REDIS_CONFIGURATION ?? "127.0.0.1,abortConnect=false,connectTimeout=500";
                options.InstanceName = Variables.REDIS_INSTANCE ?? "master";
            });

            services.AddScoped<IDiscordService, DiscordService>();
            services.AddScoped<IPersonaService, PersonaService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tracker v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }
    }
}

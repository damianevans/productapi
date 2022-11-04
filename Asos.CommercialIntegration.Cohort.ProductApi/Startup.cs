using Asos.CommercialIntegration.Cohort.ProductApi.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;
using Newtonsoft.Json;
using System.IO;
using Asos.CommercialIntegration.Cohort.ProductApi.Data;

namespace Asos.CommercialIntegration.Cohort.ProductApi
{
    public class Startup
    {
        public IConfiguration Configuration { get; private set; }
        public Startup(IConfigurationRoot configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection builderServices)
        {
            builderServices.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builderServices.AddEndpointsApiExplorer();
            builderServices.AddSwaggerGen();
            builderServices.AddMemoryCache();
            builderServices.AddScoped<IProductsRepository, ProductsRepository>();
        }

        public void Configure(WebApplication app, IWebHostEnvironment env, IMemoryCache cache)
        {


            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //doesn't work in docker because of SSL termination
            //app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();
            
            app.Run();
        }


    }
}

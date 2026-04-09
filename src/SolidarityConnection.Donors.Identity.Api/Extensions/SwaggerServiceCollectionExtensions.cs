using Microsoft.OpenApi.Models;

namespace SolidarityConnection.Donors.Identity.Api.Extensions
{
    public static class SwaggerServiceCollectionExtensions
    {
        public static IServiceCollection AddFiapCloudGamesSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "FIAP Cloud Games Users API",
                    Version = "v1",
                    Description = "API para gerenciamento usuÃ¡rios, login e biblioteca de usuÃ¡rios",
                    Contact = new OpenApiContact
                    {
                        Name = "FIAP Cloud Games Team",
                        Email = "deiserech@outlook.com"
                    }
                });

                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Informe o token JWT no formato: Bearer {seu token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            return services;
        }
    }
}


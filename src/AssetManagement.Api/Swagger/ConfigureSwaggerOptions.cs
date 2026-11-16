using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AssetManagement.Api.Swagger;

/// <summary>Creates one Swagger document per API version and adds the JWT "Authorize" button.</summary>
public sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => _provider = provider;

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "Asset Management API",
                Version = description.ApiVersion.ToString(),
                Description = "Track survey scanners, drones and laser trackers: checkout/return workflow, "
                    + "maintenance records and role-based access.",
            });
        }

        const string scheme = "Bearer";
        options.AddSecurityDefinition(scheme, new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the accessToken returned by POST /api/v1/auth/login.",
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = scheme },
            }] = Array.Empty<string>(),
        });

        var xmlFile = Path.Combine(AppContext.BaseDirectory, "AssetManagement.Api.xml");
        if (File.Exists(xmlFile))
        {
            options.IncludeXmlComments(xmlFile);
        }
    }
}

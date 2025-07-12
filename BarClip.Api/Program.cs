using BarClip.Api.Hubs;
using BarClip.Core;
using BarClip.Models.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Identity.Web;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.RegisterCoreServices(builder.Configuration);
builder.Services.Configure<OnnxModelOptions>(
    builder.Configuration.GetSection("OnnxModelOptions"));
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream", "application/json"]);
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy
            .WithOrigins("https://www.barclip.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition");
    });
});
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.TokenValidationParameters.NameClaimType = "nameidentifier";
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/videoStatus") ||
                     path.StartsWithSegments("/videoStatus/negotiate")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    },
    options => { builder.Configuration.Bind("AzureAd", options); });

var app = builder.Build();

// Configure the HTTP request pipeline.

    app.UseSwagger();
    app.UseSwaggerUI();

app.UseRouting();
app.UseCors("AllowSpecificOrigin");




app.UseHttpsRedirection(); // Comment out for development

app.UseAuthentication();
app.UseAuthorization();
app.MapHub<VideoStatusHub>("/videoStatus");

app.MapControllers();

app.Run();

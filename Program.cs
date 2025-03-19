//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.IdentityModel.Tokens;
//using System.Text;
//using WebApiTemplate.Repository.Database;
//using WebApiTemplate.Constants;
//using WebApiTemplate.Repository.Database.Entities;
//using System.Security.Claims;

//var builder = WebApplication.CreateBuilder(args);

//// ✅ 1. Add PostgreSQL Database Context
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//builder.Services.AddDbContext<WebApiTemplateDbContext>(options =>
//    options.UseNpgsql(connectionString));

//// ✅ 2. Add Identity Services
//builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
//    .AddEntityFrameworkStores<WebApiTemplateDbContext>()
//    .AddDefaultTokenProviders();

//// ✅ 3. Configure JWT Authentication
//var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//var jwtkey = jwtSettings["Key"];
//if (string.IsNullOrEmpty(jwtkey))
//{
//    throw new Exception("jwt micssing");
//}
//var key = Encoding.UTF8.GetBytes(jwtkey);
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.RequireHttpsMetadata = false;
//    options.SaveToken = true;
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuerSigningKey = true,
//        IssuerSigningKey = new SymmetricSecurityKey(key),
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidIssuer = jwtSettings["Issuer"],
//        ValidAudience = jwtSettings["Audience"],
//        RoleClaimType=ClaimTypes.Role,
//        NameClaimType=ClaimTypes.Name,
//        ValidateLifetime = true,
//        ClockSkew = TimeSpan.Zero
//    };
//});
//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
//});
//// ✅ 4. Add Controllers & Swagger
//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//// ✅ 5. Configure Role Seeding
//async Task SeedRoles(IServiceProvider serviceProvider)
//{
//    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

//    string[] roleNames = { UserRoles.Admin, UserRoles.Author, UserRoles.StandardUser };
//    foreach (var role in roleNames)
//    {
//        if (!await roleManager.RoleExistsAsync(role))
//        {
//            await roleManager.CreateAsync(new IdentityRole(role));
//        }
//    }
//}

//var app = builder.Build();

//// ✅ 6. Apply Middleware
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllers();

//// ✅ 7. Seed Initial Roles on Startup
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    await SeedRoles(services);
//}

//// ✅ 8. Run Application
//app.Run();


#region References
using System;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Helpers;
using WebApiTemplate.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using WebApiTemplate.Data;
using WebApiTemplate.DTO;
using WebApiTemplate.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;


//using WebApiTemplate.Repository.Database;
//using WebApiTemplate.Repository.DatabaseOperation.Implementation;
//using WebApiTemplate.Repository.DatabaseOperation.Interface;
//using WebApiTemplate.Service;
//using WebApiTemplate.Service.Interface;
#endregion

var builder = WebApplication.CreateBuilder(args);

// Configure database context
//builder.Services.AddDbContext<WenApiTemplateDbContext>(
// options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


// 🔹 Configure Swagger to support JWT Authentication
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApiTemplate", Version = "v1" });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}' (without quotes)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

//builder.Services.AddDbContext<WebApiTemplate.Data.ApplicationDbContext>(options =>
//    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
//    new MySqlServerVersion(new Version(8, 0, 21))));

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
//    npgsqlOptions => npgsqlOptions.SetPostgresVersion(new Version(8, 0, 11))));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(GetPostgresConnectionString()));


builder.Services.AddValidatorsFromAssemblyContaining<UserRegisterValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<GenreValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ReviewValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<BookFilterValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<BookValidator>();


// Register Authentication Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<GenreService>();
builder.Services.AddScoped<ReviewService>();


// Add JWT Authentication

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"])),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"]
        };
    });



//Add Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Add services to the container.
builder.Services.AddControllers();

//inject Service layer
//builder.Services.AddScoped<IProductService, ProductService>();

//inject Data Access Layer - Repository
//builder.Services.AddScoped<IProductOperation, ProductOperation>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
// Auto Apply Migrations (for development only)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

static string GetPostgresConnectionString()

{
    var host = Environment.GetEnvironmentVariable("DB_HOST");
    var database = Environment.GetEnvironmentVariable("DB_NAME");
    var username = Environment.GetEnvironmentVariable("DB_USER");
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
    var port = Environment.GetEnvironmentVariable("DB_PORT");
    return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();

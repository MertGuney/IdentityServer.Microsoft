namespace IdentityServer.Persistence;

public static class ServiceCollectionExtensions
{
    public static void AddPersistenceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext(configuration);
        services.AddIdentity();
        services.AddServices();
        services.AddRepositories();
        services.AddEventDispatcher();
    }

    private static void AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
    }

    private static void AddEventDispatcher(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserCodesRepository, UserCodesRepository>();
    }

    private static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<ICodeService, CodeService>();
    }

    private static void AddIdentity(this IServiceCollection services)
    {
        services.AddIdentity<User, Role>(opts =>
        {
            opts.User.RequireUniqueEmail = true;

            opts.Password.RequiredLength = 8;
            opts.Password.RequireDigit = false;

            opts.Lockout.AllowedForNewUsers = true;
            opts.Lockout.MaxFailedAccessAttempts = 3;
            opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        })
            .AddPasswordValidator<IdentityPasswordValidator>()
            .AddUserValidator<IdentityUserValidator>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
    }

    public static void AddProductionIdentity(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        // services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromHours(12));

        var migrationsAssembly = "IdentityServer.Persistence";

        // Password Token Lifetime
        services.Configure<DataProtectionTokenProviderOptions>(opts => opts.TokenLifespan = TimeSpan.FromHours(2));

        var identityBuilder = services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;
            options.Events.RaiseInformationEvents = true;

            options.EmitStaticAudienceClaim = true;
        })
            .AddConfigurationStore(opts =>
            {
                opts.ConfigureDbContext =
                    b => b.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddOperationalStore(opts =>
            {
                opts.ConfigureDbContext =
                    b => b.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddAspNetIdentity<User>();

        identityBuilder.AddProfileService<IdentityProfileService>();
        identityBuilder.AddExtensionGrantValidator<TokenExchangeExtensionGrantValidator>();
        identityBuilder.AddResourceOwnerValidator<IdentityResourceOwnerPasswordValidator>();

        var rsa = new RSAKeyService(webHostEnvironment, TimeSpan.FromDays(30));

        services.AddSingleton(provider => rsa);

        identityBuilder.AddSigningCredential(rsa.GetKey(), RsaSigningAlgorithm.RS512);
    }
}

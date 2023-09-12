namespace IdentityServer.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServices();

        services.AddLocalApiAuthentication();
    }

    private static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<ITFAService, TFAService>()
                .AddScoped<IUserService, UserService>()
                .AddScoped<IAuthService, AuthService>()
                .AddScoped<IMailService, MailService>()
                .AddScoped<IQRCodeService, QRCodeService>();
    }

    public static void AddDevelopmentIdentity(this IServiceCollection services)
    {
        services.Configure<DataProtectionTokenProviderOptions>(opts => opts.TokenLifespan = TimeSpan.FromHours(2));

        var identityBuilder = services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;
            options.Events.RaiseInformationEvents = true;

            options.EmitStaticAudienceClaim = true;
        })
            .AddInMemoryIdentityResources(Config.IdentityResources())
            .AddInMemoryApiResources(Config.ApiResources())
            .AddInMemoryApiScopes(Config.ApiScopes())
            .AddInMemoryClients(Config.Clients())
            .AddAspNetIdentity<User>();

        identityBuilder.AddProfileService<IdentityProfileService>();
        identityBuilder.AddExtensionGrantValidator<TokenExchangeExtensionGrantValidator>();
        identityBuilder.AddResourceOwnerValidator<IdentityResourceOwnerPasswordValidator>();
        identityBuilder.AddDeveloperSigningCredential();
    }
}

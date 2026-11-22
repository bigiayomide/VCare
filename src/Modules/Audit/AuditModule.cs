using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using VCare.SharedKernel.Auditing;

namespace VCare.Modules.Audit;

public static class AuditModule
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuditDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", AuditEntry.Schema)));
        services.TryAddSingleton(TimeProvider.System);
        // Every module's DbContext adds this, so each change is audited in its own save.
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<AuditTrail>();
        return services;
    }

    /// <summary>
    /// Records each successful read of a resident's record: any GET whose route
    /// names a patientId. Runs after the response, so a failure to record is
    /// logged rather than turning a good read into an error.
    /// </summary>
    public static IApplicationBuilder UseResidentRecordAudit(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            await next(context);
            if (!HttpMethods.IsGet(context.Request.Method)
                || context.Response.StatusCode is < 200 or >= 300
                || context.User.Identity?.IsAuthenticated != true
                || !Guid.TryParse(context.GetRouteValue("patientId") as string, out var residentId))
            {
                return;
            }
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? context.Request.Path.Value ?? "";
            try
            {
                await context.RequestServices.GetRequiredService<AuditTrail>()
                    .RecordViewAsync(residentId, $"GET {route}", CancellationToken.None);
            }
            catch (Exception ex)
            {
                context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AuditModule))
                    .LogError(ex, "Could not record a view of resident {ResidentId}.", residentId);
            }
        });

    public static void AddAuditEndpoints(this IEndpointRouteBuilder app)
    {
        // Who did what to a resident's record, and who looked at it. For admins.
        app.MapGet("/api/patient/{patientId:Guid}/audit", GetResidentTrail)
            .WithTags("Audit")
            .RequireAuthorization("Admin_Role");
    }

    private static async Task<IResult> GetResidentTrail(Guid patientId, DateTime? before, int? take, AuditTrail trail,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await trail.ForResidentAsync(patientId, before, take ?? 100, cancellationToken));
}

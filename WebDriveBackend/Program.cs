using Microsoft.AspNetCore.Http.Features;
using Minio;
using WebDriveBackend.Models;
using WebDriveBackend.Services;

namespace WebDriveBackend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Limits.MaxRequestBodySize = 1024 * 1024 * 1024;
        });
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 1024 * 1024 * 1024;
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        // builder.Services.AddScoped<IAdminService, AdminService>();

        builder.Services.AddSqlite<Database>(Util.DbSource("Database.db"));
        builder.Services.AddMinio(configureClient =>
            configureClient
                .WithEndpoint("127.0.0.1:9000")
                .WithCredentials("minioadmin", "minioadmin")
                .WithSSL(false)
                .Build());
        builder.Services.AddScoped<IIdentityService, IdentityService>();
        builder.Services.AddScoped<IProfileService, ProfileService>();
        builder.Services.AddScoped<IMinioService, MinioService>();
        builder.Services.AddScoped<IStorageService, StorageService>();

        var app = builder.Build();
        app.UseCors("AllowAll");

        // app.MapGet("/", (IAdminService adminService) => adminService.GenerateAdminPass());

        // 注册端点
        app.MapPost("/identity/register", (IIdentityService identityService, UserRegister userRegister) =>
        {
            var ret = identityService.Register(userRegister);
            return ret.Succeeded ? Results.Ok() : Results.BadRequest(ret.Errors);
        });

        // 登录端点
        app.MapPost("/identity/login", (IIdentityService identityService, UserLogin userLogin) =>
        {
            var ret = identityService.Login(userLogin);
            return ret.Succeeded
                ? Results.Ok(new { access_token = ret.Token!.AccessToken, refresh_token = ret.Token.RefreshToken })
                : Results.BadRequest(ret.Errors);
        });

        // 注销
        app.MapGet("/identity/logout", (HttpContext httpContext, IIdentityService identityService) =>
        {
            var ret = identityService.Authenticate(httpContext);
            if (!ret.Succeeded)
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(ret.Errors);
            }

            identityService.Logout(ret.Id!);
            return Results.Ok();
        });

        // Auth require e.g.
        // var ret = identityService.Authenticate(httpContext);
        // if (!ret.Succeeded)
        // {
        //     httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        //     return Results.Json(ret.Errors);
        // }

        // 刷新令牌
        app.MapPost("/identity/refresh",
            (HttpContext httpContext, IIdentityService identityService, RefreshToken refreshToken) =>
            {
                var ret = identityService.RefreshToken(refreshToken.Token);
                if (ret.Succeeded)
                    return Results.Ok(new
                        { access_token = ret.Token!.AccessToken, refresh_token = ret.Token.RefreshToken });
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(ret.Errors);
            });


        app.MapPost("identity/change/email",
            (HttpContext httpContext, IIdentityService identityService, IdentityEmail identityEmail) =>
            {
                var ret = identityService.Authenticate(httpContext);
                if (!ret.Succeeded)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Results.Json(ret.Errors);
                }
                
                var changeRet = identityService.ChangeEmail(ret.Id!, identityEmail.Email);
                if (!changeRet.Succeeded) return Results.BadRequest(changeRet.Errors);
                return Results.Ok();
            });

        app.MapPost("identity/change/password",
            (HttpContext httpContext, IIdentityService identityService, ChangePassword changePassword) =>
            {
                var ret = identityService.Authenticate(httpContext);
                if (!ret.Succeeded)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Results.Json(ret.Errors);
                }

                var changeRet =
                    identityService.ChangePassword(ret.Id!, changePassword.OldPassword, changePassword.NewPassword);
                if (!changeRet.Succeeded) return Results.BadRequest(changeRet.Errors);
                return Results.Ok();
            });
        
        /* Profile 服务相关 */
        app.MapPost("/profile/avatar", (HttpContext httpContext, IIdentityService identityService,
            IProfileService profileService) =>
        {
            var ret = identityService.Authenticate(httpContext);
            if (!ret.Succeeded)
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(ret.Errors);
            }

            var file = httpContext.Request.Form.Files[0];

            profileService.SetAvatar(ret.Id!, file.OpenReadStream(), file.Length);

            return Results.Ok();
        });

        app.MapGet("/profile/avatar", (HttpContext httpContext, IIdentityService identityService,
            IProfileService profileService) =>
        {
            var ret = identityService.Authenticate(httpContext);
            if (!ret.Succeeded)
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(ret.Errors);
            }

            var avatar = profileService.GetAvatar(ret.Id!);
            if (!avatar.Succeeded) return Results.BadRequest("Could not download avatar");

            return Results.File(avatar.File!);
        });


        app.MapGet("/profile/name",
            (HttpContext httpContext, IProfileService profileService, IIdentityService identityService) =>
            {
                var ret = identityService.Authenticate(httpContext);
                if (!ret.Succeeded)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Results.Json(ret.Errors);
                }

                return Results.Ok(profileService.GetName(ret.Id!));
            });

        app.MapPost("/profile/name",
            (HttpContext httpContext, IProfileService profileService, IIdentityService identityService,
                ProfileName name) =>
            {
                var ret = identityService.Authenticate(httpContext);
                if (!ret.Succeeded)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Results.Json(ret.Errors);
                }

                profileService.SetName(ret.Id!, name.Name);
                return Results.Ok();
            });

        app.MapGet("/api/demo/user",
            (HttpContext httpContext, IIdentityService identityService, IProfileService profileService) =>
            {
                var ret = identityService.Authenticate(httpContext);
                if (!ret.Succeeded)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Results.Json(ret.Errors);
                }

                return Results.Ok(profileService.Demo(ret.Id!));
            });

        app.MapGet("/profile/email",
            (HttpContext httpContext, IIdentityService identityService, IProfileService profileService) =>
            {
                var ret = identityService.Authenticate(httpContext);
                if (!ret.Succeeded)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Results.Json(ret.Errors);
                }

                return Results.Ok(profileService.GetEmail(ret.Id!));
            });
        
        /* Storage 服务相关 */
        app.MapGet("/storage/list",
            (HttpContext httpContext, IIdentityService identityService, IStorageService storageService) =>
            {
                var ret = identityService.Authenticate(httpContext);
                if (!ret.Succeeded)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Results.Json(ret.Errors);
                }

                return Results.Ok(storageService.GetFileList(ret.Id!));
            });

        app.MapGet("/storage/{id}",
            (HttpContext httpContext, IIdentityService identityService, IStorageService storageService, string id) =>
            {
                var ret = identityService.Authenticate(httpContext);
                if (!ret.Succeeded)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Results.Json(ret.Errors);
                }

                var file = storageService.DownloadFile(id, ret.Id!);
                if (!file.Succeeded) return Results.BadRequest(file.Errors);
                return Results.File(file.File!);
            });

        app.MapPost("/storage",
            (HttpContext httpContext, IIdentityService identityService, IStorageService storageService) =>
            {
                var ret = identityService.Authenticate(httpContext);
                if (!ret.Succeeded)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Results.Json(ret.Errors);
                }
                
                var file = httpContext.Request.Form.Files[0];
                storageService.UploadFile(ret.Id!, file.OpenReadStream(), file.FileName, file.Length);
                return Results.Ok();
            });

        app.MapDelete("/storage/{id}",
            (HttpContext httpContext, IIdentityService identityService, IStorageService storageService, string id) =>
            {
                var ret = identityService.Authenticate(httpContext);
                if (!ret.Succeeded)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Results.Json(ret.Errors);
                }
                
                var retDelete = storageService.DeleteFile(id, ret.Id!);
                if(!retDelete.Succeeded) return Results.BadRequest(retDelete.Errors);
                return Results.Ok();
            });

        app.Run();
    }
}
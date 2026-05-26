using System;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using AWsecretary.Data;
using AWsecretary.Responsitories;
using AWsecretary.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AWsecretary API",
        Version = "v1",
        Description = "API for member management and notifications"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 讀取 ConnectionString
var cs = builder.Configuration.GetConnectionString("DefaultConnection");

// 宣告資料庫健康狀態狀態物件
var dbHealth = new DatabaseHealth { IsDatabaseConnectOK = true, IsDatabaseAvailable = true };
ServerVersion serverVersion = null;

try
{
    // 嘗試偵測資料庫版本（如果 DB 沒開，這裡會直接拋出例外）
    serverVersion = ServerVersion.AutoDetect(cs);
}
catch (Exception ex)
{
    using var loggerFactory = LoggerFactory.Create(lb => lb.AddConsole());
    var logger = loggerFactory.CreateLogger("Startup");
    logger.LogWarning(ex, "MySQL connection auto-detect failed於啟動階段。將於頁面提示資料庫不可用。");

    dbHealth.IsDatabaseConnectOK = false;

    // Fallback: 假定一個預設版本以利 DbContext 註冊，避免 DI 容器在建立時崩潰，但實際上不啟用 InMemory DB 混淆行為
    serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
}

// 註冊 ApplicationDbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(cs, serverVersion));

// 註冊 DatabaseHealth 供 middleware 與頁面檢查
builder.Services.AddSingleton(dbHealth);

// Authentication (Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Member/Login";
        options.LogoutPath = "/Member/Logout";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Repositories & Services
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IMemberService, MemberService>();

// HttpClient for FCM
builder.Services.AddHttpClient();

// Hosted service for expiry notifications
builder.Services.AddHostedService<NotificationHostedService>();

var app = builder.Build();

// ====== 新增：啟動後立即檢查資料庫是否可連線，若不可則設定 dbHealth 並記錄 ======
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetService<ILoggerFactory>();
    var logger = loggerFactory?.CreateLogger("Startup");

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        // Database.CanConnect 會嘗試以目前連線字串連線資料庫（若資料庫不存在或無法連線會回傳 false）
        if (!db.Database.CanConnect())
        {
            dbHealth.IsDatabaseAvailable = false;
            logger?.LogWarning("資料庫不可連線。應顯示 DatabaseUnavailable 頁面。");
        }
        else
        {
            dbHealth.IsDatabaseAvailable = true;
        }
    }
    catch (Exception ex)
    {
        dbHealth.IsDatabaseAvailable = false;
        logger?.LogError(ex, "檢查資料庫連線時發生例外，將顯示 DatabaseUnavailable 頁面。");
    }
}
// =====================================================================================

// ==========================================
// Configure the HTTP request pipeline.
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AWsecretary API V1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 【Middleware 1】啟動時資料庫即不可用的攔截 (必須放在 UseRouting 之前)
app.Use(async (context, next) =>
{
    var health = context.RequestServices.GetService<DatabaseHealth>();
    if (health != null && !health.IsDatabaseConnectOK)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 排除靜態資源、swagger、api、dbhealth、以及 DatabaseUnavailable(兩種路徑) 本身
        var isStatic = path.Contains('.') || path.StartsWith("/_framework") || path.StartsWith("/lib") ||
                       path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/images");
        var isSwagger = path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
        var isApi = path.StartsWith("/api", StringComparison.OrdinalIgnoreCase);
        var isDbHealth = path.Equals("/dbhealth", StringComparison.OrdinalIgnoreCase);
        var isTargetPage = path.StartsWith("/DatabaseUnavailable", StringComparison.OrdinalIgnoreCase)
                           || path.StartsWith("/Home/DatabaseUnavailable", StringComparison.OrdinalIgnoreCase);

        if (!isTargetPage && !isSwagger && !isApi && !isStatic && !isDbHealth)
        {
            // 導向 MVC 的錯誤 action（你已將 view 放在 Views/Shared，Controller action 為 Home/DatabaseUnavailable）
            context.Response.Redirect("/Home/DatabaseUnavailable");
            return;
        }
    }
    await next();
});

// 【Middleware 2】執行期（Runtime）資料庫斷線攔截 (捕捉 Try-Catch 異常)
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex) when ((ex.GetType().FullName ?? string.Empty).Contains("MySql") 
                               || (ex.InnerException?.GetType().FullName ?? string.Empty).Contains("MySql"))
    {
        var logger = context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("DatabaseMiddleware");
        logger?.LogError(ex, "Database connection failed at runtime.");

        var redirectPath = "/Home/DatabaseUnavailable";
        // 使用 StartsWithSegments 比較 PathString 的情況
        if (!context.Response.HasStarted && !context.Request.Path.StartsWithSegments(new PathString(redirectPath), StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect(redirectPath);
            return;
        }

        throw;
    }
});

// 路由與身分驗證群組
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 支援 attribute routes（API controller）與傳統 MVC route
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// 簡單診斷 endpoint
app.MapGet("/dbhealth", (DatabaseHealth dh) => Results.Json(new { dh.IsDatabaseAvailable }));

app.Run();
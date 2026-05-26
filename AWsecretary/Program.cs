using AWsecretary.Data;
using AWsecretary.Models;
using AWsecretary.Responsitories;
using AWsecretary.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MySqlConnector;
using System;
using System.IO;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 讀取版本：優先使用 appsettings.json 的 App:Version，否則退回到 assembly 資訊
var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
var version = builder.Configuration.GetValue<string>("App:Version")
              ?? assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
              ?? assembly.GetName().Version?.ToString()
              ?? "0.0.0";

// 把版本注入 DI，方便 Razor 頁面或 Controller 使用
builder.Services.AddSingleton(new AppInfo { Version = version });

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

bool fAutoCreateDataBase = true; // 自動建立資料庫（開發環境專用，生產環境請確保資料庫已存在）
// ====== 修改：啟動後立即檢查資料庫是否可連線，若不可且 fAutoCreateDataBase==true，嘗試建立資料庫與 data_member 表 ======
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetService<ILoggerFactory>();
    var logger = loggerFactory?.CreateLogger("Startup");

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        if (!db.Database.CanConnect())
        {
            if (!fAutoCreateDataBase)
            {
                dbHealth.IsDatabaseAvailable = false;
                logger?.LogWarning("資料庫不可連線。fAutoCreateDataBase=false，將顯示 DatabaseUnavailable 頁面。");
            }
            else
            {
                logger?.LogWarning("資料庫不可連線，fAutoCreateDataBase=true，開始嘗試建立資料庫與 data_member 表...");

                try
                {
                    // 先以 MySqlConnectionStringBuilder 取得資料庫名稱並建立資料庫（若不存在）
                    var csBuilder = new MySqlConnectionStringBuilder(cs);
                    var dbName = csBuilder.Database ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(dbName))
                    {
                        throw new InvalidOperationException("Connection string 未包含資料庫名稱（Database）。無法自動建立資料庫。");
                    }

                    // 以不帶 Database 的連線字串連接 MySQL 伺服器，建立 database
                    csBuilder.Database = string.Empty;
                    var serverOnlyCs = csBuilder.ConnectionString;

                    using (var serverConn = new MySqlConnection(serverOnlyCs))
                    {
                        serverConn.Open();
                        using var cmd = serverConn.CreateCommand();
                        cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{dbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
                        cmd.ExecuteNonQuery();
                        logger?.LogInformation("資料庫 `{Db}` 已存在或建立完成。", dbName);
                    }

                    // 重新嘗試連線並套用 migration（若有）或直接建立 table
                    try
                    {
                        db.Database.Migrate();
                        logger?.LogInformation("套用 EF migration 成功。");
                        dbHealth.IsDatabaseAvailable = db.Database.CanConnect();

                        // 建立 data_member 表的 SQL（使用 IF NOT EXISTS 並明確指定 utf8mb4）
                        var createTableSql = @"
CREATE TABLE IF NOT EXISTS `data_member` (
  `nid` INT(20) UNSIGNED AUTO_INCREMENT PRIMARY KEY COMMENT '流水號',
  `sid` VARCHAR(20) NOT NULL COMMENT '序號' UNIQUE,
  `create_date` DATETIME NOT NULL COMMENT '建立日期',
  `modify_date` DATETIME NULL COMMENT '最後更新日期',
  `parent_mid` VARCHAR(20) NULL COMMENT '上線會員帳號',
  `mid` VARCHAR(20) NOT NULL COMMENT '會員帳號;;英數字組合;;20' UNIQUE,
  `pwd` VARCHAR(16) NOT NULL COMMENT '密碼;;英數字組合;;16',
  `join_date` DATETIME NULL COMMENT '加入日期',
  `continue_date` DATETIME NULL COMMENT '續約日期',
  `real_continue_date` DATETIME NULL COMMENT '實際續約日期',
  `hint_days` INT(5) NOT NULL DEFAULT 30 COMMENT '提前幾天提醒',
  `birthday` DATETIME NULL COMMENT '生日',
  `name` VARCHAR(50) NULL COMMENT '名稱;;;;100',
  `eng_name` VARCHAR(50) NULL COMMENT '英文名;;非必要欄位;;50',
  `head_img` VARCHAR(255) NULL COMMENT '相片',
  `iden` VARCHAR(255) NULL COMMENT '身份證字號加密',
  `cmp_code` VARCHAR(10) NULL COMMENT '公司統編;;數字;;10',
  `role` VARCHAR(3) NULL COMMENT '角色',
  `authorization_page` VARCHAR(100) NOT NULL COMMENT '授權可視頁面',
  `address` VARCHAR(500) NULL COMMENT '地址;;;;500',
  `mobile` VARCHAR(20) NULL COMMENT '手機;;09xx123456或09xx-123-456;;20',
  `tel` VARCHAR(20) NULL COMMENT '電話;;(02)xx123456或(02)1234-5678;;20',
  `fax` VARCHAR(20) NULL COMMENT '傳真(Fax);;(02)xx123456或(02)1234-5678;;20',
  `email` VARCHAR(100) NULL COMMENT '電子信箱;;jacky@jotangi.com;;100',
  `avalible` VARCHAR(2) NOT NULL COMMENT '狀態',
  `start_date` DATETIME NULL COMMENT '生效日期',
  `signature_pic` VARCHAR(255) NULL COMMENT '簽名圖檔',
  `advertising_id` VARCHAR(200) NULL COMMENT '廣告ID',
  `device_id` VARCHAR(200) NULL COMMENT '裝置ID',
  `fcm_token` TEXT NULL COMMENT '推播token',
  `priority` INT(5) NULL COMMENT '權限;;暫時無作用;;5',
  `edit_sid` VARCHAR(20) NULL COMMENT '編輯人員',
  `cur_coupon` INT(10) NULL COMMENT '目前折價券',
  `cur_point` INT(10) NULL COMMENT '目前點數',
  `script` TEXT NULL COMMENT '說明',
  `remark` TEXT NULL COMMENT '備註'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='會員資料表';";

                        // 執行建表 SQL
                        db.Database.ExecuteSqlRaw(createTableSql);
                    }
                    catch (Exception migEx)
                    {
                        logger?.LogWarning(migEx, "套用 migration 失敗，改以執行建表 SQL 建立 data_member 表。");
                        logger?.LogInformation("data_member 表已建立或存在。");
                        dbHealth.IsDatabaseAvailable = db.Database.CanConnect();
                    }
                }
                catch (Exception createEx)
                {
                    dbHealth.IsDatabaseAvailable = false;
                    logger?.LogError(createEx, "自動建立資料庫或 data_member 表時發生錯誤。");
                }
            }
        }
        else
        {
            dbHealth.IsDatabaseAvailable = true;
        }
    }
    catch (Exception ex)
    {
        dbHealth.IsDatabaseAvailable = false;
        logger?.LogError(ex, "檢查或建立資料庫時發生錯誤");
    }
}
// =====================================================================================

// ==========================================
// Configure the HTTP request pipeline.
// ==========================================
bool fAlwaysSwagger = true; // 強制在任何環境都啟用 Swagger（開發或生產）
if (app.Environment.IsDevelopment() || fAlwaysSwagger)
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
    if (health != null && !health.IsDatabaseAvailable)
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
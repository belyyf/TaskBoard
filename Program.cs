using System.Diagnostics;
using TaskBoard.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DI: сервис задач (Singleton — данные в памяти живут всё время работы приложения)
builder.Services.AddSingleton<ITaskService, InMemoryTaskService>();

var app = builder.Build();

// ===== MIDDLEWARE =====

// 1. Замер времени обработки (самый внешний — оборачивает всё)
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next(context);
    sw.Stop();
    context.Response.Headers.Append("X-Response-Time-ms", sw.ElapsedMilliseconds.ToString());
});

// 2. Логирование + заголовок X-App-Name
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-App-Name", "TaskBoard");

    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("--> {Method} {Path}", context.Request.Method, context.Request.Path);

    await next(context);

    logger.LogInformation("<-- {StatusCode}", context.Response.StatusCode);
});

// 3. Health-check
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/health")
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("healthy");
        return; // не идём дальше
    }
    await next(context);
});

// ===== СТАНДАРТНЫЙ ПАЙПЛАЙН =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
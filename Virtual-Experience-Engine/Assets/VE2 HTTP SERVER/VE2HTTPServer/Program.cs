using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(o => o.AddPolicy("AllowAll",
    p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
var app = builder.Build();
app.UseCors("AllowAll");

string worldsDir = Path.Combine(builder.Environment.ContentRootPath, "Worlds");

// 1) Serve each world’s image (truly async)
app.MapGet("/worlds/{worldName}/image", async (HttpContext ctx, string worldName) =>
{
    var imgPath = Path.Combine(worldsDir, worldName, "image.png");
    if (!File.Exists(imgPath))
        return Results.NotFound("No image found.");
    var bytes = await File.ReadAllBytesAsync(imgPath);
    return Results.File(bytes, "image/png");
});

// 2) Serve the world list (truly async)
app.MapGet("/worlds", async (HttpContext ctx) =>
{
    var arr = new JsonArray();

    foreach (var dir in Directory.EnumerateDirectories(worldsDir))
    {
        var name = Path.GetFileName(dir);
        var cfgPath = Path.Combine(dir, "config.json");
        if (!File.Exists(cfgPath)) continue;

        var rawJson = await File.ReadAllTextAsync(cfgPath);
        var node = JsonNode.Parse(rawJson)!.AsObject();

        node["imageUrl"] =
            $"{ctx.Request.Scheme}://{ctx.Request.Host}/worlds/{name}/image";

        arr.Add(node);
    }

    await ctx.Response.WriteAsJsonAsync(new { worlds = arr });
});

// 3) Serve a single world by name (truly async)
app.MapGet("/worlds/{worldName}", async (HttpContext ctx, string worldName) =>
{
    var dir = Path.Combine(worldsDir, worldName);
    var cfgPath = Path.Combine(dir, "config.json");
    if (!File.Exists(cfgPath))
        return Results.NotFound("World not found.");

    var rawJson = await File.ReadAllTextAsync(cfgPath);
    var node = JsonNode.Parse(rawJson)!.AsObject();

    node["imageUrl"] =
        $"{ctx.Request.Scheme}://{ctx.Request.Host}/worlds/{worldName}/image";

    return Results.Json(node);
});

app.Run("http://localhost:5000");

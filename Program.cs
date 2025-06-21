using System.Collections.Concurrent;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCookiePolicy();

// 유저 승인/로그인 정보
ConcurrentDictionary<string, UserData> userTable = new();
// 유저 카드 목록
ConcurrentDictionary<string, List<CardEntry>> cardTable = new();

const string adminId = "관리자";
const string adminPw = "123456";

// users.json 로드
if (File.Exists("users.json"))
{
    var json = File.ReadAllText("users.json");
    var data = JsonSerializer.Deserialize<Dictionary<string, UserData>>(json);
    if (data != null)
        foreach (var kv in data)
            userTable[kv.Key] = kv.Value;
}

// userCards.json 로드
if (File.Exists("userCards.json"))
{
    var json = File.ReadAllText("userCards.json");
    var data = JsonSerializer.Deserialize<Dictionary<string, List<CardEntry>>>(json);
    if (data != null)
        foreach (var kv in data)
            cardTable[kv.Key] = kv.Value;
}

// 로그인
app.MapPost("/login", async context =>
{
    var req = await JsonSerializer.DeserializeAsync<LoginRequest>(context.Request.Body);
    if (string.IsNullOrWhiteSpace(req?.id) || string.IsNullOrWhiteSpace(req?.pw)) { context.Response.StatusCode = 400; return; }

    string id = req.id!;
    string pw = req.pw!;

    if (id == adminId && pw == adminPw)
    {
        context.Response.Cookies.Append("user", id);
        context.Response.Cookies.Append("role", "admin");
        await context.Response.WriteAsync("ADMIN");
        return;
    }

    if (userTable.TryGetValue(id, out var info))
    {
        if (info.Password != pw)
        {
            await context.Response.WriteAsync("DUPLICATE_ID"); return;
        }
        context.Response.Cookies.Append("user", id);
        context.Response.Cookies.Append("role", "user");
        await context.Response.WriteAsync(info.IsApproved ? "APPROVED" : "ALREADY_PENDING");
        return;
    }

    userTable[id] = new UserData { Password = pw, IsApproved = false };
    context.Response.Cookies.Append("user", id);
    context.Response.Cookies.Append("role", "user");
    SaveUsers();
    await context.Response.WriteAsync("PENDING");
});

// 관리자 승인/거절/강퇴
app.MapPost("/admin/approve/{id}", async context =>
{
    var id = context.Request.RouteValues["id"]?.ToString();
    if (id != null && userTable.ContainsKey(id))
    {
        userTable[id].IsApproved = true;
        SaveUsers();
    }
    await context.Response.WriteAsync($"승인 완료: {id}");
});

app.MapPost("/admin/deny/{id}", async context =>
{
    var id = context.Request.RouteValues["id"]?.ToString();
    if (id != null)
    {
        userTable.TryRemove(id, out _);
        SaveUsers();
    }
    await context.Response.WriteAsync($"삭제 완료: {id}");
});

app.MapPost("/admin/kick/{id}", async context =>
{
    var id = context.Request.RouteValues["id"]?.ToString();
    if (id != null && userTable.TryRemove(id, out _))
    {
        SaveUsers();
        await context.Response.WriteAsync($"강퇴 완료: {id}");
        return;
    }
    await context.Response.WriteAsync($"강퇴 실패: {id}");
});

app.MapGet("/admin/pending", async context =>
{
    var list = userTable.Where(kv => !kv.Value.IsApproved).Select(kv => kv.Key).ToList();
    var json = JsonSerializer.Serialize(list);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(json);
});

app.MapGet("/admin/approved", async context =>
{
    var list = userTable.Where(kv => kv.Value.IsApproved).Select(kv => kv.Key).ToList();
    var json = JsonSerializer.Serialize(list);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(json);
});

// 로그아웃
app.MapPost("/logout", async context =>
{
    context.Response.Cookies.Delete("user");
    context.Response.Cookies.Delete("role");
    await context.Response.WriteAsync("LOGGED_OUT");
});

// 인증 확인
app.MapGet("/check-auth", async context =>
{
    var user = context.Request.Cookies["user"];
    var role = context.Request.Cookies["role"];

    if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(role))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("UNAUTHORIZED");
        return;
    }

    if (role == "user" && (!userTable.TryGetValue(user, out var info) || !info.IsApproved))
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("NOT_APPROVED");
        return;
    }

    var json = JsonSerializer.Serialize(new { id = user, role = role });
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(json);
});

// 카드 등록
app.MapPost("/card/add", async context =>
{
    var user = context.Request.Cookies["user"];
    if (string.IsNullOrEmpty(user))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("UNAUTHORIZED");
        return;
    }

    var card = await JsonSerializer.DeserializeAsync<CardEntry>(context.Request.Body);
    if (card == null)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("INVALID_CARD");
        return;
    }

    if (!cardTable.ContainsKey(user)) cardTable[user] = new List<CardEntry>();
    cardTable[user].Add(card);
    SaveCards();
    await context.Response.WriteAsync("ADDED");
});

// 카드 목록
app.MapGet("/card/list", async context =>
{
    var user = context.Request.Cookies["user"];
    if (string.IsNullOrEmpty(user))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("UNAUTHORIZED");
        return;
    }

    cardTable.TryGetValue(user, out var cards);
    cards ??= new List<CardEntry>();

    var json = JsonSerializer.Serialize(cards);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(json);
});

void SaveUsers() => File.WriteAllText("users.json", JsonSerializer.Serialize(userTable));
void SaveCards() => File.WriteAllText("userCards.json", JsonSerializer.Serialize(cardTable));

AppDomain.CurrentDomain.ProcessExit += (_, __) => { SaveUsers(); SaveCards(); };

app.Run();

// DTO들
public class UserData
{
    public bool IsApproved { get; set; }
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    public string? id { get; set; }
    public string? pw { get; set; }
}

public class CardEntry
{
    public string Tier { get; set; } = "";
    public string Job { get; set; } = "";
    public string Effect { get; set; } = "";
    public int Count { get; set; } = 1;
}

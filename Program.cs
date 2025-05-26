using System.Collections.Concurrent;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// 댓글 메모리 저장소 (서버 꺼지면 사라짐)
ConcurrentQueue<string> comments = new();

app.MapGet("/", async context =>
{
    string html = @"
<!DOCTYPE html>
<html>
<head><title>King_God_General</title></head>
<body>
    <h2></h2>
    <form id='commentForm'>
        <input type='text' id='commentInput' placeholder='Enter comment' />
        <button type='submit'>Check</button>
    </form>
    <ul id='commentList'></ul>

    <script>
        async function loadComments() {
            const res = await fetch('/comments');
            const data = await res.json();
            const list = document.getElementById('commentList');
            list.innerHTML = '';
            data.forEach(c => {
                const li = document.createElement('li');
                li.textContent = c;
                list.appendChild(li);
            });
        }

        document.getElementById('commentForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            const input = document.getElementById('commentInput');
            if (input.value.trim() === '') return;
            await fetch('/comment', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(input.value)
            });
            input.value = '';
            loadComments();
        });

        loadComments();
        setInterval(loadComments, 3000);
    </script>
</body>
</html>";
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(html);
});

app.MapPost("/comment", async context =>
{
    var text = await JsonSerializer.DeserializeAsync<string>(context.Request.Body);
    if (!string.IsNullOrWhiteSpace(text))
        comments.Enqueue(text);
    context.Response.StatusCode = 200;
});

app.MapGet("/comments", context =>
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsync(JsonSerializer.Serialize(comments));
});

app.Run();

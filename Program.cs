using System.Collections.Concurrent;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ✅ 정적 파일 사용 (wwwroot 내 이미지/JS/CSS 가능)
app.UseStaticFiles();

// ✅ 댓글 저장소 & 파일 경로
ConcurrentQueue<string> comments = new();
string filePath = "comments.txt";

// ✅ 서버 시작 시 댓글 복원
if (File.Exists(filePath))
{
    foreach (var line in File.ReadLines(filePath))
        comments.Enqueue(line);
}

app.MapGet("/", async context =>
{
    string html = @"
<!DOCTYPE html>
<html lang='ko'>
<head>
    <meta charset='UTF-8'>
    <title>작은 마음, 큰 기적</title>
    <style>
        body {
            font-family: 'Arial', sans-serif;
            background-color: #fef8f5;
            color: #333;
            margin: 0;
            padding: 0;
        }
        header {
            background-color: #f27272;
            padding: 20px;
            color: white;
            text-align: center;
        }
        .container {
            max-width: 800px;
            margin: 30px auto;
            padding: 20px;
            background: white;
            border-radius: 10px;
            box-shadow: 0 0 10px #ddd;
        }
        .gallery img {
            width: 100%;
            max-height: 250px;
            object-fit: contain;
            margin-bottom: 15px;
            border-radius: 8px;
            background-color: #eee;
        }
        h2 {
            color: #d9534f;
        }
        form {
            margin-top: 20px;
            display: flex;
            gap: 10px;
        }
        input[type='text'] {
            flex: 1;
            padding: 10px;
            font-size: 16px;
        }
        button {
            padding: 10px 20px;
            background-color: #f27272;
            color: white;
            border: none;
            cursor: pointer;
            font-weight: bold;
        }
        ul {
            list-style: none;
            padding: 0;
            margin-top: 20px;
        }
        li {
            background: #fcebea;
            margin-bottom: 8px;
            padding: 10px;
            border-radius: 5px;
        }
    </style>
</head>
<body>
    <header>
        <h1>작은 마음, 큰 기적</h1>
        <p>이 아이가 배부르다는 느낌을 알까요?</p>
    </header>

    <div class='container'>
        <div class='gallery'>
            <img src='/NewFolder/KakaoTalk_20250611_213408609.jpg' alt='아이 사진1'>
            <img src='/NewFolder/KakaoTalk_20250611_213408609_01.jpg' alt='아이 사진2'>
            <img src='/NewFolder/KakaoTalk_20250611_213408609_02.jpg' alt='아이 사진3'>
        </div>

        <h2>마음을 나눠주세요</h2>
        <form id='commentForm'>
            <input type='text' id='commentInput' placeholder='따뜻한 말을 남겨주세요' />
            <button type='submit'>마음 남기기</button>
        </form>
        <ul id='commentList'></ul>
    </div>

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

// ✅ 댓글 추가 → 메모리 + 파일 저장
app.MapPost("/comment", async context =>
{
    var text = await JsonSerializer.DeserializeAsync<string>(context.Request.Body);
    if (!string.IsNullOrWhiteSpace(text))
    {
        comments.Enqueue(text);
        await File.AppendAllTextAsync(filePath, text + Environment.NewLine);  // ✅ 파일에 저장
    }
    context.Response.StatusCode = 200;
});

// ✅ 댓글 전체 조회
app.MapGet("/comments", context =>
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsync(JsonSerializer.Serialize(comments));
});

app.Run();

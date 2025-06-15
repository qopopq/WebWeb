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
    string html = @"<!DOCTYPE html>
<html lang='ko'>
<head>
    <meta charset='UTF-8'>
    <title>우리 길드 출석 용병 시스템</title>
    <style>
        :root {
            --primary: #5d71ff;
            --accent: #ff8cab;
            --bg: #f7f8ff;
            --card: #ffffff;
            --text: #333333;
        }
        * { box-sizing: border-box; }
        body {
            font-family: 'Pretendard', 'Arial', sans-serif;
            margin: 0;
            background: var(--bg);
            color: var(--text);
            line-height: 1.6;
        }
        header {
            padding: 2.5rem 1rem;
            background: linear-gradient(135deg, var(--primary), var(--accent));
            color: white;
            text-align: center;
        }
        h1 { margin: 0; font-size: 2.5rem; letter-spacing: 1px; }
        h2 { color: var(--primary); margin-top: 2.5rem; font-size: 1.5rem; }
        .container {
            max-width: 750px;
            margin: 0 auto;
            padding: 2rem 1.25rem 4rem;
        }
        section {
            background: var(--card);
            padding: 1.5rem 1.25rem;
            border-radius: 1rem;
            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.05);
            margin-bottom: 2rem;
        }
        ul { padding-left: 1.25rem; margin: 0; }
        li { margin-bottom: 0.5rem; }
        li strong { color: var(--accent); }
        .nickname { color: var(--primary); font-weight: 600; }
        .comments form {
            display: flex;
            gap: 0.5rem;
            margin-top: 1rem;
        }
        .comments input {
            flex: 1;
            border: 1px solid #ddd;
            padding: 0.75rem;
            border-radius: 0.5rem;
            font-size: 1rem;
        }
        .comments button {
            border: none;
            background: var(--primary);
            color: white;
            padding: 0.75rem 1.25rem;
            border-radius: 0.5rem;
            font-weight: 600;
            cursor: pointer;
        }
        .comments ul { margin-top: 1rem; list-style: none; padding: 0; }
        .comments li {
            background: var(--bg);
            padding: 0.75rem;
            border-radius: 0.5rem;
            margin-bottom: 0.5rem;
        }
    </style>
</head>
<body>
    <header>
        <h1>우리 길드</h1>
        <p>출석 용병 시스템 안내</p>
    </header>
    <main class='container'>
        <section class='rules'>
            <h2>출석 용병 조건</h2>
            <ul>
                <li><strong>5일 출석</strong> → 43깜엽 지급</li>
                <li><strong>연속 7일 출석</strong> → 깜주 지급<br><small>(단, 중간에 하루라도 빠지면 43깜엽으로 대체)</small></li>
                <li>
                  보상을 받은 후 출석 일수는 초기화되며 다음 출석부터 1일로 다시 시작됩니다.<br>
                  <small>
                    (출석체크 후 반드시 길챗에 카운트 1..2..3...7 이렇게 남겨주시면 됩니다.<br>
                    최종 출첵 완료 시 <strong>7_(본계 닉)</strong> 또는 <strong>5_(본계 닉)</strong> 형태로 남겨주세요.)
                  </small>
                </li>
            </ul>
        </section>
        <section class='people'>
            <h2>보상 지급 담당</h2>
            <ul>
                <li>깜엽 지급 : <span class='nickname'>쇄</span>, <span class='nickname'>오픈채팅봇</span></li>
                <li>주엽 지급 : <span class='nickname'>내</span></li>
            </ul>
        </section>
        <section class='notes'>
            <h2>알림</h2>
            <p>엽서 누락은 절대 없으나, 현실 일정으로 지급이 다소 지연될 수 있습니다. 재촉은 삼가 바랍니다.</p>
            <p>길드 건물이 완공되기 전까지는 위 조건이 유지되며, 추후 보상 체계 변경 시 사전 공지하겠습니다.</p>
            <p>꾸준한 출석에 진심으로 감사드리며, 보상 지급과 관련해 착오가 없으시길 바랍니다!</p>
        </section>
        <section class='comments'>
            <h2>길드 게시판</h2>
            <form id='commentForm'>
                <input type='text' id='commentInput' placeholder='길드원에게 한 마디...' autocomplete='off' />
                <button type='submit'>등록</button>
            </form>
            <ul id='commentList'></ul>
        </section>
    </main>
    <script>
        async function loadComments() {
            const res = await fetch('/comments');
            const data = await res.json();
            const list = document.getElementById('commentList');
            list.innerHTML = '';
            data.slice().reverse().forEach(c => {
                const li = document.createElement('li');
                li.textContent = c;
                list.appendChild(li);
            });
        }

        document.getElementById('commentForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            const input = document.getElementById('commentInput');
            if (!input.value.trim()) return;
            await fetch('/comment', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(input.value)
            });
            input.value = '';
            loadComments();
        });

        loadComments();
        setInterval(loadComments, 5000);
    </script>
</body>
</html>";
    context.Response.ContentType = "text/html; charset=utf-8";
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

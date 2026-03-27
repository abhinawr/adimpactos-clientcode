using Microsoft.AspNetCore.Mvc;
using AdImpactOs.Survey.Services;

namespace AdImpactOs.Survey.Controllers;

/// <summary>
/// Serves the panelist-facing HTML survey page.
/// Route: /survey/take/{token}
/// </summary>
[Route("survey")]
[ApiExplorerSettings(IgnoreApi = true)]
public class SurveyTakePageController : Controller
{
    private readonly SurveyTokenService _tokenService;

    public SurveyTakePageController(SurveyTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpGet("take/{token}")]
    public IActionResult Take(string token)
    {
        var payload = _tokenService.ValidateToken(token);
        if (payload == null)
        {
            return Content(GetErrorPage("Invalid or Expired Link",
                "This survey link is no longer valid. It may have expired or been used already. Please contact the survey administrator for a new link."),
                "text/html");
        }

        return Content(GetSurveyPage(token), "text/html");
    }

    private string GetSurveyPage(string token)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
    <title>Survey - AdImpact Os</title>
    <style>
        *, *::before, *::after {{ box-sizing: border-box; margin: 0; padding: 0; }}
        :root {{
            --primary: #1a73e8; --primary-dark: #1557b0; --success: #34a853;
            --danger: #ea4335; --bg: #f0f2f5; --card: #fff; --text: #202124;
            --text-muted: #5f6368; --border: #dadce0; --radius: 12px;
        }}
        body {{ font-family: 'Segoe UI', system-ui, -apple-system, sans-serif; background: var(--bg); color: var(--text); min-height: 100vh; display: flex; flex-direction: column; align-items: center; padding: 24px 16px; }}
        .header {{ text-align: center; margin-bottom: 24px; }}
        .header .brand {{ font-size: 14px; color: var(--text-muted); margin-bottom: 8px; display: flex; align-items: center; justify-content: center; gap: 6px; }}
        .header .brand span {{ color: var(--primary); }}
        .header h1 {{ font-size: 24px; font-weight: 700; margin-bottom: 4px; }}
        .header .desc {{ font-size: 14px; color: var(--text-muted); max-width: 520px; }}
        .survey-card {{ background: var(--card); border: 1px solid var(--border); border-radius: var(--radius); width: 100%; max-width: 640px; overflow: hidden; }}
        .progress-bar {{ height: 4px; background: #e8eaed; }}
        .progress-fill {{ height: 100%; background: var(--primary); transition: width .3s ease; width: 0%; }}
        .question-container {{ padding: 24px; }}
        .question-num {{ font-size: 12px; font-weight: 600; color: var(--primary); text-transform: uppercase; letter-spacing: .5px; margin-bottom: 8px; }}
        .question-text {{ font-size: 18px; font-weight: 600; margin-bottom: 20px; line-height: 1.4; }}
        .question-metric {{ display: inline-block; font-size: 11px; padding: 2px 8px; background: #e8f0fe; color: var(--primary); border-radius: 10px; margin-bottom: 12px; }}
        .options {{ display: flex; flex-direction: column; gap: 10px; }}
        .option {{ display: flex; align-items: center; gap: 12px; padding: 14px 16px; border: 2px solid var(--border); border-radius: 8px; cursor: pointer; transition: all .15s; font-size: 15px; }}
        .option:hover {{ border-color: var(--primary); background: #f8f9ff; }}
        .option.selected {{ border-color: var(--primary); background: #e8f0fe; }}
        .option input {{ display: none; }}
        .option .radio {{ width: 20px; height: 20px; border: 2px solid var(--border); border-radius: 50%; display: flex; align-items: center; justify-content: center; flex-shrink: 0; transition: all .15s; }}
        .option.selected .radio {{ border-color: var(--primary); background: var(--primary); }}
        .option.selected .radio::after {{ content: ''; width: 8px; height: 8px; background: #fff; border-radius: 50%; }}
        .rating-options {{ display: flex; gap: 8px; flex-wrap: wrap; }}
        .rating-btn {{ width: 48px; height: 48px; border: 2px solid var(--border); border-radius: 8px; display: flex; align-items: center; justify-content: center; font-size: 16px; font-weight: 600; cursor: pointer; transition: all .15s; background: var(--card); }}
        .rating-btn:hover {{ border-color: var(--primary); background: #f8f9ff; }}
        .rating-btn.selected {{ border-color: var(--primary); background: var(--primary); color: #fff; }}
        .nav-bar {{ padding: 16px 24px; border-top: 1px solid var(--border); display: flex; justify-content: space-between; align-items: center; }}
        .btn {{ padding: 10px 24px; border: none; border-radius: 8px; font-size: 14px; font-weight: 600; cursor: pointer; transition: all .15s; }}
        .btn-primary {{ background: var(--primary); color: #fff; }}
        .btn-primary:hover {{ background: var(--primary-dark); }}
        .btn-primary:disabled {{ background: #ccc; cursor: not-allowed; }}
        .btn-outline {{ background: transparent; border: 1px solid var(--border); color: var(--text); }}
        .btn-outline:hover {{ background: #f8f9fa; }}
        .btn-success {{ background: var(--success); color: #fff; }}
        .counter {{ font-size: 13px; color: var(--text-muted); }}
        .loading {{ text-align: center; padding: 60px 24px; }}
        .loading .spinner {{ display: inline-block; width: 32px; height: 32px; border: 3px solid var(--border); border-top-color: var(--primary); border-radius: 50%; animation: spin .6s linear infinite; }}
        @keyframes spin {{ to {{ transform: rotate(360deg); }} }}
        .error-box {{ text-align: center; padding: 40px 24px; }}
        .error-box h2 {{ color: var(--danger); margin-bottom: 8px; }}
        .success-box {{ text-align: center; padding: 60px 24px; }}
        .success-box .check {{ width: 64px; height: 64px; background: var(--success); border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 16px; font-size: 32px; color: #fff; }}
        .success-box h2 {{ margin-bottom: 8px; }}
        .success-box p {{ color: var(--text-muted); }}
        .textarea {{ width: 100%; min-height: 100px; padding: 12px; border: 2px solid var(--border); border-radius: 8px; font-size: 14px; font-family: inherit; resize: vertical; }}
        .textarea:focus {{ outline: none; border-color: var(--primary); }}
        @media (max-width: 480px) {{
            body {{ padding: 16px 12px; }}
            .question-text {{ font-size: 16px; }}
            .rating-btn {{ width: 40px; height: 40px; font-size: 14px; }}
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""brand""><span>&#9678;</span> AdImpact Os</div>
        <h1 id=""surveyTitle"">Loading survey...</h1>
        <p class=""desc"" id=""surveyDesc""></p>
    </div>
    <div class=""survey-card"" id=""surveyCard"">
        <div class=""progress-bar""><div class=""progress-fill"" id=""progress""></div></div>
        <div id=""content"">
            <div class=""loading""><div class=""spinner""></div><p style=""margin-top:12px;color:var(--text-muted)"">Loading survey questions...</p></div>
        </div>
    </div>

    <script>
    const TOKEN = '{EscapeJs(token)}';
    const API_BASE = '/api/surveys/take/' + TOKEN;
    let survey = null;
    let currentQ = 0;
    let answers = {{}};
    let startTime = Date.now();
    let questionStartTime = Date.now();

    async function init() {{
        try {{
            const res = await fetch(API_BASE);
            if (!res.ok) {{
                const err = await res.json();
                showError(err.error || 'Failed to load survey');
                return;
            }}
            survey = await res.json();
            document.getElementById('surveyTitle').textContent = survey.surveyName;
            document.getElementById('surveyDesc').textContent = survey.description || '';
            renderQuestion();
        }} catch (e) {{
            showError('Unable to connect to the survey server. Please try again later.');
        }}
    }}

    function renderQuestion() {{
        if (!survey || !survey.questions || !survey.questions.length) {{
            showError('No questions found in this survey.');
            return;
        }}

        const q = survey.questions[currentQ];
        const total = survey.questions.length;
        const pct = ((currentQ) / total) * 100;
        document.getElementById('progress').style.width = pct + '%';

        let optionsHtml = '';
        const type = (q.questionType || '').toLowerCase();

        if (type === 'yesno') {{
            optionsHtml = renderOptions(['Yes', 'No'], q.questionId);
        }} else if (type === 'rating') {{
            const scale = q.scale || 10;
            let btns = '';
            for (let i = 1; i <= scale; i++) {{
                const sel = answers[q.questionId]?.numericValue === i ? 'selected' : '';
                btns += `<div class=""rating-btn ${{sel}}"" onclick=""selectRating('${{q.questionId}}', ${{i}})"">${{i}}</div>`;
            }}
            optionsHtml = `<div class=""rating-options"">${{btns}}</div>`;
        }} else if (type === 'openended') {{
            const val = answers[q.questionId]?.answer || '';
            optionsHtml = `<textarea class=""textarea"" id=""openAnswer"" placeholder=""Type your answer..."" oninput=""selectOpen('${{q.questionId}}')"" >${{escHtml(val)}}</textarea>`;
        }} else {{
            optionsHtml = renderOptions(q.options || [], q.questionId);
        }}

        const metricHtml = q.metric ? `<span class=""question-metric"">${{escHtml(q.metric.replace(/_/g, ' '))}}</span>` : '';

        document.getElementById('content').innerHTML = `
            <div class=""question-container"">
                <div class=""question-num"">Question ${{currentQ + 1}} of ${{total}}</div>
                ${{metricHtml}}
                <div class=""question-text"">${{escHtml(q.questionText)}}</div>
                <div class=""options"" id=""optionsArea"">${{optionsHtml}}</div>
            </div>
            <div class=""nav-bar"">
                <button class=""btn btn-outline"" onclick=""prevQuestion()"" ${{currentQ === 0 ? 'style=""visibility:hidden""' : ''}}>&#8592; Back</button>
                <span class=""counter"">${{currentQ + 1}} / ${{total}}</span>
                ${{currentQ === total - 1
                    ? '<button class=""btn btn-success"" id=""nextBtn"" onclick=""submitSurvey()"">Submit &#10003;</button>'
                    : '<button class=""btn btn-primary"" id=""nextBtn"" onclick=""nextQuestion()"">Next &#8594;</button>'
                }}
            </div>
        `;
        questionStartTime = Date.now();
    }}

    function renderOptions(options, qid) {{
        return options.map((opt, i) => {{
            const sel = answers[qid]?.answer === opt ? 'selected' : '';
            return `<label class=""option ${{sel}}"" onclick=""selectOption('${{qid}}', '${{escAttr(opt)}}', ${{i + 1}})"">
                <div class=""radio""></div>
                <span>${{escHtml(opt)}}</span>
            </label>`;
        }}).join('');
    }}

    function selectOption(qid, answer, numVal) {{
        answers[qid] = {{ questionId: qid, answer: answer, numericValue: numVal }};
        renderQuestion();
    }}

    function selectRating(qid, value) {{
        answers[qid] = {{ questionId: qid, answer: String(value), numericValue: value }};
        renderQuestion();
    }}

    function selectOpen(qid) {{
        const val = document.getElementById('openAnswer').value;
        answers[qid] = {{ questionId: qid, answer: val, numericValue: null }};
    }}

    function nextQuestion() {{
        const q = survey.questions[currentQ];
        if (q.required && !answers[q.questionId]) {{
            alert('Please answer this question before continuing.');
            return;
        }}
        if (currentQ < survey.questions.length - 1) {{
            currentQ++;
            renderQuestion();
        }}
    }}

    function prevQuestion() {{
        if (currentQ > 0) {{
            currentQ--;
            renderQuestion();
        }}
    }}

    async function submitSurvey() {{
        const q = survey.questions[currentQ];
        if (q.required && !answers[q.questionId]) {{
            alert('Please answer this question before submitting.');
            return;
        }}

        const btn = document.getElementById('nextBtn');
        btn.disabled = true;
        btn.textContent = 'Submitting...';

        const totalSeconds = Math.round((Date.now() - startTime) / 1000);
        const answerList = Object.values(answers);

        const ua = navigator.userAgent.toLowerCase();
        let deviceType = 'Desktop';
        if (/mobile|android|iphone/.test(ua)) deviceType = 'Mobile';
        else if (/tablet|ipad/.test(ua)) deviceType = 'Tablet';

        try {{
            const res = await fetch(API_BASE, {{
                method: 'POST',
                headers: {{ 'Content-Type': 'application/json' }},
                body: JSON.stringify({{
                    answers: answerList,
                    responseTimeSeconds: totalSeconds,
                    deviceType: deviceType
                }})
            }});

            if (res.ok) {{
                const data = await res.json();
                showSuccess();
            }} else {{
                const err = await res.json();
                alert('Error: ' + (err.error || 'Failed to submit. Please try again.'));
                btn.disabled = false;
                btn.textContent = 'Submit \u2713';
            }}
        }} catch (e) {{
            alert('Network error. Please check your connection and try again.');
            btn.disabled = false;
            btn.textContent = 'Submit \u2713';
        }}
    }}

    function showSuccess() {{
        document.getElementById('progress').style.width = '100%';
        document.getElementById('content').innerHTML = `
            <div class=""success-box"">
                <div class=""check"">&#10003;</div>
                <h2>Thank you!</h2>
                <p>Your response has been recorded successfully.</p>
                <p style=""margin-top:16px;font-size:13px"">You may close this window.</p>
            </div>
        `;
    }}

    function showError(msg) {{
        document.getElementById('content').innerHTML = `
            <div class=""error-box"">
                <h2>&#9888; Error</h2>
                <p style=""color:var(--text-muted)"">${{escHtml(msg)}}</p>
            </div>
        `;
    }}

    function escHtml(s) {{ const d = document.createElement('div'); d.textContent = s || ''; return d.innerHTML; }}
    function escAttr(s) {{ return (s || '').replace(/'/g, ""\\'\"").replace(/\\""/g, '&quot;'); }}

    init();
    </script>
</body>
</html>";
    }

    private string GetErrorPage(string title, string message)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
    <title>Survey Error - AdImpact Os</title>
    <style>
        body {{ font-family: 'Segoe UI', system-ui, sans-serif; background: #f0f2f5; display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; padding: 16px; }}
        .card {{ background: #fff; border: 1px solid #dadce0; border-radius: 12px; padding: 48px 32px; text-align: center; max-width: 480px; }}
        h1 {{ color: #ea4335; font-size: 24px; margin-bottom: 12px; }}
        p {{ color: #5f6368; font-size: 15px; line-height: 1.5; }}
        .brand {{ font-size: 13px; color: #5f6368; margin-top: 24px; }}
    </style>
</head>
<body>
    <div class=""card"">
        <h1>&#9888; {System.Net.WebUtility.HtmlEncode(title)}</h1>
        <p>{System.Net.WebUtility.HtmlEncode(message)}</p>
        <div class=""brand"">AdImpact Os</div>
    </div>
</body>
</html>";
    }

    private static string EscapeJs(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}

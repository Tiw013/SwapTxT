using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace TranslatorApp.Core
{
    /// <summary>
    /// Base interface for all translation engines
    /// </summary>
    public interface ITranslationEngine
    {
        string Name { get; }
        Task<string> TranslateAsync(string text, string sourceLang = "auto", string targetLang = "th");
    }

    // ─────────────────────────────────────────────────────────
    // ENGINE 1: Google Translate (Free, No API Key required)
    // ─────────────────────────────────────────────────────────
    public class GoogleTranslateEngine : ITranslationEngine
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        public string Name => "Google Translate (ฟรี)";

        static GoogleTranslateEngine()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<string> TranslateAsync(string text, string sourceLang = "auto", string targetLang = "th")
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            // Detect if text is Thai -> translate to English, otherwise to Thai
            bool isThaiText = IsThai(text);
            if (isThaiText)
            {
                sourceLang = "th";
                targetLang = "en";
            }
            else
            {
                sourceLang = "auto";
                targetLang = "th";
            }

            try
            {
                string encodedText = HttpUtility.UrlEncode(text);
                string url = $"https://translate.googleapis.com/translate_a/single" +
                             $"?client=gtx&sl={sourceLang}&tl={targetLang}" +
                             $"&dt=t&q={encodedText}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                return ParseGoogleTranslateResponse(json);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"ไม่สามารถเชื่อมต่อ Google Translate ได้\n{ex.Message}");
            }
        }

        private string ParseGoogleTranslateResponse(string json)
        {
            // Google Translate free API returns nested array format:
            // [[["translated","original",null,null,10]],null,"auto",...]
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var sb = new StringBuilder();
            if (root.ValueKind == JsonValueKind.Array)
            {
                var firstArray = root[0];
                if (firstArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in firstArray.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() > 0)
                        {
                            var translatedPart = item[0];
                            if (translatedPart.ValueKind == JsonValueKind.String)
                            {
                                sb.Append(translatedPart.GetString());
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private bool IsThai(string text)
        {
            // Check if text contains Thai characters (Unicode range: 0E00–0E7F)
            int thaiCount = 0;
            int totalChars = 0;
            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    totalChars++;
                    if (c >= '\u0E00' && c <= '\u0E7F')
                        thaiCount++;
                }
            }
            return totalChars > 0 && (double)thaiCount / totalChars > 0.3;
        }
    }

    // ─────────────────────────────────────────────────────────
    // ENGINE 2: OpenAI GPT (BYOK)
    // ─────────────────────────────────────────────────────────
    public class OpenAITranslateEngine : ITranslationEngine
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiKey;
        private readonly string _tone;

        public string Name => "OpenAI GPT (AI Mode)";

        public OpenAITranslateEngine(string apiKey, string tone = "มาตรฐาน")
        {
            _apiKey = apiKey;
            _tone = tone;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<string> TranslateAsync(string text, string sourceLang = "auto", string targetLang = "th")
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new Exception("กรุณาใส่ OpenAI API Key ในหน้าตั้งค่าก่อนนะคะ");

            bool isThaiText = IsThai(text);
            string targetLanguage = isThaiText ? "English" : "Thai";
            string toneInstruction = GetToneInstruction();

            string systemPrompt = $"You are a professional translator. {toneInstruction} " +
                                  $"Translate the given text to {targetLanguage}. " +
                                  $"Return ONLY the translated text, no explanations or notes.";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = text }
                },
                max_tokens = 2000,
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post,
                "https://api.openai.com/v1/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                using var errDoc = JsonDocument.Parse(responseJson);
                var errMsg = errDoc.RootElement
                    .GetProperty("error")
                    .GetProperty("message")
                    .GetString();
                throw new Exception($"OpenAI Error: {errMsg}");
            }

            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                ?.Trim() ?? string.Empty;
        }

        private string GetToneInstruction()
        {
            return _tone switch
            {
                "ทางการ" => "Use formal and professional language.",
                "เป็นกันเอง" => "Use casual, friendly and conversational language.",
                _ => "Use natural, standard language."
            };
        }

        private bool IsThai(string text)
        {
            int thaiCount = 0;
            int totalChars = 0;
            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    totalChars++;
                    if (c >= '\u0E00' && c <= '\u0E7F')
                        thaiCount++;
                }
            }
            return totalChars > 0 && (double)thaiCount / totalChars > 0.3;
        }
    }

    // ─────────────────────────────────────────────────────────
    // ENGINE 3: Google Gemini (BYOK)
    // ─────────────────────────────────────────────────────────
    public class GeminiTranslateEngine : ITranslationEngine
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiKey;
        private readonly string _tone;

        public string Name => "Google Gemini (AI Mode)";

        public GeminiTranslateEngine(string apiKey, string tone = "มาตรฐาน")
        {
            _apiKey = apiKey;
            _tone = tone;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<string> TranslateAsync(string text, string sourceLang = "auto", string targetLang = "th")
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new Exception("กรุณาใส่ Gemini API Key ในหน้าตั้งค่าก่อนนะคะ");

            bool isThaiText = IsThai(text);
            string targetLanguage = isThaiText ? "English" : "Thai";
            string toneInstruction = GetToneInstruction();

            string prompt = $"Translate the following text to {targetLanguage}. " +
                            $"{toneInstruction} Return ONLY the translated text:\n\n{text}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 2000
                }
            };

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/" +
                         $"gemini-1.5-flash:generateContent?key={_apiKey}";

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini Error: HTTP {(int)response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()
                ?.Trim() ?? string.Empty;
        }

        private string GetToneInstruction()
        {
            return _tone switch
            {
                "ทางการ" => "Use formal and professional language.",
                "เป็นกันเอง" => "Use casual, friendly and conversational language.",
                _ => "Use natural, standard language."
            };
        }

        private bool IsThai(string text)
        {
            int thaiCount = 0;
            int totalChars = 0;
            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    totalChars++;
                    if (c >= '\u0E00' && c <= '\u0E7F')
                        thaiCount++;
                }
            }
            return totalChars > 0 && (double)thaiCount / totalChars > 0.3;
        }
    }
}

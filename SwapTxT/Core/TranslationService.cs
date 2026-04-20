using SwapTxT.Models;
using System.Net.Http;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwapTxT.Core
{
    /// <summary>
    /// Dual-engine translation service.
    /// Engine 1: Google Translate (free, no key required).
    /// Engine 2: AI - supports OpenAI, Gemini, and OpenRouter.
    /// </summary>
    public class TranslationService
    {
        private static readonly HttpClient Http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        public async Task<string> TranslateAsync(AppSettings settings, string text, string sourceLang, string targetLang)
        {
            return settings.Engine switch
            {
                TranslationEngine.Google => await GoogleTranslateAsync(text, sourceLang, targetLang),
                TranslationEngine.AI => await AITranslateAsync(settings, text, sourceLang, targetLang),
                _ => text
            };
        }

        // ─── Engine 1: Google Translate (Free) ────────────────────────────────────
        private static async Task<string> GoogleTranslateAsync(string text, string sl, string tl)
        {
            string encoded = HttpUtility.UrlEncode(text);
            // Use sl=auto to let Google detect the source language automatically.
            // This is more robust than relying on the user's manual setting.
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={tl}&dt=t&q={encoded}";

            var response = await Http.GetStringAsync(url);
            var json = JArray.Parse(response);

            // Response[0] is array of [translated, original, ...] chunks
            var sb = new StringBuilder();
            foreach (var chunk in json[0])
            {
                var translated = chunk[0]?.ToString();
                if (!string.IsNullOrEmpty(translated))
                    sb.Append(translated);
            }
            return sb.ToString();
        }

        // ─── Engine 2: AI Translation ─────────────────────────────────────────────
        private static async Task<string> AITranslateAsync(AppSettings settings, string text, string sl, string tl)
        {
            string toneInstruction = "";
            if (!string.IsNullOrWhiteSpace(settings.AITone) && settings.AITone.ToLower() != "standard")
            {
                if (settings.AITone.Contains("Coding", StringComparison.OrdinalIgnoreCase) || 
                    settings.AITone.Contains("Structured", StringComparison.OrdinalIgnoreCase))
                {
                    toneInstruction = " Format: 1.Steps 2.Logic 3.Concise 4.Headers 5.Objective.";
                }
                else
                {
                    toneInstruction = $" Ensure the translation has a {settings.AITone} tone and mood. Make it sound natural.";
                }
            }

            string systemPrompt = $"Translate from {GetLanguageName(sl)} to {GetLanguageName(tl)}.{toneInstruction} Return ONLY translated text.";

            string cleanModel = settings.AIModel;

            // Map user-friendly UI names to valid API identifiers
            cleanModel = MapToApiModel(cleanModel, settings.AIProvider);

            // Native APIs do not use the provider/ prefix like OpenRouter does.
            // If it's NOT OpenRouter, we strip the prefix (e.g., google/gemini -> gemini)
            if (settings.AIProvider != AIProvider.OpenRouter && cleanModel.Contains('/'))
            {
                cleanModel = cleanModel.Split('/').Last();
            }

            return settings.AIProvider switch
            {
                AIProvider.OpenAI => await CallOpenAIAsync(settings.OpenAIKey, cleanModel, systemPrompt, text, "https://api.openai.com/v1/chat/completions"),
                AIProvider.OpenRouter => await CallOpenAIAsync(settings.OpenRouterKey, cleanModel, systemPrompt, text, "https://openrouter.ai/api/v1/chat/completions"),
                AIProvider.Gemini => await CallGeminiAsync(settings.GeminiKey, cleanModel, systemPrompt, text),
                _ => text
            };
        }

        private static string MapToApiModel(string uiName, AIProvider provider)
        {
            if (string.IsNullOrWhiteSpace(uiName)) return provider == AIProvider.Gemini ? "gemini-1.5-flash" : "gpt-3.5-turbo";
            string lower = uiName.ToLowerInvariant();

            // OpenRouter MUST be checked first, because it uses compound names 
            // that might contain "gemini" or "gpt", and we need to preserve the prefix.
            if (provider == AIProvider.OpenRouter)
            {
                if (!lower.Contains("/"))
                {
                    if (lower.Contains("gemini"))
                    {
                        if (lower.Contains("pro")) return "google/gemini-pro-1.5";
                        return "google/gemini-flash-1.5";
                    }
                    if (lower.Contains("claude"))
                    {
                        if (lower.Contains("opus")) return "anthropic/claude-3-opus";
                        if (lower.Contains("haiku")) return "anthropic/claude-3.5-haiku";
                        return "anthropic/claude-3.5-sonnet"; 
                    }
                    if (lower.Contains("gpt"))
                    {
                        if (lower.Contains("4o")) return "openai/gpt-4o";
                        return "openai/gpt-3.5-turbo";
                    }
                    if (lower.Contains("llama")) return "meta-llama/llama-3.1-8b-instruct";
                }
                return uiName; // If it already has a slash, use it exactly as provided
            }

            if (provider == AIProvider.Gemini)
            {
                if (lower.Contains("flash")) return "gemini-1.5-flash";
                if (lower.Contains("pro")) return "gemini-1.5-pro";
                return "gemini-1.5-flash";
            }
            
            if (provider == AIProvider.OpenAI)
            {
                if (lower.Contains("gpt-4o")) return "gpt-4o";
                if (lower.Contains("gpt-4-turbo")) return "gpt-4-turbo";
                if (lower.Contains("gpt-4")) return "gpt-4";
                if (lower.Contains("gpt-3.5")) return "gpt-3.5-turbo";
            }
            
            return uiName;
        }

        // OpenAI-compatible endpoint (works for OpenAI and OpenRouter)
        private static async Task<string> CallOpenAIAsync(string apiKey, string model, string systemPrompt, string userText, string endpoint)
        {
            // Merging the system instructions into the user prompt.
            // This is a bullet-proof fix for providers like Minimax that have strict validation
            // or length limits (e.g. 256 chars) purely on the 'system' role. 
            string combinedText = $"{systemPrompt}\n\n---\nText to translate:\n{userText}";

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = combinedText }
                },
                temperature = 0.3,
                max_tokens = 2048
            };

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error ({response.StatusCode}): {error.Substring(0, Math.Min(150, error.Length))}");
            }

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            return json["choices"]?[0]?["message"]?["content"]?.ToString()?.Trim() ?? "";
        }

        // Google Gemini API
        private static async Task<string> CallGeminiAsync(string apiKey, string model, string systemPrompt, string userText)
        {
            if (string.IsNullOrWhiteSpace(model)) model = "gemini-1.5-flash";
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new {
                        parts = new[] {
                            new { text = systemPrompt + "\n\n" + userText }
                        }
                    }
                },
                generationConfig = new { temperature = 0.3, maxOutputTokens = 2048 }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            };

            var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API Error ({response.StatusCode}): {ParseGeminiError(error)}");
            }

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            return json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()?.Trim() ?? "";
        }

        private static string ParseGeminiError(string jsonError)
        {
            try
            {
                var jObj = JObject.Parse(jsonError);
                return jObj["error"]?["message"]?.ToString() ?? "Unknown API Error";
            }
            catch { return jsonError.Substring(0, Math.Min(100, jsonError.Length)); }
        }

        private static string GetLanguageName(string code) => code switch
        {
            "th" => "Thai",
            "en" => "English",
            _ => code
        };
    }
}

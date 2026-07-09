using System.Text;
using System.Text.RegularExpressions;
using ToDoApp.DTOs.ToDoDTOs;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ToDoApp.Services
{
    public class AiScoringService : IAiScoringService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiScoringService> _logger;

        public AiScoringService(HttpClient httpClient, ILogger<AiScoringService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<int> EstimateTaskComplexityAsync(string title, string? description)
        {
            var prompt = $$"""
                            You are a task difficulty evaluator.

                            Estimate how difficult it is for an average healthy adult to COMPLETE the task.

                            The input may contain:
                            - Title
                            - Description
                            - Deadline

                            Evaluate the task using the title and description together.
                            The description may provide additional context.

                            The deadline is provided only for context.
                            NEVER change the difficulty because of how soon the deadline is.

                            Consider:
                            - estimated time
                            - planning
                            - number of steps
                            - mental effort
                            - physical effort
                            - required knowledge or skills
                            - dependency on other people
                            - overall complexity

                            Difficulty scale:
                            1-3 = trivial
                            4-6 = easy
                            7-10 = moderate
                            11-14 = difficult
                            15-17 = very difficult
                            18-20 = extremely difficult

                            If Description or Deadline is empty, ignore them.

                            Rules:
                            - Return ONLY one integer from 1 to 20.
                            - No words.
                            - No explanation.
                            - No punctuation.
                            - No markdown.

                            Task Title: {{title}}

                            Task Description:
                            {{description ?? "No description provided."}}
                            """;

            var requestBody = new OllamaRequestDto
            {
                Model = "gemma3:4b",
                Prompt = prompt,
                Stream = false
            };

            try
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                // Звертаємося до ендпоінту генерації
                var response = await _httpClient.PostAsync("api/generate", jsonContent);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OllamaResponseDto>(responseString);

                if (result != null && !string.IsNullOrWhiteSpace(result.Response))
                {
                    // Використовуємо регулярний вираз, щоб знайти перше число у відповіді.
                    // Це рятує, якщо LLM все ж вирішить написати "15." або "Score: 15"
                    var match = Regex.Match(result.Response, @"\d+");

                    if (match.Success && int.TryParse(match.Value, out int score))
                    {
                        // Обмежуємо діапазон, щоб ШІ не дав 100 балів
                        return Math.Clamp(score, 1, 20);
                    }
                }

                _logger.LogWarning("Ollama returned an unexpected format: {Response}", result?.Response);
                return 0; // Значення за замовчуванням, якщо не вдалося розпізнати
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Ollama or process the request.");
                return 0; // Значення за замовчуванням, якщо Ollama вимкнена
            }
        }
    }
}
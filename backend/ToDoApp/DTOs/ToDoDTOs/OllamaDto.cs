using System.Text.Json.Serialization;

namespace ToDoApp.DTOs.ToDoDTOs
{
    public class OllamaRequestDto
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "gemma3:4b"; 

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    public class OllamaResponseDto
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;
    }
}

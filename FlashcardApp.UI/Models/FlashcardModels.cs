using System.Text.Json.Serialization;
namespace FlashcardApp.Models;

public class Deck {
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("isReviewDeck")] public bool IsReviewDeck { get; set; } = false;
    [JsonPropertyName("cards")] public List<Flashcard> Cards { get; set; } = new();
}

public class Flashcard {
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonPropertyName("front")] public string Front { get; set; } = string.Empty;
    [JsonPropertyName("back")] public string Back { get; set; } = string.Empty;
}

public record StudyViewModel(Flashcard Card, string DeckId, int CardIndex, int TotalCards, string? ReviewDeckId);

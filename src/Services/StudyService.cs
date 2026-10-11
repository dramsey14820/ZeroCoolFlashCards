using System.Text.Json;
using FlashcardApp.Models;
namespace FlashcardApp.Services;

public class StudyService(IWebHostEnvironment env) {
    private readonly string _decksPath = Path.Combine(env.ContentRootPath, "Data", "Decks");
    private readonly string _reviewDecksPath = Path.Combine(env.ContentRootPath, "Data", "ReviewDecks");
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public async Task<List<Deck>> GetAllDecksAsync() {
        var decks = new List<Deck>();
        if (!Directory.Exists(_decksPath)) Directory.CreateDirectory(_decksPath);
        if (!Directory.Exists(_reviewDecksPath)) Directory.CreateDirectory(_reviewDecksPath);

        foreach (var file in Directory.GetFiles(_decksPath, "*.json")) {
            await using var stream = File.OpenRead(file);
            var deck = await JsonSerializer.DeserializeAsync<Deck>(stream);
            if (deck != null) decks.Add(deck);
        }

        foreach (var file in Directory.GetFiles(_reviewDecksPath, "*.json")) {
            await using var stream = File.OpenRead(file);
            var deck = await JsonSerializer.DeserializeAsync<Deck>(stream);
            if (deck != null) {
                deck.IsReviewDeck = true; // Force flag based on directory
                decks.Add(deck);
            }
        }
        return decks.OrderBy(d => d.Title).ToList();
    }

    public async Task<Deck?> GetDeckAsync(string id) {
        var path = Path.Combine(_decksPath, $"{id}.json");
        if (File.Exists(path)) {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<Deck>(stream);
        }

        path = Path.Combine(_reviewDecksPath, $"{id}.json");
        if (File.Exists(path)) {
            await using var stream = File.OpenRead(path);
            var deck = await JsonSerializer.DeserializeAsync<Deck>(stream);
            if (deck != null) deck.IsReviewDeck = true;
            return deck;
        }

        return null;
    }

    public async Task SaveDeckAsync(Deck deck) {
        var targetDir = deck.IsReviewDeck ? _reviewDecksPath : _decksPath;
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
        
        var path = Path.Combine(targetDir, $"{deck.Id}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, deck, _jsonOptions);
    }

    public void DeleteDeck(string id) {
        var path = Path.Combine(_decksPath, $"{id}.json");
        if (File.Exists(path)) File.Delete(path);

        var revPath = Path.Combine(_reviewDecksPath, $"{id}.json");
        if (File.Exists(revPath)) File.Delete(revPath);
    }

    public async Task<string?> ProcessRatingAsync(string sourceDeckId, string cardId, int rating, string? reviewDeckId) {
        if (rating >= 5) return reviewDeckId;

        var sourceDeck = await GetDeckAsync(sourceDeckId);
        if (sourceDeck == null) return reviewDeckId;

        var card = sourceDeck.Cards.FirstOrDefault(c => c.Id == cardId);
        if (card == null) return reviewDeckId;

        Deck reviewDeck;
        var baseTitle = sourceDeck.Title.Split(" - Review")[0]; // Prevent stacking review labels

        if (string.IsNullOrEmpty(reviewDeckId)) {
            reviewDeckId = sourceDeckId.Replace("_rev_", "") + "_rev_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            reviewDeck = new Deck {
                Id = reviewDeckId,
                Title = $"{baseTitle} - Review ({DateTime.Now.ToString("MMM dd, yyyy")})",
                IsReviewDeck = true
            };
        } else {
            reviewDeck = await GetDeckAsync(reviewDeckId) ?? new Deck {
                Id = reviewDeckId,
                Title = $"{baseTitle} - Review ({DateTime.Now.ToString("MMM dd, yyyy")})",
                IsReviewDeck = true
            };
        }

        if (!reviewDeck.Cards.Any(c => c.Id == card.Id)) {
            reviewDeck.Cards.Add(card);
            await SaveDeckAsync(reviewDeck);
        }

        return reviewDeckId;
    }
}

using Microsoft.AspNetCore.Mvc;
using FlashcardApp.Services;
using FlashcardApp.Models;
using System.Text.Json;

namespace FlashcardApp.Controllers;

public class DeckController(StudyService studyService) : Controller {
    public async Task<IActionResult> Index() {
        return View(await studyService.GetAllDecksAsync());
    }

    public async Task<IActionResult> Study(string deckId, int cardIndex = 0, string? reviewDeckId = null) {
        var deck = await studyService.GetDeckAsync(deckId);
        
        if (deck == null || cardIndex < 0 || cardIndex >= deck.Cards.Count) {
            ViewBag.ReviewDeckId = reviewDeckId;
            return View("Complete", (object)deckId);
        }
        
        var card = deck.Cards[cardIndex];
        return View(new StudyViewModel(card, deckId, cardIndex, deck.Cards.Count, reviewDeckId));
    }

    [HttpPost]
    public async Task<IActionResult> RateCard(string deckId, string cardId, int rating, int cardIndex, string? reviewDeckId) {
        var updatedReviewDeckId = await studyService.ProcessRatingAsync(deckId, cardId, rating, reviewDeckId);
        return RedirectToAction(nameof(Study), new { deckId, cardIndex = cardIndex + 1, reviewDeckId = updatedReviewDeckId });
    }

    public IActionResult CreateDeck() => View();
    
    [HttpPost]
    public async Task<IActionResult> CreateDeck(string id, string title) {
        var deck = new Deck { Id = id.Replace(" ", "_").ToLower(), Title = title };
        await studyService.SaveDeckAsync(deck);
        return RedirectToAction(nameof(EditDeck), new { id = deck.Id });
    }

    public async Task<IActionResult> EditDeck(string id) {
        var deck = await studyService.GetDeckAsync(id);
        if (deck == null) return NotFound();
        return View(deck);
    }

    [HttpPost]
    public IActionResult DeleteDeck(string id) {
        studyService.DeleteDeck(id);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> EditCard(string deckId, string cardId = "") {
        var deck = await studyService.GetDeckAsync(deckId);
        if (deck == null) return NotFound();
        var card = deck.Cards.FirstOrDefault(c => c.Id == cardId) ?? new Flashcard();
        ViewBag.DeckId = deckId;
        return View(card);
    }

    [HttpPost]
    public async Task<IActionResult> SaveCard(string deckId, Flashcard card) {
        var deck = await studyService.GetDeckAsync(deckId);
        if (deck != null) {
            var existing = deck.Cards.FirstOrDefault(c => c.Id == card.Id);
            if (existing != null) {
                existing.Front = card.Front;
                existing.Back = card.Back;
            } else {
                if (string.IsNullOrEmpty(card.Id)) card.Id = Guid.NewGuid().ToString();
                deck.Cards.Add(card);
            }
            await studyService.SaveDeckAsync(deck);
        }
        return RedirectToAction(nameof(EditDeck), new { id = deckId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCard(string deckId, string cardId) {
        var deck = await studyService.GetDeckAsync(deckId);
        if (deck != null) {
            deck.Cards.RemoveAll(c => c.Id == cardId);
            await studyService.SaveDeckAsync(deck);
        }
        return RedirectToAction(nameof(EditDeck), new { id = deckId });
    }

    public IActionResult ImportDeck() => View();

    [HttpPost]
    public async Task<IActionResult> ImportDeck(string id, string title, IFormFile jsonFile) {
        if (jsonFile == null || jsonFile.Length == 0) {
            ModelState.AddModelError("", "Please select a file.");
            return View();
        }
        try {
            using var stream = jsonFile.OpenReadStream();
            var importCards = await JsonSerializer.DeserializeAsync<List<ImportFormat>>(stream);
            if (importCards == null) {
                ModelState.AddModelError("", "Could not parse JSON.");
                return View();
            }
            var deck = new Deck { 
                Id = id.Replace(" ", "_").ToLower(), 
                Title = title 
            };
            foreach(var item in importCards) {
                deck.Cards.Add(new Flashcard {
                    Id = Guid.NewGuid().ToString(),
                    Front = item.Term ?? "",
                    Back = item.Definition ?? ""
                });
            }
            await studyService.SaveDeckAsync(deck);
            return RedirectToAction(nameof(EditDeck), new { id = deck.Id });
        }
        catch (Exception ex) {
            ModelState.AddModelError("", "Error importing file: " + ex.Message);
            return View();
        }
    }
}

public class ImportFormat {
    [System.Text.Json.Serialization.JsonPropertyName("term")]
    public string? Term { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("definition")]
    public string? Definition { get; set; }
}

using Godot;
using System.Collections.Generic;

namespace CardFramework;

/// <summary>
/// JSON-based card factory implementation with asset management and caching.
///
/// JsonCardFactory extends CardFactoryA to provide JSON-based card creation with
/// sophisticated asset loading, data caching, and error handling. It manages
/// card definitions stored as JSON files and automatically loads corresponding
/// image assets from specified directories.
///
/// Key Features:
/// - JSON-based card data definition with flexible schema
/// - Automatic asset loading and texture management
/// - Performance-optimized data caching for rapid card creation
/// - Comprehensive error handling with detailed logging
/// - Directory scanning for bulk card data preloading
/// - Configurable asset and data directory paths
/// </summary>
public partial class JsonCardFactory : CachedCardFactoryA<Godot.Collections.Dictionary>
{
    [ExportGroup("card_scenes")]
    /// <summary>Base card scene to instantiate for each card (must inherit from Card class).</summary>
    [Export] public PackedScene DefaultCardScene { get; set; }

    [ExportGroup("asset_paths")]
    /// <summary>Directory path containing card image assets (PNG, JPG, etc.).</summary>
    [Export] public string CardAssetDir { get; set; }
    /// <summary>Directory path containing card information JSON files.</summary>
    [Export] public string CardInfoDir { get; set; }

    [ExportGroup("default_textures")]
    /// <summary>Common back face texture used for all cards when face-down.</summary>
    [Export] public Texture2D BackImage { get; set; }

    public override void _Ready()
    {
        if (DefaultCardScene == null)
        {
            GD.PushError("default_card_scene is not assigned!");
            return;
        }

        var tempInstance = DefaultCardScene.Instantiate();
        if (tempInstance is not Card)
        {
            GD.PushError("Invalid node type! default_card_scene must reference a Card.");
            DefaultCardScene = null;
        }
        
        tempInstance.QueueFree();
    }

    public override Card CreateCard(string cardName, CardContainer target)
    {
        if (PreloadedCards.TryGetValue(cardName, out var cachedData))
        {
            var cardInfo = (Godot.Collections.Dictionary)cachedData["info"];
            var frontImage = (Texture2D)cachedData["texture"];
            return CreateCardNode((string)cardInfo["name"], frontImage, target, cardInfo);
        }
        else
        {
            var cardInfo = LoadCardInfo(cardName);
            if (cardInfo == null || cardInfo.Count == 0)
            {
                GD.PushError($"Card info not found for card: {cardName}");
                return null;
            }

            if (!cardInfo.ContainsKey("front_image"))
            {
                GD.PushError($"Card info does not contain 'front_image' key for card: {cardName}");
                return null;
            }

            var frontImagePath = CardAssetDir + "/" + (string)cardInfo["front_image"];
            var frontImage = LoadImage(frontImagePath);
            if (frontImage == null)
            {
                GD.PushError($"Card image not found: {frontImagePath}");
                return null;
            }

            return CreateCardNode((string)cardInfo["name"], frontImage, target, cardInfo);
        }
    }

    public override void PreloadCardData()
    {
        var dir = DirAccess.Open(CardInfoDir);
        if (dir == null)
        {
            GD.PushError($"Failed to open directory: {CardInfoDir}");
            return;
        }

        dir.ListDirBegin();
        var fileName = dir.GetNext();
        while (fileName != "")
        {
            if (!fileName.EndsWith(".json"))
            {
                fileName = dir.GetNext();
                continue;
            }

            var cardName = fileName.GetBaseName();
            var cardInfo = LoadCardInfo(cardName);
            if (cardInfo == null)
            {
                GD.PushError($"Failed to load card info for {cardName}");
                fileName = dir.GetNext();
                continue;
            }

            var frontImageKey = cardInfo.ContainsKey("front_image") ? (string)cardInfo["front_image"] : "";
            var frontImagePath = CardAssetDir + "/" + frontImageKey;
            var frontImageTexture = LoadImage(frontImagePath);
            if (frontImageTexture == null)
            {
                GD.PushError($"Failed to load card image: {frontImagePath}");
                fileName = dir.GetNext();
                continue;
            }

            PreloadedCards[cardName] = new Godot.Collections.Dictionary
            {
                { "info", cardInfo },
                { "texture", frontImageTexture }
            };
            GD.Print($"Preloaded card data: {PreloadedCards[cardName]}");

            fileName = dir.GetNext();
        }
    }

    private Godot.Collections.Dictionary LoadCardInfo(string cardName)
    {
        var jsonPath = CardInfoDir + "/" + cardName + ".json";
        if (!FileAccess.FileExists(jsonPath))
            return new Godot.Collections.Dictionary();

        using var file = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Read);
        var jsonString = file.GetAsText();

        var json = new Json();
        var error = json.Parse(jsonString);
        if (error != Error.Ok)
        {
            GD.PushError($"Failed to parse JSON: {jsonPath}");
            return new Godot.Collections.Dictionary();
        }

        return (Godot.Collections.Dictionary)json.Data;
    }

    private Texture2D LoadImage(string imagePath)
    {
        var texture = GD.Load<Texture2D>(imagePath);
        if (texture == null)
        {
            GD.PushError($"Failed to load image resource: {imagePath}");
            return null;
        }
        return texture;
    }

    private Card CreateCardNode(string cardName, Texture2D frontImage, CardContainer target, Godot.Collections.Dictionary cardInfo)
    {
        var card = GenerateCard(cardInfo);
        if (card == null)
            return null;

        if (!target.CanAddCards(new List<Card> { card }))
        {
            GD.Print($"Card cannot be added: {cardName}");
            card.QueueFree();
            return null;
        }

        card.CardInfo = cardInfo;
        card.CardSize = CardSize;

        var cardsNode = target.GetNode<Control>("Cards");
        cardsNode.AddChild(card);
        target.AddCard(card);

        card.CardName = cardName;
        card.SetFaces(frontImage, BackImage);

        return card;
    }

    private Card GenerateCard(Godot.Collections.Dictionary cardInfo)
    {
        if (DefaultCardScene == null)
        {
            GD.PushError("default_card_scene is not assigned!");
            return null;
        }
        return DefaultCardScene.Instantiate<Card>();
    }
}

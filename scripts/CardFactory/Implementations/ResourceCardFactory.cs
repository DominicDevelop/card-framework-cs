using Godot;

namespace CardFramework;

/// <summary>
/// Resource-based card factory implementation that loads card data from Godot Resource files (.tres/.res).
/// </summary>
public partial class ResourceCardFactory : CachedCardFactoryA<Resource>
{
    /// <summary>Directory path containing card information Resource files (.tres, .res).</summary>
    [Export]
    public string CardInfoDir { get; set; }

    public override Card CreateCard(string cardName, CardContainer target)
    {
        throw new System.NotImplementedException();
    }

    public override void PreloadCardData()
    {
        DirAccess dir = DirAccess.Open(CardInfoDir);

        if (dir == null)
        {
            GD.PushError($"Failed to open directory: {CardInfoDir}");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (fileName != "")
        {
            if (fileName.EndsWith(".tres") || fileName.EndsWith(".res"))
            {
                string resourcePath = CardInfoDir.TrimSuffix("/") + "/" + fileName;
                var resource = GD.Load<Resource>(resourcePath);

                if (resource != null)
                {
                    string cardName = fileName.GetBaseName();
                    PreloadedCards[cardName] = resource;
                } else
                {
                    GD.PushError($"Failed to load resource: {resourcePath}");
                }
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
    }
}
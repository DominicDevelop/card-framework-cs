using System.Collections.Generic;

namespace CardFramework;

/// <summary>
/// Abstract card factory with typed cache for preloaded card data.
/// Extends CardFactoryA with a generic dictionary cache for storing preloaded card data
/// to improve performance. Key: card identifier (string), Value: card data of type T.
/// </summary>
/// <typeparam name="CachedCarddataType">The type of cached card data.</typeparam>
public abstract partial class CachedCardFactoryA<CachedCarddataType> : CardFactoryA
{
    /// <summary>Dictionary cache for storing preloaded card data to improve performance.</summary>
    protected Dictionary<string, CachedCarddataType> PreloadedCards { get; } = new();
}

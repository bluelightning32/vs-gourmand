using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Collectibles;

abstract public class AggregateCondition : ICondition {
  public AggregateCondition() {}

  public abstract IEnumerable<ICondition> Conditions { get; }

  public IEnumerable<AssetLocation> Categories {
    get {
      IEnumerable<AssetLocation> result = Enumerable.Empty<AssetLocation>();
      foreach (ICondition cond in Conditions) {
        result = Enumerable.Concat(result, cond.Categories);
      }
      return result;
    }
  }

  public IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>>
  GetCategories(IReadonlyCategoryDict catdict, CollectibleObject match) {
    IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>> result =
        Enumerable.Empty<KeyValuePair<AssetLocation, IAttribute[]>>();
    foreach (ICondition cond in Conditions) {
      result = Enumerable.Concat(result, cond.GetCategories(catdict, match));
    }
    return result;
  }

  public void EnumerateMatches(MatchResolver resolver, EnumItemClass itemClass,
                               ref List<CollectibleObject> matches) {
    foreach (ICondition cond in Conditions) {
      if (cond == null) {
        continue;
      }
      cond.EnumerateMatches(resolver, itemClass, ref matches);
    }
  }
}

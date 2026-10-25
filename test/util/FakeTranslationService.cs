using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Gourmand.Test;

class FakeTranslationService : ITranslationService {
  public EnumLinebreakBehavior LineBreakBehavior =>
      throw new NotImplementedException();

  public string LanguageCode => throw new NotImplementedException();

  public string Get(string key, params object[] args) {
    return string.Format(key, args);
  }

  public IDictionary<string, string> GetAllEntries() {
    throw new NotImplementedException();
  }

  public string GetIfExists(string key, params object[] args) {
    throw new NotImplementedException();
  }

  public string GetMatching(string key, params object[] args) {
    return string.Format(key, args);
  }

  public string GetMatchingIfExists(string key, params object[] args) {
    throw new NotImplementedException();
  }

  public string GetUnformatted(string key) { return key; }

  public bool HasTranslation(string key, bool findWildcarded = true) {
    return false;
  }

  public bool HasTranslation(string key, bool findWildcarded, bool logErrors) {
    return false;
  }

  public void InitialiseSearch() { throw new NotImplementedException(); }

  public void Invalidate() { throw new NotImplementedException(); }

  public void Load(bool lazyload = false) {}

  public void PreLoad(string assetsPath, bool lazyLoad = false) {
    throw new NotImplementedException();
  }

  public void PreLoadModWorldConfig(string modPath, string modDomain,
                                    bool lazyLoad = false) {
    throw new NotImplementedException();
  }

  public void UseAssetManager(IAssetManager assetManager) {}
}
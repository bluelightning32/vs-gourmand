using Newtonsoft.Json;

using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Gourmand;

public class ModDependency {
  [JsonProperty("modid", Required = Required.Always)]
  public readonly string ModId;
  [JsonProperty("versionAtLeast")]
  public readonly string VersionAtLeast = null;
  [JsonProperty("versionBefore")]
  public readonly string VersionBefore = null;

  public ModDependency(string modId, string versionAtLeast,
                       string versionBefore) {
    ModId = modId;
    VersionAtLeast = versionAtLeast;
    VersionBefore = versionBefore;
  }

  public bool IsSatisified(IModLoader modLoader) {
    Mod mod = modLoader.GetMod(ModId);
    if (mod == null) {
      return false;
    }
    if (VersionAtLeast != null &&
        !GameVersion.IsAtLeastVersion(mod.Info.Version, VersionAtLeast)) {
      return false;
    }
    if (VersionBefore != null &&
        !GameVersion.IsLowerVersionThan(mod.Info.Version, VersionBefore)) {
      return false;
    }
    return true;
  }
}

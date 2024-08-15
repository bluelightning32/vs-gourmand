using System;
using System.Collections.Generic;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Gourmand;

public class GourmandSystem : ModSystem {
  private ICoreAPI _api;
  public override void Start(ICoreAPI api) { _api = api; }
}

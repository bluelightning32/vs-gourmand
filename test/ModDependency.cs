using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class ModDependency {
  [TestMethod]
  public void IsSatisifiedInstalled() {
    string json = @"{ modId: ""survival"" }";
    Real.ModDependency dependency =
        JsonUtil.ToObject<Real.ModDependency>(json, "gourmand");
    Assert.IsTrue(dependency.IsSatisified(LoadAssets.Server.Api.ModLoader));
  }

  [TestMethod]
  public void IsSatisifiedNotInstalled() {
    string json = @"{ modId: ""notinstalled"" }";
    Real.ModDependency dependency =
        JsonUtil.ToObject<Real.ModDependency>(json, "gourmand");
    Assert.IsFalse(dependency.IsSatisified(LoadAssets.Server.Api.ModLoader));
  }

  [TestMethod]
  public void IsSatisifiedVersionAtLeastTooHigh() {
    string json = @"{ modId: ""survival"", versionAtLeast: ""9.9.9"" }";
    Real.ModDependency dependency =
        JsonUtil.ToObject<Real.ModDependency>(json, "gourmand");
    Assert.IsFalse(dependency.IsSatisified(LoadAssets.Server.Api.ModLoader));
  }

  [TestMethod]
  public void IsSatisifiedVersionAtLeastMet() {
    string json = @"{ modId: ""survival"", versionAtLeast: ""1.19.8"" }";
    Real.ModDependency dependency =
        JsonUtil.ToObject<Real.ModDependency>(json, "gourmand");
    Assert.IsTrue(dependency.IsSatisified(LoadAssets.Server.Api.ModLoader));
  }

  [TestMethod]
  public void IsSatisifiedVersionBeforeMet() {
    string json = @"{ modId: ""survival"", versionBefore: ""9.9.9"" }";
    Real.ModDependency dependency =
        JsonUtil.ToObject<Real.ModDependency>(json, "gourmand");
    Assert.IsTrue(dependency.IsSatisified(LoadAssets.Server.Api.ModLoader));
  }

  [TestMethod]
  public void IsSatisifiedVersionBeforeTooLow() {
    string json = @"{ modId: ""survival"", versionBefore: ""1.16.0"" }";
    Real.ModDependency dependency =
        JsonUtil.ToObject<Real.ModDependency>(json, "gourmand");
    Assert.IsFalse(dependency.IsSatisified(LoadAssets.Server.Api.ModLoader));
  }
}

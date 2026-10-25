using PrefixClassName.MsTest;

using Real = Gourmand;

namespace Gourmand.Test;

[PrefixTestClass]
public class GourmandSystem {
  [TestMethod]
  public void Logger() {
    // Trying to read the logger also verifies that the mod was loaded.
    Assert.IsNotNull(Real.GourmandSystem.Logger);
  }
}

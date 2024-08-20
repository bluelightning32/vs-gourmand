using System.Reflection;
using System.Runtime.CompilerServices;

namespace Gourmand.Test;

// This class allows the Vintagestory dlls to be loaded from $(VINTAGE_STORY).
public class AddVintageStoryPath {

  [ModuleInitializer]
  internal static void ModuleInitialize() {
    _assemblyResolveDelegate = new ResolveEventHandler(
        (sender, args) => LoadFromVintageStory(sender, args));
    AppDomain.CurrentDomain.AssemblyResolve += _assemblyResolveDelegate;
  }

  static ResolveEventHandler _assemblyResolveDelegate = null;

  static Assembly LoadFromVintageStory(object sender, ResolveEventArgs args) {
    string vsDir = Environment.GetEnvironmentVariable("VINTAGE_STORY");
    if (vsDir == null) {
      Console.Error.WriteLine(
          "Warning: the VINTAGE_STORY environmental variable is unset. The " +
          "tests will likely be unable to load the Vintagestory dlls.");
      return null;
    }
    string dllName = new AssemblyName(args.Name).Name + ".dll";
    string assemblyFile = Path.Combine(vsDir, dllName);
    if (File.Exists(assemblyFile)) {
      return Assembly.LoadFrom(assemblyFile);
    }
    foreach (string subdir in new string[] { "Lib", "Mods" }) {
      assemblyFile = Path.Combine(vsDir, subdir, dllName);
      if (File.Exists(assemblyFile)) {
        return Assembly.LoadFrom(assemblyFile);
      }
    }
    return null;
  }
}

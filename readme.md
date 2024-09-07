# Gourmand Vintage Story Mod

This mod gives the player a health buff after food diversity points are earned by eating foods that match food achievements.

The [design doc](https://docs.google.com/document/d/1whnTROr1Fg5Y081aiHFQoz8qgozNUVrWCMIde17Pp9g/edit) has more information on the design and purpose of the mod.

## Building

The `VINTAGE_STORY` environment variable must be set before loading the
project. It should be set to the install location of Vintage Story (the
directory that contains VintagestoryLib.dll).

A Visual Studio Code workspace is included. The mod can be built through it or
from the command line.

### Release build from command line

This will produce a zip file in a subfolder of `bin/Release`.
```
dotnet build -c Release
```

### Debug build from command line

This will produce a zip file in a subfolder of `bin/Debug`.
```
dotnet build -c Debug
```

### Run unit tests

```
dotnet test -c Debug --logger:"console;verbosity=detailed"
```

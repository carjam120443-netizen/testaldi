# Build dependencies

The repository does not include game DLLs or proprietary game files.

For local development, put the BepInEx 5.x BepInEx.dll at:

Dependencies/BepInEx.dll

The GitHub Actions workflow downloads the public BepInEx package automatically for CI.

When Testaldi starts using the Baldi's Basics Plus Dev API or Unity/game assemblies, those reference DLLs should be supplied locally in Dependencies/ and should not be committed to the repository.

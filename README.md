# Google Translate BFBB Rewrite

This C# console app runs BFBB through Google Translate X amount of times. It is a rewrite of a Python version of this app that was written very incorrectly and ignored the complexities of the TEXT file format. This version is still a bit scuffed, but way less than the previous attempt.

## Config
The config file contains settings for the paths the program uses.
- ExtractedGameFilesPath - Path to the base game files, can be from Dolphin or GCR.
- HeavyModManagerFolderPath - Path to the folder containing the HMM executable.
- TemporaryPath - The temporary folder path.
- TEXTListJSONPath - The cache json file to store the translated file names.
- TranslationIterations - How many times the game gets translated.
- MaxRetries - Max retries for when an error occurs.
- LogToFile - If the logger will log to a log.log file.

## Building
To build it, just open the repository inside Visual Studio 2022 or later and click Build.

## Contributing
If you want to contribute, just make a PR or if there's a serious issue, make an issue if you want. I might respond to it, don't expect a fast response.

## Sources
[Heavy Iron Modding Wiki](https://www.heavyironmodding.org/wiki/Main_Page) - Helped with the TEXT file format and the .HIP file format as well.

[HipHopTool](https://github.com/igorseabra4/HipHopTool) - Wouldn't be possible without this, helped with extracting and repacking .HIP files.

[Google Translate](https://translate.google.com/) - The engine that all the TEXT gets translated through.

[SHiFT](https://www.youtube.com/@SHiFTss) - Made the Spongebob Smoking image, I just cropped & converted it.

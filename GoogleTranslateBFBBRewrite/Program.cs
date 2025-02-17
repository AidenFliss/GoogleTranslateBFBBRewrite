using System;
using System.IO;
using System.Web;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HipHopFile;

namespace GoogleTranslateBFBBRewrite;

public class Config
{
    public string ExtractedGameFilesPath { get; set; }
    public string HeavyModManagerFolderPath { get; set; }
    public string TemporaryPath { get; set; }
    public string TEXTListJSONPath { get; set; }
    public int TranslationIterations { get; set; }
    public int MaxRetries { get; set; }
    public bool LogToFile { get; set; }

    public override int GetHashCode()
    {
        return HashCode.Combine(ExtractedGameFilesPath, HeavyModManagerFolderPath, TemporaryPath, TEXTListJSONPath, TranslationIterations, MaxRetries, LogToFile);
    }
}

internal class Program
{
    public static List<string> ValidLangCodes = new List<string>
    {
            "af", "sq", "am", "ar", "hy", "as", "ay", "az", "bm", "eu", "be", "bn", "bho", "bs", "bg",
            "ca", "ceb", "ny", "co", "hr", "cs", "da", "dv", "doi", "nl", "eo", "et", // removed Chinese bc it makes the text stay chineese in some cases
            "ee", "fil", "fi", "fr", "fy", "gl", "ka", "de", "el", "gn", "gu", "ht", "ha", "haw", "he",
            "hi", "hmn", "hu", "is", "ig", "ilo", "id", "ga", "it", "ja", "jv", "kn", "kk", "km", "rw",
            "kok", "ko", "kri", "ku", "ckb", "ky", "lo", "la", "lv", "ln", "lt", "lg", "lb", "mk", "mai",
            "mg", "ms", "ml", "mt", "mi", "mr", "mni", "lus", "mn", "my", "ne", "no", "or", "om", "ps",
            "fa", "pl", "pt", "pa", "qu", "ro", "ru", "sm", "sa", "gd", "nso", "sr", "st", "sn", "sd",
            "si", "sk", "sl", "so", "es", "su", "sw", "sv", "tg", "ta", "tt", "te", "th", "ti", "ts",
            "tr", "tk", "tw", "uk", "ur", "ug", "uz", "vi", "cy", "xh", "yi", "yo", "zu"
    };

    public static List<string> SkippedLevels = new List<string>
    {
            "font2", // weird HipHopTool bug for font2 no matter the game copy, so uh, skip theres no text anyways, so its fine...
            "plat", // just thumb icons for the game, no text
            "hb00", // just a cutscene level, no text
            "sppa", // the player models, this will guarantee this skips them
            "spsb",
            "spsc",
    };

    public static Dictionary<string, string> levelPrefixToFolder = new Dictionary<string, string>
    {
            {"b1", "b1"},
            {"b2", "b2"},
            {"b3", "b3"},
            {"bb", "bb"},
            {"bc", "bc"},
            {"db", "db"},
            {"gl", "gl"},
            {"gy", "gy"},
            {"hb", "hb"},
            {"jf", "jf"},
            {"kf", "kf"},
            {"pg", "pg"},
            {"rb", "rb"},
            {"sm", "sm"},
            {"sp", "sp"},
            {"mn", "mn"},
            {"bo", "" },
            {"fo", "" },
            {"pl", "" },
    }; // i know this is useless, but idc bc it took a while to make it

    private static readonly string cfgPath = "config.json";

    public static Logger logger;

    static void Main(string[] args)
    {
        Console.WriteLine($"Google Translate BFBB/TSSM Rewrite\nV1.3\nBy: Aiden Fliss");
        Console.WriteLine($"Parsing '{cfgPath}'...");

        Config config;
        if (File.Exists(cfgPath))
        {
            string json = File.ReadAllText(cfgPath);
            config = JsonSerializer.Deserialize<Config>(json);
        }
        else
        {
            config = new Config { ExtractedGameFilesPath = "", HeavyModManagerFolderPath = "", TemporaryPath = "temp", TEXTListJSONPath = "cache.json", TranslationIterations = 0, MaxRetries = 5, LogToFile = true };
            SaveSettings(cfgPath, config);
            Console.WriteLine($"No '{cfgPath}' found! Generated a new one. Please fill out the information and restart");
        }

        Console.WriteLine($"Parsed '{cfgPath}'! Validating...");

        if (!Directory.Exists(config.ExtractedGameFilesPath))
        {
            Console.WriteLine("Invalid ExtractedGameFilesPath! Path does not exist!");
            AskExit();
        }

        if (!Directory.Exists(config.HeavyModManagerFolderPath))
        {
            Console.WriteLine("Invalid HeavyModManagerFolderPath! Path does not exist!");
            AskExit();
        }

        if (!Directory.Exists(config.TemporaryPath))
        {
            Console.WriteLine("Invalid TemporaryPath! Path does not exist!");
            AskExit();
        }

        if (!File.Exists(config.TEXTListJSONPath))
        {
            Console.WriteLine("Invalid TEXTListJSONPath! File does not exist!");
            AskExit();
        }

        if (config.TranslationIterations < 2)
        {
            Console.WriteLine("Invalid amount of TranslationIterations! Cannot be less than 2!\n" +
                "Otherwise nothing would be translated silly :P!");
            AskExit();
        }

        if (config.TranslationIterations > 50)
        {
            Console.Write($"Warning: TranslationIterations values of over 50 ({config.TranslationIterations}) is not recommended! " +
                "It might take a VERY long time!\nAre you sure? [y/n]: ");
            while (true)
            {
                string input = Console.ReadLine();
                if (input == String.Empty || input.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("Ok then.");
                    AskExit();
                }
                else if (input.Equals("y", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("Okay, you asked for this...");
                }
                else
                {
                    Console.WriteLine("Invalid! Try again\nAre you sure? [y/n]: ");
                    continue;
                }
                break;
            }
        }

        if (config.MaxRetries < 1)
        {
            Console.WriteLine("Having less than 1 MaxRetries is invalid!");
            AskExit();
        }

        Console.WriteLine("Config:");
        Console.WriteLine($"Extracted Files Path: '{config.ExtractedGameFilesPath}'");
        Console.WriteLine($"Heavy Mod Manager Path: '{config.HeavyModManagerFolderPath}'");
        Console.WriteLine($"Temporary Path: '{config.TemporaryPath}'");
        Console.WriteLine($"TEXT List JSON Path: '{config.TEXTListJSONPath}'");
        Console.WriteLine($"Translation Iterations: {config.TranslationIterations}");
        Console.WriteLine($"Max Retries: {config.MaxRetries}");
        Console.WriteLine($"Log To File: {config.LogToFile}");
        Console.Write("Are these settings correct? [y/n]: ");

        while (true)
        {
            string input = Console.ReadLine();
            if (input == String.Empty || input.Equals("n", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine("Ok then. Go fix em'!");
                AskExit();
            }
            else if (input.Equals("y", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine("Ok.");
            }
            else
            {
                Console.WriteLine("Invalid! Try again\nAre you sure? [y/n]: ");
                continue;
            }
            break;
        }
        
        logger = new Logger("log.log", config.LogToFile);

        Stopwatch totalTimeStopwatch = new Stopwatch();
        totalTimeStopwatch.Start();

        Mod mod = new Mod();
        mod.Game = HMM_Game.BFBB;
        mod.CreatedAt = DateTime.Now;
        mod.UpdatedAt = mod.CreatedAt;
        mod.ModName = $"Translated Game {mod.CreatedAt.Year:D4}-{mod.CreatedAt.Month:D2}-{mod.CreatedAt.Day:D2} {mod.CreatedAt.Hour:D2}:{mod.CreatedAt.Minute:D2}:{mod.CreatedAt.Second:D2}";
        mod.Author = "BFBB Google Translator";
        mod.Description = $"This is a translated game generated by a Google Translate script. It was translated through Google Translate {config.TranslationIterations} times.";
        mod.ModId = $"bfbb-translator-{config.TranslationIterations}-{config.GetHashCode()}";

        string modFolder = Path.Combine(config.HeavyModManagerFolderPath, "Mods", mod.ModId);

        if (!Directory.Exists(modFolder))
        {
            Directory.CreateDirectory(modFolder);
        }

        File.WriteAllText(Path.Combine(modFolder, "mod.json"), JsonSerializer.Serialize<Mod>(mod));

        if (!Directory.Exists(Path.Combine(modFolder, "files")))
        {
            Directory.CreateDirectory(Path.Combine(modFolder, "files"));
        }

        logger.Log($"Validated '{cfgPath}'!");
        logger.Log("Getting all .HIP files...");

        string[] hipFiles = Directory.GetFiles(config.ExtractedGameFilesPath, "*.HIP", SearchOption.AllDirectories);

        logger.Log("Translating .HIP files...");

        foreach (string hipFile in hipFiles)
        {
            string fileName = Path.GetFileName(hipFile);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(hipFile);

            string containingFolder = Path.GetFileName(Path.GetDirectoryName(hipFile));

            if (containingFolder == "mn-pal")
            {
                logger.Log("Skipping mn-pal because it's an unused menu thats not needed...");
                continue;
            }

            if (SkippedLevels.Contains(fileNameNoExt))
            {
                logger.Log($"Skipping {fileNameNoExt} because it is in the exceptions list...");
                continue;
            }

            logger.Log($"Extracting {fileName}...");
            ExtractHIP(hipFile, config.TemporaryPath);
            logger.Log($"Extracted {fileName}!");

            string extractPath = Path.Combine(config.TemporaryPath, fileNameNoExt);
            List<TEXT> texts = GetTextAssets(extractPath);

            logger.Log("Translating all TEXT assets...");

            foreach (TEXT text in texts)
            {
                if (IsTextInList(config.TEXTListJSONPath, text.assetName))
                {
                    logger.Log($"Skipping {text.assetName} because it's already translated...");
                    continue;
                }

                logger.Log($"Translating {text.assetName}...");

                List<string> chunks = SplitAtTags(new(text.text));
                List<string> newChunks = new List<string>();

                foreach (string chunk in chunks)
                {
                    if (chunk.Length > 200)
                    {
                        logger.Log("Chunk is over 200 characters! This may take a bit...");
                    }
                    else if (chunk.Length < 2)
                    {
                        logger.Log("Skipping less than 2 characters chunk...");
                        newChunks.Add(chunk);
                        continue;
                    }

                    if (chunk.StartsWith("{") && chunk.EndsWith("}"))
                    {
                        logger.Log("Skipping formatting chunk...");
                        newChunks.Add(chunk);
                        continue;
                    }

                    newChunks.Add(BulkTranslate(chunk, config.TranslationIterations, config));
                }

                string finalText = ArrayToString(newChunks);

                TEXT newText = new TEXT
                {
                    charCount = (uint)finalText.Length,
                    text = finalText.ToCharArray(),
                };

                TextParser.WriteTextAsset(text.assetPath, newText);
                AddTextToList(config.TEXTListJSONPath, text.assetName);
            }

            PackHIP(Path.Combine(config.TemporaryPath, fileNameNoExt) + "\\Settings.ini", Path.Combine(modFolder, "files"));
        }

        totalTimeStopwatch.Stop();
        double elapsedSeconds = totalTimeStopwatch.Elapsed.TotalSeconds;
        logger.Log($"BFBB Google Translator script completed in: {elapsedSeconds:F2} seconds");
        logger.Log("The mod has been generated inside the HMM mods folder.");
        logger.Log("It is ready to play! Enjoy! :)");

        AskExit();
    }

    public static void AskExit(int exitCode = 0)
    {
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        Environment.Exit(exitCode);
    }

    public static string BulkTranslate(string str, int amount, Config config)
    {
        string prevLang = "en";
        string nextLang = PickRandomLanguage();
        string nextString = str;
        string lastSuccessfulTranslation = str;

        for (int i = 0; i < amount; i++)
        {
            bool success = false;

            for (int attempt = 1; attempt <= config.MaxRetries; attempt++)
            {
                try
                {
                    nextString = Translate(nextString, prevLang, nextLang);
                    lastSuccessfulTranslation = nextString;
                    success = true;
                    break;
                }
                catch (Exception e)
                {
                    logger.Log("ERROR", $"Translation attempt {attempt} from {prevLang} to {nextLang} failed! Ex: {e.ToString()}");
                }
            }

            if (!success)
            {
                logger.Log("WARN", $"All {config.MaxRetries} retries failed for {prevLang} -> {nextLang}! Using last successful translation.");
                nextString = lastSuccessfulTranslation;
            }

            prevLang = nextLang;
            nextLang = PickRandomLanguage();
        }

        nextString = Translate(nextString, prevLang, "en");
        return nextString;
    }

    public static string Translate(string str, string fromLang, string toLang)
    {
        string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLang}&tl={toLang}&dt=t&q={HttpUtility.UrlEncode(str)}";

        HttpClient httpClient = new HttpClient();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)");

            HttpResponseMessage response = httpClient.Send(request);
            response.EnsureSuccessStatusCode();

            string result = response.Content.ReadAsStringAsync().Result;
            result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception("Translation failed", ex); // Throw exception so BulkTranslate can handle it
        }
    }

    public static List<TEXT> GetTextAssets(string hipFolder)
    {
        List<TEXT> textAssets = new List<TEXT>();

        if (!Directory.Exists(hipFolder) || !Directory.Exists(Path.Combine(hipFolder, "Text")))
            throw new Exception("Invalid HIP folder!");

        string[] files = Directory.GetFiles(Path.Combine(hipFolder, "Text"), "*.*", SearchOption.TopDirectoryOnly);

        foreach (string file in files)
        {
            logger.Log($"Reading {Path.GetFileNameWithoutExtension(file)}...");
            TEXT text = TextParser.ReadTextAsset(file);
            textAssets.Add(text);
            logger.Log($"Parsed {Path.GetFileNameWithoutExtension(file)}!");
        }

        return textAssets;
    }

    public static void ExtractHIP(string filePath, string extractPath)
    {
        string fullExtractPath = Path.Combine(extractPath, Path.GetFileNameWithoutExtension(filePath));

        if (Directory.Exists(fullExtractPath))
        {
            logger.Log($"{Path.GetFileName(filePath)} is already extracted! Skipping...");
            return;
        }

        logger.Log($"Extracting {Path.GetFileName(filePath)} to {extractPath}...");
        (HipFile hipFile, Game game, Platform platform) = HipHopFile.HipFile.FromPath(filePath);
        hipFile.ToIni(game, fullExtractPath, true, true);
    }

    public static void PackHIP(string iniPath, string packPath)
    {
        string hipFileName = Path.GetFileName(Path.GetDirectoryName(iniPath));
        string hipFolderName = levelPrefixToFolder[hipFileName[..2]];
        string fullExportPath = Path.Combine(packPath, hipFolderName) + $"\\{hipFileName}.HIP";
        logger.Log($"Packing {hipFileName}.HIP...");
        (HipFile hipFile, Game game, Platform platform) = HipFile.FromINI(iniPath);

        if (!Directory.Exists(Path.Combine(packPath, hipFolderName)))
            Directory.CreateDirectory(Path.Combine(packPath, hipFolderName));

        File.WriteAllBytes(fullExportPath, hipFile.ToBytes(game, platform));
    }

    public static List<string> SplitAtTags(string str)
    {
        string pattern = @"({[^{}]*})";
        string[] chunksWithTags = Regex.Split(str, pattern);
        List<string> filteredChunks = chunksWithTags.Where(chunk => !string.IsNullOrWhiteSpace(chunk)).ToList();

        return filteredChunks;
    }

    public static string ArrayToString(List<string> stringArray)
    {
        string final = "";

        foreach (string st in stringArray)
        {
            final += st;
        }

        return final;
    }

    public static string PickRandomLanguage()
    {
        Random rng = new Random();
        return ValidLangCodes[rng.Next(0, ValidLangCodes.Count)];
    }

    public static void SaveSettings(string path, Config config)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(path, json);
    }

    public static void AddTextToList(string filePath, string newText)
    {
        List<string> translatedFiles = new();

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            if (data != null && data.TryGetValue("TranslatedTEXTFiles", out var existingFiles))
            {
                translatedFiles = existingFiles;
            }
        }

        if (!translatedFiles.Contains(newText))
        {
            translatedFiles.Add(newText);
            var updatedData = new Dictionary<string, List<string>> { { "TranslatedTEXTFiles", translatedFiles } };
            File.WriteAllText(filePath, JsonSerializer.Serialize(updatedData, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public static bool IsTextInList(string filePath, string text)
    {
        List<string> translatedFiles = new();

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            if (data != null && data.TryGetValue("TranslatedTEXTFiles", out var existingFiles))
            {
                translatedFiles = existingFiles;
            }
        }

        return translatedFiles.Contains(text);
    }
}

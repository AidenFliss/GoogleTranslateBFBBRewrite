using System;
using System.IO;
using System.Web;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using HipHopFile;
using System.Text.RegularExpressions;
using System.Linq;

// Ignore Spelling: ini

namespace GoogleTranslateBFBBRewrite
{
    public class Config
    {
        public string ExtractedGameFilesPath { get; set; }
        public string OutputGameFilesPath { get; set; }
        public int TranslationIterations { get; set; }
        public bool GenerateHeavyModManagerCompatibleMod { get; set; }
    }

    internal class Program
    {
        public static List<string> ValidLangCodes = new List<string>
        {
            "af", "sq", "am", "ar", "hy", "as", "ay", "az", "bm", "eu", "be", "bn", "bho", "bs", "bg",
            "ca", "ceb", "ny", "zh-CN", "zh-TW", "co", "hr", "cs", "da", "dv", "doi", "nl", "eo", "et",
            "ee", "fil", "fi", "fr", "fy", "gl", "ka", "de", "el", "gn", "gu", "ht", "ha", "haw", "he",
            "hi", "hmn", "hu", "is", "ig", "ilo", "id", "ga", "it", "ja", "jv", "kn", "kk", "km", "rw",
            "kok", "ko", "kri", "ku", "ckb", "ky", "lo", "la", "lv", "ln", "lt", "lg", "lb", "mk", "mai",
            "mg", "ms", "ml", "mt", "mi", "mr", "mni", "lus", "mn", "my", "ne", "no", "or", "om", "ps",
            "fa", "pl", "pt", "pa", "qu", "ro", "ru", "sm", "sa", "gd", "nso", "sr", "st", "sn", "sd",
            "si", "sk", "sl", "so", "es", "su", "sw", "sv", "tg", "ta", "tt", "te", "th", "ti", "ts",
            "tr", "tk", "tw", "uk", "ur", "ug", "uz", "vi", "cy", "xh", "yi", "yo", "zu"
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
        };

        private static readonly string cfgPath = "config.json";

        static void Main(string[] args)
        {
            Console.WriteLine("Google Translate BFBB/TSSM Rewrite\nV1.0\nBy: Aiden Fliss");
            Console.WriteLine($"Parsing '{cfgPath}'...");

            Config config;
            if (File.Exists(cfgPath))
            {
                string json = File.ReadAllText(cfgPath);
                config = JsonSerializer.Deserialize<Config>(json);
            }
            else
            {
                config = new Config { ExtractedGameFilesPath = "", OutputGameFilesPath = "", GenerateHeavyModManagerCompatibleMod = false, TranslationIterations = 0 };
                SaveSettings(cfgPath, config);
                Console.WriteLine($"No '{cfgPath}' found! Generated a new one. Please fill out the information and restart");
            }

            Console.WriteLine($"Parsed '{cfgPath}'! Validating...");

            if (!Directory.Exists(config.ExtractedGameFilesPath))
            {
                Console.WriteLine("Invalid ExtractedGameFilesPath! Path does not exist!");
                AskExit();
            }

            if (!Directory.Exists(config.OutputGameFilesPath))
            {
                Console.WriteLine("Invalid OutputGameFilesPath! Path does not exist!");
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

            Console.WriteLine($"Validated '{cfgPath}'!");
            Console.WriteLine("Getting all .HIP files...");

            string[] hipFiles = Directory.GetFiles(config.ExtractedGameFilesPath);

            Console.WriteLine("Translating .HIP files...");

            foreach (string hipFile in hipFiles)
            {
                string fileName = Path.GetFileName(hipFile);
                string fileNameNoExt = Path.GetFileNameWithoutExtension(hipFile);

                string containingFolder = Path.GetFileName(Path.GetDirectoryName(hipFile));

                if (containingFolder == "mn-pal")
                {
                    Console.WriteLine("Skipping mn-pal because it's an unused menu thats not needed...");
                    continue;
                }

                Console.WriteLine($"Extracting {fileName}...");
                ExtractHIP(hipFile, config.ExtractedGameFilesPath);
                Console.WriteLine($"Extracted {fileName}!");

                string extractPath = Path.Combine(config.ExtractedGameFilesPath, fileNameNoExt);
                List<TEXT> texts = GetTextAssets(extractPath);

                Console.WriteLine("Translating all TEXT assets...");

                foreach (TEXT text in texts)
                {
                    Console.WriteLine($"Translating {text.assetName}...");

                    List<string> chunks = SplitAtTags(new(text.text));
                    List<string> newChunks = new List<string>();

                    foreach (string chunk in chunks)
                    {
                        if (chunk.Length > 200)
                        {
                            Console.WriteLine("Chunk is over 200 characters! This may take a bit...");
                        }
                        else if (chunk.Length < 2)
                        {
                            Console.WriteLine("Skipping less than 2 characters chunk...");
                            newChunks.Add(chunk);
                            continue;
                        }

                        if (chunk.StartsWith("{") && chunk.EndsWith("}"))
                        {
                            Console.WriteLine("Skipping formatting chunk...");
                            newChunks.Add(chunk);
                            continue;
                        }

                        newChunks.Add(BulkTranslate(chunk, config.TranslationIterations));
                    }

                    string finalText = ArrayToString(chunks);

                    TEXT newText = new TEXT
                    {
                        charCount = (uint)finalText.Length,
                        text = finalText.ToCharArray(),
                    };

                    TextParser.WriteTextAsset(text.assetPath, newText);
                }


            }

            //print generic program info like version, name, etc.

            //check the config.json file and read it / validate it, check if all paths inside it are valid
            //if they are, continue to next block, else
            //print "Please set all paths inside '{cfgPath}'!"

            //paths are valid, and checked, now loop through all .hip files and extract them
            //when extracted, get all TEXT assets, we want to get the text part of the file, ignoring
            //the binary meta data, after than, loop through ALL the actual text data and split
            //at every set of {} formatting tags, after than, loop through all not tag chunks
            //and translate them X amount of times specified inside config.json, after translating
            //all the chunks, recombine them all, and write it to the text file, repeat for all text
            //assets inside the entire game

            //text is now all translated, now we repack the game by getting every hip folder
            //and repacking them using hip hop file, then copy them into the game extracted directory
            //then if you want, repack the translated hips only into a heavy mod manager mod, optional
            //maybe a bool for the json to generate a mod folder for hmm and user can repack it
            //then just use hmm for running game, post translate should also send an amount of time in
            //total it took to translate, repack, and write all the files.


            //string input = "The robots have stolen all the {c:keyword}steering wheels{reset:c}!";
            //string output = BulkTranslate(input, 10);
            //Console.WriteLine(input + " -> " + output);

            //do the code later...

            AskExit();
        }

        public static void AskExit(int exitCode = 0)
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(exitCode);
        }

        public static string BulkTranslate(string str, int amount)
        {
            string prevLang = "en";
            string nextLang = PickRandomLanguage();
            string nextString = str;
            for (int i = 0; i < amount; i++)
            {
                try
                {
                    nextString = Translate(nextString, prevLang, nextLang);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error translating {str}! Skipping...");
                    nextString = str;
                }

                prevLang = nextLang;
                nextLang = PickRandomLanguage();
            }

            string finalString = Translate(nextString, prevLang, "en");
            return finalString;
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
            catch
            {
                return "Error";
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
                Console.WriteLine($"Reading {Path.GetFileNameWithoutExtension(file)}...");
                TEXT text = TextParser.ReadTextAsset(file);
                textAssets.Add(text);
                Console.WriteLine($"Parsed {Path.GetFileNameWithoutExtension(file)}!");
            }

            return textAssets;
        }

        public static void ExtractHIP(string filePath, string extractPath)
        {
            Console.WriteLine($"Extracting {Path.GetFileName(filePath)} to {extractPath}...");
            (HipFile hipFile, Game game, Platform platform) = HipHopFile.HipFile.FromPath(filePath);
            hipFile.ToIni(game, Path.Combine(extractPath, Path.GetFileNameWithoutExtension(filePath)), true, true);
        }

        public static void PackHIP(string iniPath, string packPath)
        {
            string hipFileName = Path.GetFileName(Path.GetDirectoryName(iniPath));//hb01
            string hipFolderName = levelPrefixToFolder[hipFileName[..2]]; //aka hb or b1
            string fullExportPath = Path.Combine(packPath, hipFolderName) + $"\\{hipFileName}.HIP";
            Console.WriteLine($"Packing {hipFileName}.HIP...");
            (HipFile hipFile, Game game, Platform platform) = HipFile.FromINI(iniPath);
            File.WriteAllBytes(fullExportPath, hipFile.ToBytes(game, platform));
        }
        //                    C:\export\hb\hb01.HIP
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
    }
}

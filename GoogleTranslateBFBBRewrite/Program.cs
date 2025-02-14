using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using HipHopFile;

namespace GoogleTranslateBFBBRewrite
{
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

        static void Main(string[] args)
        {
            //do the code later...
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
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

        public static void ExtractHIP(string filePath, string extractPath)
        {
            Console.WriteLine($"Extracting {Path.GetFileName(filePath)} to {extractPath}...");
            (HipFile hipFile, Game game, Platform platform) = HipHopFile.HipFile.FromPath(filePath);
            hipFile.ToIni(game, Path.Combine(extractPath, Path.GetFileNameWithoutExtension(filePath)), true, true);
        }

        public static string PickRandomLanguage()
        {
            Random rng = new Random();
            return ValidLangCodes[rng.Next(0, ValidLangCodes.Count)];
        }
    }
}

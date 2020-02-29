using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongbeamerSongbookIntegrator.Controller
{
    public class DetectLanguage
    {
        private List<string> EnglishWords = null;
        private List<string> GermanWords  = null;

        public DetectLanguage()
        {
        }

        public async Task<bool> Init()
        {
            EnglishWords = await FileContentToList(Settings.Instance.ENGLISH_WORDS_PATH);
            GermanWords = await FileContentToList(Settings.Instance.GERMAN_WORDS_PATH);

            return true;
        }

        public string Detect(string text)
        {
            if (string.IsNullOrEmpty(text) || GermanWords == null || EnglishWords == null) { return ""; }

            var segments = text.ToLower().Split(' ');
            var countGerman = 0;
            var countEnglish = 0;

            foreach (var word in segments)
            {
                if (EnglishWords.Contains(word)) { countEnglish++; }
                if (GermanWords.Contains(word)) { countGerman++; }
            }

            return countEnglish > countGerman ? "Englisch" : "Deutsch";
        }

        private async Task<List<string>> FileContentToList(string path)
        {
            if (!File.Exists(path)) { return null; }
            var sr = new StreamReader(path);

            sr.ReadLine(); // 1. Zeile enthält Link zur Quelle

            var list = new List<string>();
            var line = "";
            while ((line = await sr.ReadLineAsync()) != null)
            {
                list.Add(line.ToLower());
            }

            return list;
        }
    }
}

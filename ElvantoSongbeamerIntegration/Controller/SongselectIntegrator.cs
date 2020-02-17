using SongbeamerSongbookIntegrator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace ElvantoSongbeamerIntegration.Controller
{
    public class SongselectIntegrator
    {
        #region Variables
        public Dictionary<int, string> SongNumberDict { get; private set; } = new Dictionary<int, string>();
        public static string DICT_PATH = Path.Combine(Settings.Instance.SONGS_PATH, "CcliDictionary.jstxt");
        #endregion


        public bool UpdateCCLIDictionary(bool force)
        {
            // Falls schon eine Datei existiert, diese nur aktualisieren, wenn sie älter als 2 Wochen ist.
            if (!force && File.Exists(DICT_PATH))
            {
                if (DateTime.Now.Subtract(File.GetLastWriteTime(DICT_PATH)) < TimeSpan.FromDays(14)) { return LoadCCLIDictionary(); }
            }

            InitCCLIDictionary(false);
            SaveCCLIDictionary();

            return true;
        }

        public bool LoadCCLIDictionary()
        {
            if (!File.Exists(DICT_PATH)) { return false; }

            var sr = File.OpenText(DICT_PATH);
            if (sr == null) { return false; }

            // Splitten nach #
            var items = sr.ReadToEnd().Split(new char[] { '#' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (var item in items)
            {
                var parts = item.Split('|');
                var number = 0;
                if (parts.Count() != 2 || !int.TryParse(parts[0], out number)) { continue; }

                try
                {
                    SongNumberDict.Add(number, parts[1]);
                }
                catch
                {
                    MessageBox.Show($"Die CCLI-Nummer {number} existiert mehrfach!\r\n\r\n'{Path.GetFileNameWithoutExtension(parts[1])}' mit '{Path.GetFileNameWithoutExtension(SongNumberDict[number])}'", "Doppelte CCLI-Nummer", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            sr.Close();

            return true;
        }

        public bool SaveCCLIDictionary()
        {
            var sw = new StreamWriter(DICT_PATH, false, System.Text.Encoding.UTF8);

            if (sw == null) { return false; }

            foreach (var item in SongNumberDict)
            {
                sw.Write($"{item.Key}|{item.Value}#");
            }

            sw.Flush();
            sw.Close();

            return true;
        }

        public List<string> FindCCLIDuplicates()
        {
            return InitCCLIDictionary(true);
        }

        private List<string> InitCCLIDictionary(bool checkDuplicates)
        {
            // Alle Songs einlesen und mit SongTitel -> Full Path speichern
            // Alle Song-Dateien unter "Songbeamer/Lieder" durchgehen, nur .sng-Dateien anschauen - Folien für Vorlagen - Ordner ausschließen
            var files = Directory.GetFiles(Settings.Instance.SONGS_PATH, "*.sng", SearchOption.AllDirectories)
                                 .Where(x => !x.StartsWith($"{Settings.Instance.SONGS_PATH}\\{Settings.Instance.TEMPLATE_FILES_FOLDER}")).ToList();

            var duplicateList = new List<string>();
            foreach (var path in files)
            {
                var number = GetCCLIFromSongFile(path);

                try
                {
                    if (number > 0) { SongNumberDict.Add(number, path); }
                }
                catch
                {
                    // CCLI nicht unique...
                    if (!checkDuplicates) { MessageBox.Show($"Die CCLI-Nummer {number} existiert mehrfach!\r\n\r\n'{Path.GetFileNameWithoutExtension(path)}' mit '{Path.GetFileNameWithoutExtension(SongNumberDict[number])}'", "Doppelte CCLI-Nummer", MessageBoxButton.OK, MessageBoxImage.Warning); }
                    else { duplicateList.Add($"{number}: '{Path.GetFileNameWithoutExtension(SongNumberDict[number])}' vs. '{Path.GetFileNameWithoutExtension(path)}'"); }
                }
            }

            return duplicateList;
        }
        private int GetCCLIFromSongFile(string fullPath)
        {
            if (!File.Exists(fullPath)) { return -1; };

            // Open song file
            StreamReader sr = File.OpenText(fullPath);
            if (sr == null) { return -1; };

            // Search for CCLI-Entry, parse Number
            string sLine;
            while (true)
            {
                sLine = sr.ReadLine();
                if (sLine == null) break;

                // Is this the actuall Level in the order?
                if (!sLine.StartsWith("#")) { break; }
                if (sLine.StartsWith("#CCLI="))
                {
                    sLine = sLine.Remove(0, "#CCLI=".Length);
                    var number = -1;

                    if (!int.TryParse(sLine, out number)) { return -1; }

                    sr.Close();
                    return number;
                }
            }
            sr.Close();

            return -1;
        }
    }
}

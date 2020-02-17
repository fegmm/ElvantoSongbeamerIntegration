using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ElvantoSongbeamerIntegration.Controller
{
    class SongSheetOpener
    {
        public static string SONGS_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Lieder";
        public static string SERVICES_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Abläufe";
        public static string SERVICES_PATH_YOUTH = @"C:\Nextcloud\Medientechnik\SongBeamer\Abläufe\_Jugend";

        private const string ELVANTO_URL = "https://fegmm.elvanto.eu";
        private const string SONGS_PART_URL = "/songs/";
        private string SongbookE21Path;

        private Dictionary<string, string> SongUrlExceptions = new Dictionary<string, string>();

        private string logString = "";

        public SongSheetOpener(string songbookE21Path, string urlExceptionsElvantoPath)
        {
            SongbookE21Path = songbookE21Path;
            LoadElvantoURLExceptions(urlExceptionsElvantoPath);
        }

        public string OpenSongOfFile(string filepath)
        {
            if (!File.Exists(filepath)) { return "Fehler: Datei konnte nicht gefunden werden!"; }

            var songname = Path.GetFileNameWithoutExtension(filepath);
            return OpenSongInElvantoIfExists(songname, filepath);
        }

        private bool DoesSongExistInElvanto(string songpath)
        {
            // (In Datei selbst schauen -> unnötig, solange Ordnerstruktur bleibt)

            // Dateipfad prüfen (schneller)
            return songpath.Contains("\\Liederpool\\");
        }

        public string OpenSongInElvantoIfExists(string songname, string filepath)
        {
            if(!DoesSongExistInElvanto(filepath))
            {
                // Liederbuch herausfinden
                var songbooks = SongbookImporter.GetSongbooksFromFile(filepath);

                if (!songbooks.Any())
                {
                    var songbookImporter = new SongbookImporter(SongbookE21Path);

                    songbooks = songbookImporter.ImportSongbooksOfFile(filepath).Result;
                }

                return songname + ":\t" + songbooks;
            }

            var url = GetUrlException(songname);
            if (!url.Any())
            {
                // Zu Elvanto passende URL erstellen:  Umlaute und alles nach '/' löschen, ggf. auch Inhalte in ()
                var index = songname.IndexOf('/');
                var indexBracket = songname.IndexOf('(');

                var songCleaned = index >= 0 ? songname.Remove(index) : songname;
                var songCleanedBracketRemoved = indexBracket >= 0 ? songCleaned.Remove(indexBracket) : songname;

                var songUrl1 = RemoveUmlautsFromString(songCleaned);
                var songUrlBracketsRemoved = RemoveUmlautsFromString(songCleanedBracketRemoved);

                // Falls eine URL nicht passt, andere nehmen  -> TODO URL testen

                url = ELVANTO_URL + SONGS_PART_URL + songUrlBracketsRemoved;
            }
            System.Diagnostics.Process.Start(url);

            return songname + ":\tElvanto";
        }

        public string ParseService(string path, string songsPath)
        {
            if (!File.Exists(path)) return "";

            // Dateiinhalt vom Ablaufplan einlesen und alles bis auf Items löschen
            StreamReader sr = File.OpenText(path);
            var content = sr.ReadToEnd();
            sr.Close();

            content = content.Remove(0, ("object AblaufPlanItems: TAblaufPlanItems" + Environment.NewLine + "  items = <" + Environment.NewLine).Length);
            var indexEnd = content.IndexOf('>', content.Length - 10);
            content = content.Remove(indexEnd, content.Length - indexEnd);

            // Items separieren und nur Lieder weiterverarbeiten
            String[] separator = { "item" + Environment.NewLine };
            var songs = content.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList().Where(x => x.Contains("FileName =") && !x.Contains("FileName = '..\\")); //Environment.NewLine.ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (var songText in songs)
            {
                // Songtitel und Songpfad herausfinden
                var songTitle = songText.Remove(songText.IndexOf(Environment.NewLine)).Trim().Replace("Caption = '", "");
                songTitle = songTitle.Remove(songTitle.Length - 1);
                songTitle = UmlautsUnicodeToUTF8(songTitle);

                var songpath = songText.Remove(0, songText.IndexOf("FileName =")).Replace("FileName = '", "");
                songpath = songpath.Remove(songpath.IndexOf("'"));
                songpath = Path.Combine(songsPath, Path.Combine(songsPath, songpath));

                logString += OpenSongInElvantoIfExists(songTitle, songpath) + Environment.NewLine;
            }

            return logString;
        }

        private static String RemoveUmlautsFromString(String song)
        {
            song = UmlautsUnicodeToUTF8(song.Trim());
            var songNoUmlaut = song.Replace("ä", "").Replace("ö", "").Replace("ü", "").Replace("Ä", "").Replace("Ö", "").Replace("Ü", "");

            return songNoUmlaut.Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(" ", "-").ToLower();
        }

        public static String UmlautsUnicodeToUTF8(String song)
        {
            // https://de.wikipedia.org/wiki/Hilfe:Sonderzeichenreferenz
            song = song.Replace("#246#", "#246''#").Replace("#228#", "#228''#").Replace("#252#", "#252''#").Replace("#223#", "#223''#").Replace("#196#", "#196''#").Replace("#214#", "#214''#").Replace("#220#", "#220''#"); // Gr'#246#223'e -> Gr'#246''#223'e
            return song.Replace("'#246'", "ö").Replace("'#228'", "ä").Replace("'#252'", "ü").Replace("'#223'", "ß").Replace("'#196'", "Ä").Replace("'#214'", "Ö").Replace("'#220'", "Ü");
        }

        public static String UmlautsUTF8ToUnicode(String song)
        {
            // https://de.wikipedia.org/wiki/Hilfe:Sonderzeichenreferenz
            song = song.Replace("ö", "'#246'").Replace("ä", "'#228'").Replace("ü", "'#252'").Replace("ß", "'#223'").Replace("Ä", "'#196'").Replace("Ö", "'#214'").Replace("Ü", "'#220'");
            return song.Replace("''", ""); // Gr'#246''#223'e   -> Gr'#246#223'e
        }

        private bool LoadElvantoURLExceptions(string path)
        {
            if (!File.Exists(path)) return false;

            // Lade Version mit entfernten Kommata zum besseren Vergleichen
            StreamReader sr = File.OpenText(path);
            var content = sr.ReadToEnd();
            sr.Close();

            string[] separator = { "\r" };
            var items = content.ToLower().Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Split(new[] { '#' }));
            foreach (var item in items)
            {
                SongUrlExceptions.Add(item[0].Trim(), item[1].Trim());
            }

            return true;
        }

        private string GetUrlException(string songname)
        {
            var lowerCaseSongname = songname.ToLower().Trim();
            if (!SongUrlExceptions.ContainsKey(lowerCaseSongname)) { return "";  }

            var song = SongUrlExceptions[lowerCaseSongname];
            return song;
        }

        public static string FindCurrentlyOpenedService(string songpath)
        {
            // Von Lied auf Ablaufverzeichnis schließen
            var songsFolder = songpath;
            while(songsFolder.Contains("Lieder"))
            {
                songsFolder = Path.GetDirectoryName(songsFolder);
            }
            var serviceFolder = Path.Combine(songsFolder, "Abläufe");

            // Abläufe öffnen
            var files = Directory.GetFiles(serviceFolder, "*.col", SearchOption.AllDirectories).ToList();

            // Open File to write
            var maxAccessTime = new DateTime(2000, 1, 1);
            var fileOfMaxAccessTime = "";
            foreach (var filepath in files)
            {
                var lastAccessTime = File.GetLastAccessTime(filepath);
                if (lastAccessTime > maxAccessTime)
                {
                    maxAccessTime = lastAccessTime;
                    fileOfMaxAccessTime = filepath;
                }
            }

            return fileOfMaxAccessTime;
        }

        public List<string> FindSongDuplicates()
        {
            // Alle Songs einlesen und mit SongTitel -> Full Path speichern
            // Alle Song-Dateien unter "Songbeamer/Lieder" durchgehen, nur .sng-Dateien anschauen - Folien für Vorlagen - Ordner ausschließen
            var files = Directory.GetFiles(SongSheetOpener.SONGS_PATH, "*.sng", SearchOption.AllDirectories).Where(x => !x.StartsWith(SongSheetOpener.SONGS_PATH + "\\Folien für Vorlagen")).ToList();

            var songsDict = new Dictionary<string, string>();
            var duplicateList = new List<string>();
            foreach (var path in files)
            {
                try
                {
                    songsDict.Add(Path.GetFileNameWithoutExtension(path), path);
                }
                catch
                {
                    // Datei-Name nicht unique...
                    duplicateList.Add(path);
                }
            }

            return duplicateList;
        }
    }
}

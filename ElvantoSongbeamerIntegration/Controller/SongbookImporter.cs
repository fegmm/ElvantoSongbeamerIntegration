using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ElvantoSongbeamerIntegration.Controller
{
    class SongbookImporter
    {
        private const string SONG_DB_URL = "https://www.liederdatenbank.de";

        private List<string> SongsInE21Cleaned = new List<string>();

        private static HttpClient Client = new HttpClient();        // Instance it once saves resources: https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/


        public SongbookImporter(string songbookE21Path)
        {
            // Prepare E21 songs
            ImportE21Songs(songbookE21Path);
        }

        #region Mehrere Dateien
        public async Task<bool> AddSonbooksToFilesInFolder(string folder, bool onlyAddIfNotSet, bool scanSubdirectories = true)
        {
            if (!Directory.Exists(folder)) { return false; }

            // Folien für Vorlagen - Ordner ausschließen, nur .sng-Dateien bearbeiten
            var files = Directory.GetFiles(folder, "*.sng", scanSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList().Where(x => !x.StartsWith(folder + "\\Folien"));

            // Open File to write
            StreamWriter sw = new StreamWriter(folder + "\\SongbooksAdded-Protocol.txt");
            if (sw == null) return false;

            foreach (var file in files)
            {
                var result = await AddSongbooksToFile(file, onlyAddIfNotSet);

                // Write Header
                var appendix = result ? "" : "(skipped)";
                sw.WriteLine(file + appendix);
                sw.Flush();
            }

            // Close File
            sw.Close();

            return true;
        }
        #endregion

        #region Eine Datei
        public async Task<bool> AddSongbooksToFile(string path, bool skipIfSongbookExists)
        {
            var resultTuple = await ImportSongbooksOfFile(path, skipIfSongbookExists);

            if (!resultTuple.Item1) { return false; }

            return SaveSongbookToFile(path, resultTuple.Item2, resultTuple.Item3, resultTuple.Item4, resultTuple.Item5);
        }

        public async Task<string> ImportSongbooksOfFile(string path)
        {
            var resultTuple = await ImportSongbooksOfFile(path, false);

            return resultTuple.Item4;
        }

        private async Task<Tuple<bool, string, string, string, bool>> ImportSongbooksOfFile(string path, bool skipIfSongbookExists)
        {
            if (!File.Exists(path)) { return Tuple.Create<bool, string, string, string, bool>(false, "", "", "", false); };

            // Open Settings file
            StreamReader sr = File.OpenText(path);
            string sLine;

            if (sr == null) { return Tuple.Create<bool, string, string, string, bool>(false, "", "", "", false); };

            // Analyze levels from File
            var hasSongbook = false;
            var content = "";
            var songtext = "";
            var songbooks = "";

            while (true)
            {
                sLine = sr.ReadLine();
                if (sLine == null) break;

                // Is this the actuall Level in the order?
                if (sLine.StartsWith("#"))
                {
                    if (sLine.StartsWith("#Title="))
                    {
                        songbooks = await ImportSongbookValue(sLine.Remove(0, 7).TrimEnd(), path);  //"#Title="

                        content += sLine + Environment.NewLine;
                    }
                    else if (sLine.StartsWith("#Songbook="))
                    {
                        hasSongbook = true;
                        if (skipIfSongbookExists)
                        {
                            sr.Close();
                            { return Tuple.Create<bool, string, string, string, bool>(false, "", "", "", true); };
                        }
                        content += "#Songbook=" + songbooks + Environment.NewLine;  // SOngbook-Zeile austauschen,  wird dann nicht ans Ende drangehängt beim Speichern
                    }
                    else
                    {
                        content += sLine + Environment.NewLine;
                    }

                }
                else if (sLine.StartsWith("---"))
                {
                    // Text beginnt
                    songtext += sLine + Environment.NewLine;
                    songtext += sr.ReadToEnd();
                }
                else
                {
                    content += sLine + Environment.NewLine;
                }
            }
            sr.Close();

            return Tuple.Create<bool, string, string, string, bool>(true, content, songtext, songbooks, hasSongbook);
        }

        public static bool SaveSongbookToFile(string path, string meta, string songtext, string songbooks, bool hasSongbook)
        {
            // Convert File to 
            var encoding = detectTextEncoding(path); //GetEncoding(path);

            if (encoding != Encoding.UTF8)
            {
                byte[] encBytes = encoding.GetBytes(meta);
                byte[] utf8Bytes = Encoding.Convert(encoding, Encoding.UTF8, encBytes);
                meta = Encoding.UTF8.GetString(utf8Bytes);

                encBytes = encoding.GetBytes(songtext);
                utf8Bytes = Encoding.Convert(encoding, Encoding.UTF8, encBytes);
                songtext = Encoding.UTF8.GetString(utf8Bytes);
            }

            // Open File to write
            StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8);
            if (sw == null) return false;

            // Write Header
            sw.Write(meta);
            if (!hasSongbook) { sw.WriteLine("#Songbook=" + songbooks); }  // Falls es schon eins hat, wurde es bereits oben in Meta ersetzt

            sw.Write(songtext);

            // Close File
            sw.Close();

            return true;
        }
        #endregion

        #region E21 - Liederbuch
        private bool ImportE21Songs(string path)
        {
            if (!File.Exists(path)) return false;

            // Lade Version mit entfernten Kommata zum besseren Vergleichen
            StreamReader sr = File.OpenText(path);
            var content = sr.ReadToEnd();
            sr.Close();

            string[] separator = { "\r" };
            SongsInE21Cleaned = content.ToLower().Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList();

            return true;
        }

        public static int GetNumberLength(char[] threeChars)
        {
            for (var i = 2; i >= 0; i++)
            {
                if (threeChars[i] < '0' || threeChars[i] > '9') { return i; }
            }
            return 0;
        }

        private int GetE21SongNumber(string songname)
        {
            var song = SongsInE21Cleaned.Where(x => x.Contains(songname.Replace(",", "").ToLower()));

            // Falls vorhanden, Buchseite von Rest trennen
            var number = -1;
            if (song.Count() == 1)
            {
                var firstChars = song.First().Take(3);
                var numChars = GetNumberLength(firstChars.ToArray());

                number = int.Parse(String.Join(String.Empty, firstChars).Remove(numChars, 3 - numChars));
            }
            else if (song.Count() > 1)
            {
                // TODO:  Hear breakpoint setzen
                var stop = 1;
            }

            return number;
        }
        #endregion

        #region Eigentlicher Importer
        private async Task<string> ImportSongbookValue(string songname, string songpath)
        {
            var songbooks = await ImportSongbookFromDB(songname);

            if (songbooks.Length < 1)
            {
                var numberInE21 = GetE21SongNumber(songname);
                songbooks += numberInE21 != -1 ? "E21, " + numberInE21.ToString() : "Blatt";
            }
            if (songpath.Contains("\\Liederpool\\")) { songbooks += " | elvanto"; }

            return songbooks;
        }

        public class GitHubClient
        {
            private readonly HttpClient _httpClient;

            public GitHubClient(HttpClient httpClient)
            {
                _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            }

            public async Task<string> GetData(string url)
            {
                var request = CreateRequest(url);
                var result = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                var b = new byte[64];

                using (var contentStream = await result.Content.ReadAsStreamAsync())
                {
                    await contentStream.ReadAsync(b, 0, 64);
                }

                return b.ToString();
            }

            private static HttpRequestMessage CreateRequest(string url)
            {
                return new HttpRequestMessage(HttpMethod.Get, url);
            }
        }


        private async Task<string> ImportSongbookFromDB(string songname)
        {
            // URL escapen
            var newUrl = SONG_DB_URL + "/search?query=" + songname.Trim().Replace("&", "%26").Replace("+", "%2B").Replace("(", "%28").Replace(")", "%29").Replace(" ", "+").Replace("<br>", "") + "&mode=title";
            newUrl = "https://www.liederdatenbank.de/search?query=you+are+holy+(prince+of+peace)&mode=title";

            String source = "";
            var client = new GitHubClient(Client);
            var data = await client.GetData(newUrl.ToLower()).ConfigureAwait(false);

            try
            {
                var response = await Client.GetByteArrayAsync(newUrl.ToLower()).ConfigureAwait(false);
                source = Encoding.GetEncoding("utf-8").GetString(response, 0, response.Length - 1);
            }
            catch(Exception)
            {
            }
            
            source = WebUtility.HtmlDecode(source);
            HtmlDocument result = new HtmlDocument();
            result.LoadHtml(source);

            // 1. Überschrift nehmen
            var songs = result.DocumentNode.Descendants().Where(x => (x.Name == "a" && x.OuterHtml.StartsWith("<a href=\"/song/"))).ToList();

            if (songs.Count < 1) { return ""; }

            // Multiple Songs?
            var correctSong = songs.Where(x => x.InnerText.StartsWith(songname)).ToList();
            var song = correctSong.Count == 1 ? correctSong.FirstOrDefault() : songs.FirstOrDefault();

            if (songs.Count != 1 && correctSong.Count != 1)
            {
                if (songs.Count != 1)
                {
                    var i = 0;
                    song = correctSong[i];

                    return "unbekannt";
                }

                // throw new Exception("HTML-Format des Youtube-Livestream hat sich geändert! Offline-Status kann nicht mehr herausgefunden werden");
            }

            var test = string.Join(String.Empty, song.OuterHtml.Skip(9).TakeWhile(x => x != '\"').ToArray());
            newUrl = SONG_DB_URL + test;
            var songbooks = await GetSongbooksOfSong(newUrl);

            return songbooks;
        }

        private async Task<string> GetSongbooksOfSong(string url, string songbook = "Feiert Jesus!", string abbreviation = "FJ")
        {
            var response = await Client.GetByteArrayAsync(url);
            String source = Encoding.GetEncoding("utf-8").GetString(response, 0, response.Length - 1);
            source = WebUtility.HtmlDecode(source);
            HtmlDocument result = new HtmlDocument();
            result.LoadHtml(source);

            // 1. Überschrift nehmen
            var content = result.GetElementbyId("content");
            var songbooks = content.Descendants().Where(x => x.Name == "a" && x.InnerText.Contains(songbook)).ToList(); //.Descendants().Where(x => (x.Name == "h2" && x.InnerText.Contains(songbook))).ToList();

            var songbooksString = string.Join(" | ", songbooks.Select(x => x.InnerText + ", " + x.NextSibling.InnerText.Trim())).Replace(songbook, abbreviation);

            return songbooksString;
        }

        public static string GetSongbooksFromFile(string filepath)
        {
            if (!File.Exists(filepath)) { return ""; };

            // Open Settings file
            StreamReader sr = File.OpenText(filepath);
            string sLine;

            if (sr == null) { return ""; };

            // Analyze levels from File
            var songbooks = "";
            while (true)
            {
                sLine = sr.ReadLine();
                if (sLine == null) break;

                // Is this the actuall Level in the order?
                if (sLine.StartsWith("#Songbook="))
                {
                    songbooks = sLine.Replace("#Songbook=", "");
                    break;
                }
            }
            sr.Close();

            return songbooks;
        }
        #endregion

        #region Encoding
        // https://stackoverflow.com/questions/1025332/determine-a-strings-encoding-in-c-sharp

        // Function to detect the encoding for UTF-7, UTF-8/16/32 (bom, no bom, little
        // & big endian), and local default codepage, and potentially other codepages.
        // 'taster' = number of bytes to check of the file (to save processing). Higher
        // value is slower, but more reliable (especially UTF-8 with special characters
        // later on may appear to be ASCII initially). If taster = 0, then taster
        // becomes the length of the file (for maximum reliability). 'text' is simply
        // the string with the discovered encoding applied to the file.
        public static Encoding detectTextEncoding(string filename, int taster = 1000)
        {
            byte[] b = File.ReadAllBytes(filename);

            //////////////// First check the low hanging fruit by checking if a
            //////////////// BOM/signature exists (sourced from http://www.unicode.org/faq/utf_bom.html#bom4)
            if (b.Length >= 4 && b[0] == 0x00 && b[1] == 0x00 && b[2] == 0xFE && b[3] == 0xFF) { return Encoding.GetEncoding("utf-32BE"); }  // UTF-32, big-endian 
            else if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xFE && b[2] == 0x00 && b[3] == 0x00) { return Encoding.UTF32; }    // UTF-32, little-endian
            else if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF) { return Encoding.BigEndianUnicode; }     // UTF-16, big-endian
            else if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE) { return Encoding.Unicode; }              // UTF-16, little-endian
            else if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) { return Encoding.UTF8; } // UTF-8
            else if (b.Length >= 3 && b[0] == 0x2b && b[1] == 0x2f && b[2] == 0x76) { return Encoding.UTF7; } // UTF-7


            //////////// If the code reaches here, no BOM/signature was found, so now
            //////////// we need to 'taste' the file to see if can manually discover
            //////////// the encoding. A high taster value is desired for UTF-8
            if (taster == 0 || taster > b.Length) taster = b.Length;    // Taster size can't be bigger than the filesize obviously.


            // Some text files are encoded in UTF8, but have no BOM/signature. Hence
            // the below manually checks for a UTF8 pattern. This code is based off
            // the top answer at: https://stackoverflow.com/questions/6555015/check-for-invalid-utf8
            // For our purposes, an unnecessarily strict (and terser/slower)
            // implementation is shown at: https://stackoverflow.com/questions/1031645/how-to-detect-utf-8-in-plain-c
            // For the below, false positives should be exceedingly rare (and would
            // be either slightly malformed UTF-8 (which would suit our purposes
            // anyway) or 8-bit extended ASCII/UTF-16/32 at a vanishingly long shot).
            int i = 0;
            bool utf8 = false;
            while (i < taster - 4)
            {
                if (b[i] <= 0x7F) { i += 1; continue; }     // If all characters are below 0x80, then it is valid UTF8, but UTF8 is not 'required' (and therefore the text is more desirable to be treated as the default codepage of the computer). Hence, there's no "utf8 = true;" code unlike the next three checks.
                if (b[i] >= 0xC2 && b[i] <= 0xDF && b[i + 1] >= 0x80 && b[i + 1] < 0xC0) { i += 2; utf8 = true; continue; }
                if (b[i] >= 0xE0 && b[i] <= 0xF0 && b[i + 1] >= 0x80 && b[i + 1] < 0xC0 && b[i + 2] >= 0x80 && b[i + 2] < 0xC0) { i += 3; utf8 = true; continue; }
                if (b[i] >= 0xF0 && b[i] <= 0xF4 && b[i + 1] >= 0x80 && b[i + 1] < 0xC0 && b[i + 2] >= 0x80 && b[i + 2] < 0xC0 && b[i + 3] >= 0x80 && b[i + 3] < 0xC0) { i += 4; utf8 = true; continue; }
                utf8 = false; break;
            }
            if (utf8 == true)
            {
                return Encoding.UTF8;
            }


            // The next check is a heuristic attempt to detect UTF-16 without a BOM.
            // We simply look for zeroes in odd or even byte places, and if a certain
            // threshold is reached, the code is 'probably' UF-16.          
            double threshold = 0.1; // proportion of chars step 2 which must be zeroed to be diagnosed as utf-16. 0.1 = 10%
            int count = 0;
            for (int n = 0; n < taster; n += 2) if (b[n] == 0) count++;
            if (((double)count) / taster > threshold) { return Encoding.BigEndianUnicode; }
            count = 0;
            for (int n = 1; n < taster; n += 2) if (b[n] == 0) count++;
            if (((double)count) / taster > threshold) { return Encoding.Unicode; } // (little-endian)


            // Finally, a long shot - let's see if we can find "charset=xyz" or
            // "encoding=xyz" to identify the encoding:
            for (int n = 0; n < taster - 9; n++)
            {
                if (
                    ((b[n + 0] == 'c' || b[n + 0] == 'C') && (b[n + 1] == 'h' || b[n + 1] == 'H') && (b[n + 2] == 'a' || b[n + 2] == 'A') && (b[n + 3] == 'r' || b[n + 3] == 'R') && (b[n + 4] == 's' || b[n + 4] == 'S') && (b[n + 5] == 'e' || b[n + 5] == 'E') && (b[n + 6] == 't' || b[n + 6] == 'T') && (b[n + 7] == '=')) ||
                    ((b[n + 0] == 'e' || b[n + 0] == 'E') && (b[n + 1] == 'n' || b[n + 1] == 'N') && (b[n + 2] == 'c' || b[n + 2] == 'C') && (b[n + 3] == 'o' || b[n + 3] == 'O') && (b[n + 4] == 'd' || b[n + 4] == 'D') && (b[n + 5] == 'i' || b[n + 5] == 'I') && (b[n + 6] == 'n' || b[n + 6] == 'N') && (b[n + 7] == 'g' || b[n + 7] == 'G') && (b[n + 8] == '='))
                    )
                {
                    if (b[n + 0] == 'c' || b[n + 0] == 'C') n += 8; else n += 9;
                    if (b[n] == '"' || b[n] == '\'') n++;
                    int oldn = n;
                    while (n < taster && (b[n] == '_' || b[n] == '-' || (b[n] >= '0' && b[n] <= '9') || (b[n] >= 'a' && b[n] <= 'z') || (b[n] >= 'A' && b[n] <= 'Z')))
                    { n++; }
                    byte[] nb = new byte[n - oldn];
                    Array.Copy(b, oldn, nb, 0, n - oldn);
                    try
                    {
                        string internalEnc = Encoding.ASCII.GetString(nb);
                        return Encoding.GetEncoding(internalEnc);
                    }
                    catch { break; }    // If C# doesn't recognize the name of the encoding, break.
                }
            }


            // If all else fails, the encoding is probably (though certainly not
            // definitely) the user's local codepage! One might present to the user a
            // list of alternative encodings as shown here: https://stackoverflow.com/questions/8509339/what-is-the-most-common-encoding-of-each-language
            // A full list can be found using Encoding.GetEncodings();
            return Encoding.Default;
        }


        /// <summary

        /// Get File's Encoding

        /// </summary>
        /// <param name="filename">The path to the file
        private static Encoding GetEncoding(string filename)
        {
            // This is a direct quote from MSDN:  
            // The CurrentEncoding value can be different after the first
            // call to any Read method of StreamReader, since encoding
            // autodetection is not done until the first call to a Read method.

            using (var reader = new StreamReader(filename, Encoding.Default, true))
            {
                if (reader.Peek() >= 0) // you need this!
                    reader.Read();

                return reader.CurrentEncoding;
            }

            //File.WriteAllText(@"c:\Lieder\testfile.txt", "Hello World\r\nline2\r\nline3", Encoding.UTF8);
            // filename = @"c:\Lieder\testfile.txt";

            // Read the BOM
            /*var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.ASCII;*/
        }
        #endregion
    }
}
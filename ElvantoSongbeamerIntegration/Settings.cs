using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongbeamerSongbookIntegrator
{
    public sealed class Settings
    {
        private static readonly Settings instance = new Settings();
        public readonly string PPTS_PATH = @"C:\Nextcloud\Medientechnik\2 PPT-Liedfolien und PPT-Master";
        public readonly string SONGS_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Lieder";
        public readonly string PICTURES_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Bilder";
        public readonly string SCRIPTS_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Scripts";
        public readonly string SERVICES_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Abläufe";
        public readonly string SERVICES_YOUTH_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Abläufe\_Jugend";
        public readonly string TEMPLATE_FILES_FOLDER = "Folien für Vorlagen";

        public string E21_SONGS_PATH;
        public string URL_EXCEPTIONS_PATH;
        public string CCLI_DICT_PATH;

        public readonly string ELVANTO_URL = "https://fegmm.elvanto.eu";
        public readonly string SONGS_PART_URL = "/songs/";       

        private Settings()
        {
            E21_SONGS_PATH = $"{SCRIPTS_PATH}\\E21-Liederbuch (ohne Kommata).txt";
            URL_EXCEPTIONS_PATH = $"{SCRIPTS_PATH}\\ElvantoURLExceptions.txt";
            CCLI_DICT_PATH = $"{SONGS_PATH}\\CcliDictionary.jstxt";
        }

        public static Settings Instance  {  get {  return instance;  } }
    }
}

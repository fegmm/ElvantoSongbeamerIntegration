namespace SongbeamerSongbookIntegrator
{
    public sealed class Settings
    {
        private static readonly Settings instance = new Settings();
        public readonly string PPTS_PATH = @"C:\Nextcloud\Medientechnik\2 PPT-Liedfolien und PPT-Master";
        public readonly string SONGS_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Lieder";
        public readonly string IMAGES_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Bilder";
        public readonly string IMAGES_DIASHOW_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Bilder\Temporäre Diashow-Bilder für Gottesdienste";
        public readonly string SCRIPTS_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Scripts";
        public readonly string SERVICES_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Abläufe";
        public readonly string SERVICES_YOUTH_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Abläufe\_Jugend";
        public readonly string SERVICES_TEMPLATES_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Gottesdienst-Vorlagen";
        public readonly string TEMPLATE_FILES_FOLDER = "Folien für Vorlagen";

        public readonly bool ADD_KLIMAKAMMER_NOTES = true;  // Notiz am Anfang: Ob Klimakammer an ist und eine am Ende, ob sie aus ist anfügen?

        public string E21_SONGS_PATH;
        public string PRAYER_POINTS_PPT_PATH;
        public string URL_EXCEPTIONS_PATH;
        public string CCLI_DICT_PATH;

        public readonly string ELVANTO_URL = "https://fegmm.elvanto.eu";
        public readonly string SONGS_PART_URL = "/songs/";       

        private Settings()
        {
            E21_SONGS_PATH = $"{SCRIPTS_PATH}\\E21-Liederbuch (ohne Kommata).txt";
            URL_EXCEPTIONS_PATH = $"{SCRIPTS_PATH}\\ElvantoURLExceptions.txt";
            CCLI_DICT_PATH = $"{SONGS_PATH}\\CcliDictionary.jstxt";
            PRAYER_POINTS_PPT_PATH = $"{IMAGES_DIASHOW_PATH}\\Gebetsanliegen.pptx";

            // TODO:  Falls PICTURES_DIASHOW_PATH nicht existiert, den einen existierenden Ordner in dem Pfad nehmen
        }

        public static Settings Instance  {  get {  return instance;  } }
    }
}

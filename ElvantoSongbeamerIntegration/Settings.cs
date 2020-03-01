namespace SongbeamerSongbookIntegrator
{
    public sealed class Settings
    {
        private static readonly Settings instance = new Settings();
        public readonly string PPTS_PATH               = @"C:\Nextcloud\Medientechnik\2 PPT-Liedfolien und PPT-Master";
        public readonly string SONGS_PATH              = @"C:\Nextcloud\Medientechnik\SongBeamer\Lieder";
        public readonly string IMAGES_PATH             = @"C:\Nextcloud\Medientechnik\SongBeamer\Bilder";
        public readonly string MEDIA_PATH              = @"C:\Nextcloud\Medientechnik\SongBeamer\Medien für Gottesdienste";
        public readonly string DIASHOW_IMAGES_PATH     = @"C:\Nextcloud\Medientechnik\SongBeamer\Medien für Gottesdienste\Für Diashow am Ende";
        public readonly string ANNOUNCEMENTS_PATH      = @"C:\Nextcloud\Medientechnik\SongBeamer\Medien für Gottesdienste\Ansagen kommender Sonntag";
        public readonly string SCRIPTS_PATH            = @"C:\Nextcloud\Medientechnik\SongBeamer\Scripts";
        public readonly string SERVICES_PATH           = @"C:\Nextcloud\Medientechnik\SongBeamer\Abläufe";
        public readonly string SERVICES_YOUTH_PATH     = @"C:\Nextcloud\Medientechnik\SongBeamer\Abläufe\_Jugend";
        public readonly string SERVICES_TEMPLATES_PATH = @"C:\Nextcloud\Medientechnik\SongBeamer\Gottesdienst-Vorlagen";

        public readonly string TEMPLATE_FILES_FOLDER   = "Folien für Vorlagen";

        public readonly string ALLOWED_MEDIA_EXTENSIONS = ".pdf|.jpg|.jpeg|.png|.wav|.mp3|.mp4|.mov|.avi|.wmv|.ppt|.pptx|.sng";

        public readonly bool ADD_KLIMAKAMMER_NOTES = true;  // Notiz am Anfang: Ob Klimakammer an ist und eine am Ende, ob sie aus ist anfügen?
        public readonly bool ADD_NOTE_FOR_VORTRAGSLIED = true;  // Falls ein Vortragslied gesungen wird, dafür nur eine Notzi-Anzeigen (Text nicht nötig)?

        public string E21_SONGS_PATH;
        public string PRAYER_POINTS_PPT_PATH;
        public string URL_EXCEPTIONS_PATH;
        public string CCLI_DICT_PATH;
        public string ENGLISH_WORDS_PATH;
        public string GERMAN_WORDS_PATH;

        public readonly string ELVANTO_URL = "https://fegmm.elvanto.eu";
        public readonly string SONGS_PART_URL = "/songs/";       

        private Settings()
        {
            E21_SONGS_PATH         = $"{SCRIPTS_PATH}\\data\\E21-Liederbuch (ohne Kommata).txt";
            URL_EXCEPTIONS_PATH    = $"{SCRIPTS_PATH}\\data\\ElvantoURLExceptions.txt";
            ENGLISH_WORDS_PATH     = $"{SCRIPTS_PATH}\\data\\Top5000EnglishWords.txt";
            GERMAN_WORDS_PATH      = $"{SCRIPTS_PATH}\\data\\Top5000GermanWords.txt";

            CCLI_DICT_PATH         = $"{SONGS_PATH}\\CcliDictionary.jstxt";
            PRAYER_POINTS_PPT_PATH = $"{MEDIA_PATH}\\Gebetsanliegen.pptx";

            // TODO:  Falls PICTURES_DIASHOW_PATH nicht existiert, den einen existierenden Ordner in dem Pfad nehmen
        }

        public static Settings Instance  {  get {  return instance;  } }
    }
}

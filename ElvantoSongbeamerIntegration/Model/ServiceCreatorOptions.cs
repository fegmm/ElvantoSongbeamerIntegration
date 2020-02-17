namespace ElvantoSongbeamerIntegration.Model
{
    public class ServiceCreatorOptions
    {
        public bool   IsForYouth             { get; set; }
        public bool   RecognizeSermon        { get; set; }
        public bool   RecognizeOptionalSongs { get; set; }
        public bool   UseCCLI                { get; set; }
        public bool   OpenSongbeamer         { get; set; }
        public bool   PptAsSecondChance      { get; set; }
    }
}

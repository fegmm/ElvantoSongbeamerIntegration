namespace ElvantoSongbeamerIntegration.Model
{
    public class ServiceItemData : System.Attribute
    {
        public string Color { get; set; }
        public string BGColor { get; set; }
    }

    public enum ServiceItemType
    {
        None,
        Error,
        [ServiceItemData(Color = "clBlue", BGColor = null)] Song,                // -> Color = clBlue
        [ServiceItemData(Color = "11731032", BGColor = null)] TextReading,       // -> Color = 11731032
        [ServiceItemData(Color = "clWhite", BGColor = "16744448")] Note,         // -> Color = clWhite (16777215),  BGColor = 16744448,  noFileName
        [ServiceItemData(Color = "clWhite", BGColor = "30975")] Note2,           // -> Color = clWhite (16777215),  BGColor = 30975,  noFileName
        [ServiceItemData(Color = "clGreen", BGColor = null)] Image,              // -> Color = clGreen
        [ServiceItemData(Color = "clBlack", BGColor = null)] PDF,
        [ServiceItemData(Color = "clBlack", BGColor = null)] PPT,
        [ServiceItemData(Color = "clBlack", BGColor = null)] Audio,
        [ServiceItemData(Color = "clRed", BGColor = null)] Video,
        [ServiceItemData(Color = "clBlack", BGColor = null)] Diashow
    }
}
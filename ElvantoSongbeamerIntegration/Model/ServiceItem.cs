using ElvantoSongbeamerIntegration.Controller;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ElvantoSongbeamerIntegration.Model
{
    public class ServiceItem
    {
        public static string        NewLine = "\r\n";

        private string              Caption;
        private string              Filename;
        private ServiceItemType     Type;
        private List<ServiceItem>   SubItems; // Für Diashow

        public ServiceItem(string caption, string filename, ServiceItemType type)
        {
            this.Caption  = caption;
            this.Filename = filename;
            this.Type     = type;
            this.SubItems = new List<ServiceItem>();
        }

        public override string ToString()
        {
            var content = "    item" + NewLine;

            content += $"      Caption = {SplitStringIfOver64CharsAddSingleQuote(Caption)}" + NewLine;
            content += $"      Color = {GetAttr(Type).Color}" + NewLine;
            if (Type == ServiceItemType.Diashow) { return DiashowToString(content); }

            if (GetAttr(Type).BGColor != null)   { content += $"      BGColor = {GetAttr(Type).BGColor}" + NewLine; }
            if (!string.IsNullOrEmpty(Filename)) { content += $"      FileName = {SplitStringIfOver64CharsAddSingleQuote(Filename)}" + NewLine; }

            return content + "    end" + NewLine;
        }

        public void AddSubItemForDiashow(ServiceItem image)
        {
            if (image.Type != ServiceItemType.Image || Type != ServiceItemType.Diashow) { return; }

            SubItems.Add(image);
        }

        public static ServiceItemType? GetItemTypeFromExtension(string extension)
        {
            switch (extension.ToLower())
            {
                case ".jpg":
                case ".png":
                    return ServiceItemType.Image;

                case ".pdf":
                    return ServiceItemType.PDF;

                case ".mp3":
                case ".wav":
                    return ServiceItemType.Audio;

                case ".ppt":
                case ".pptx":
                    return ServiceItemType.PPT;

                case ".mp4":
                case ".mov":
                case ".wmv":
                case ".avi":
                    return ServiceItemType.Video;

                case ".sng":
                    return ServiceItemType.Song;

                default:
                    return null;
            }
        }

        private string DiashowToString(string content)
        {
            content += $"      StreamClass = 'TPresentationSlideShow'" + NewLine;
            content += $"      GUID = '{{{Guid.NewGuid().ToString().ToUpper()}}}'" + NewLine;

            var data = $"object PresentationSlideShow: TPresentationSlideShow{NewLine}  SlideCollection = <";
            foreach (var item in SubItems)
            {
                data += $"    item{NewLine}      FileName = {SplitStringIfOver64CharsAddSingleQuote(item.Filename)}{NewLine}    end{NewLine}";
            }
            data = data.Remove(data.Length - 1);
            data += $"    >{NewLine}";

            data += $"  Loop = True{NewLine}  FitToScreen = False{NewLine}end";

            var dataEnc = ASCIItoHex(data);
            content += "      Data = {" + NewLine + FormatsHexString(dataEnc) + "}" + NewLine;

            return content + "    end" + NewLine;
        }

        private static string FormatsHexString(string dataEnc)
        {
            var output = "";
            var i = 1;

            // Hex-Zeichen in Länge von je 64 Bit umbrechen
            while(dataEnc.Length > 64 * i)
            {
                output += "        " + dataEnc.Substring((i - 1) * 64, 64) + NewLine;
                i++;
            }
            if (dataEnc.Length % 64 != 0) { output += "        " + dataEnc.Substring((i - 1) * 64); }
            else
            {
                output = output.Remove(dataEnc.Length - 1);
            }

            return output;
        }

        public static string ConvertHex(string hexString)
        {
            try
            {
                string ascii = string.Empty;

                for (int i = 0; i < hexString.Length; i += 2)
                {
                    string hs = string.Empty;

                    hs = hexString.Substring(i, 2);
                    ulong decval = Convert.ToUInt64(hs, 16);
                    long deccc = Convert.ToInt64(hs, 16);
                    char character = Convert.ToChar(deccc);
                    ascii += character;

                }

                return ascii;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            return string.Empty;
        }

        public string ASCIItoHex(string Value)
        {
            StringBuilder sb = new StringBuilder();

            byte[] inputByte = Encoding.UTF8.GetBytes(Value);

            foreach (byte b in inputByte)
            {
                sb.Append(string.Format("{0:X2}", b));
            }

            return sb.ToString();
        }

        private static ServiceItemData GetAttr(ServiceItemType p)
        {
            return (ServiceItemData)Attribute.GetCustomAttribute(ForValue(p), typeof(ServiceItemData));
        }

        private static MemberInfo ForValue(ServiceItemType p)
        {
            return typeof(ServiceItemType).GetField(Enum.GetName(typeof(ServiceItemType), p));
        }

        private string SplitStringIfOver64CharsAddSingleQuote(string filename)
        {
            var utf = SongSheetOpener.UmlautsUnicodeToUTF8(filename);

            if (utf.Length > 64)
            {
                var part1 = SongSheetOpener.UmlautsUTF8ToUnicode(utf.Substring(0, 64));
                var part2 = SongSheetOpener.UmlautsUTF8ToUnicode(utf.Substring(64));
                return $"{NewLine}        '{part1}' +{NewLine}        '{part2}'";
            }
            return $"'{filename}'";
        }
    }
}

using System;
using System.Reflection;

namespace SongbeamerSongbookIntegrator.Model
{
    public class ServiceTemplateItemData : System.Attribute
    {
        public string Abbreviation { get; set; }
        public string Filename { get; set; }

        public static ServiceTemplateItemData GetAttr(ServiceTemplateType p)
        {
            return (ServiceTemplateItemData)Attribute.GetCustomAttribute(ForValue(p), typeof(ServiceTemplateItemData));
        }

        public static MemberInfo ForValue(ServiceTemplateType p)
        {
            return typeof(ServiceTemplateType).GetField(Enum.GetName(typeof(ServiceTemplateType), p));
        }
    }

    public enum ServiceTemplateType
    {
        [ServiceTemplateItemData(Abbreviation = "mo", Filename = "Songbeamer Morgengottesdienst-Vorlage.col")] MorningService,
        [ServiceTemplateItemData(Abbreviation = "mi", Filename = "Songbeamer Mittaggottesdienst-Vorlage.col")] MiddayService,
        [ServiceTemplateItemData(Abbreviation = "a", Filename = "Songbeamer Abendgottesdienst-Vorlage.col")] EveningService,
        [ServiceTemplateItemData(Abbreviation = "Jugend")] Youth
    }
}
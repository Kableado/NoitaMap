using System.Xml.Serialization;
using NoitaMap.Logging;

namespace NoitaMap;

public static class XmlUtility
{
    public static T LoadXml<T>(string xmlContent) where T : class
    {
        XmlSerializer gfxSerializer = new XmlSerializer(typeof(T));
        using StringReader xmlReader = new StringReader(xmlContent);
        T? result = null;

        try
        {
            result = gfxSerializer.Deserialize(xmlReader) as T;
        }
        catch
        {
            Logger.LogWarning($"Failure reading XML: {xmlContent[..100]}");
        }

        if (result == null)
        {
            throw new Exception($"Failure reading XML: {xmlContent[..100]}");
        }
        
        return result;
    }
}

using System.Collections.Concurrent;
using System.Numerics;
using System.Xml;
using NoitaMap.Graphics;
using NoitaMap.Graphics.Atlases;
using NoitaMap.Logging;
using Veldrid;

namespace NoitaMap.Map;

public class AreaContainer : IRenderable
{
    private readonly ConcurrentQueue<AreaEntity> ThreadedAreaQueue = new ConcurrentQueue<AreaEntity>();

    public readonly List<AreaEntity> AreaEntities = new List<AreaEntity>();

    public IReadOnlyList<AreaEntitySprite> AreaEntitySprites => AreaSpriteAtlas.AtlasObjects;

    private readonly QuadObjectAtlasBuffer<AreaEntitySprite> AreaSpriteAtlas;

    private bool Disposed;

    public AreaContainer(Renderer renderer)
    {
        AreaSpriteAtlas = new QuadObjectAtlasBuffer<AreaEntitySprite>(renderer);
    }

    public void LoadArea(string path)
    {
        byte[] decompressedData = NoitaFile.LoadCompressedFile(path);

        using (MemoryStream ms = new MemoryStream(decompressedData))
        {
            using BinaryReader reader = new BinaryReader(ms);

            int numberThatsZeroIThink = reader.ReadBEInt32();

            int numberThatsInPathName = reader.ReadBEInt32();

            int numberThatsOneIthink = reader.ReadBEInt32();

            int numberOfPositions = reader.ReadBEInt32();

            Dictionary<int, List<Vector2>> positionData = new Dictionary<int, List<Vector2>>();

            for (int i = 0; i < numberOfPositions; i++)
            {
                int index = reader.ReadBEInt32();
                float x = reader.ReadBESingle();
                float y = reader.ReadBESingle();

                if (!positionData.TryGetValue(index, out List<Vector2>? positions))
                {
                    positionData.Add(index, positions = new List<Vector2>());
                }

                positions.Add(new Vector2(x, y));
            }

            int numberOfEntityXml = reader.ReadBEInt32();

            for (int i = 0; i < numberOfEntityXml; i++)
            {
                string xmlFilePath = reader.ReadNoitaString() ?? "";
                int bigNumberIdk = reader.ReadBEInt32();
                int smallerNumberMaybeHealthIdk = reader.ReadBEInt32();

                try
                {
                    foreach (Vector2 pos in positionData[i])
                    {
                        ThreadedAreaQueue.Enqueue(new AreaEntity(xmlFilePath, pos));

                        LoadEntitySprite(xmlFilePath, pos);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error while loading entity {xmlFilePath} for area file {path}:");
                    Logger.LogWarning(ex);
                }
            }
        }
    }

    private void LoadEntitySprite(string xmlFilePath, Vector2 pos)
    {
        string caselessBaseXmlFilePath = xmlFilePath.ToLower();

        string? fullXmlPath = null;
        if (caselessBaseXmlFilePath.StartsWith("data/"))
        {
            fullXmlPath = Path.Combine(PathService.DataPath, caselessBaseXmlFilePath.Remove(0, 5));
        }

        if (fullXmlPath is null || !File.Exists(fullXmlPath))
        {
            return;
        }

        string baseXmlContent = PreProcessXml(xmlFilePath, File.ReadAllText(fullXmlPath));

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(baseXmlContent);

        XmlNodeList? spriteNodes = xmlDoc.SelectNodes("//SpriteComponent");

        if (spriteNodes is null)
        {
            return;
        }

        foreach (XmlNode node in spriteNodes)
        {
            AreaSpriteAtlas.AddAtlasObject(new AreaEntitySprite(node, pos));
        }
    }

    public void Update()
    {
        while (ThreadedAreaQueue.TryDequeue(out AreaEntity? area))
        {
            AreaEntities.Add(area);
        }

        AreaSpriteAtlas.Update();
    }

    public void Render(CommandList commandList)
    {
        AreaSpriteAtlas.Draw(commandList);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            AreaSpriteAtlas.Dispose();

            Disposed = true;
        }
    }

    private string PreProcessXml(string xmlPath, string xmlContent)
    {
        if (xmlPath == "data/entities/animals/worm_big.xml")
        {
            xmlContent = xmlContent.Replace("next_rect_animation=\"eat\" \r\n\t\tnext_rect_animation=\"\"", "next_rect_animation=\"eat\"");
        }

        if (xmlPath == "data/entities/animals/fireskull.xml")
        {
            xmlContent = xmlContent.Replace("count_min=\"5\"\r\n    count_max=\"5\"", "");
        }

        return xmlContent;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

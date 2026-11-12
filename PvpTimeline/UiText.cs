using System.Reflection;
namespace PvpTimeline;

/// <summary>Small, bounded UTF-8 resources. Loaded once; never read files on a game frame.</summary>
internal static class UiText
{
    private static readonly Dictionary<string,System.Text.Json.JsonDocument> names=new();
    private static readonly Dictionary<(int,string),string> translated=new();
    public static void Release(){foreach(var doc in names.Values)doc.Dispose();names.Clear();}
    public static string GameName(string kind,uint id,int language,string fallback)
    {
        if(!names.TryGetValue(kind,out var doc))
        {
            using var stream=typeof(UiText).Assembly.GetManifestResourceStream("PvpTimeline."+kind+".json");
            if(stream==null)return fallback;
            doc=System.Text.Json.JsonDocument.Parse(stream);names[kind]=doc;
        }
        var key=language switch{0=>"zhHans",1=>"zhHant",2=>"en",3=>"de",4=>"fr",5=>"ja",_=>"en"};
        if(doc.RootElement.GetProperty(kind).TryGetProperty(id.ToString(),out var entry))
        {
            if(entry.TryGetProperty(key,out var text) && !string.IsNullOrWhiteSpace(text.GetString()))return text.GetString()!;
            if(language>1 && entry.TryGetProperty("en",out var english) && !string.IsNullOrWhiteSpace(english.GetString()))return english.GetString()!;
        }
        // Never label a Chinese client fallback as English/German/French.
        if(language is 2 or 3 or 4 && fallback.Any(c=>c is >= '\u3040' and <= '\u30ff' or >= '\u3400' and <= '\u9fff'))
            return (kind=="actions"?T("Action"):T("Status"))+" #"+id;
        return fallback;
    }
    public static string SourceName(string value)=>value=="自身 / self"?T("Self"):value=="未知 / unknown"?T("Unknown"):value.StartsWith("未观测来源 / unseen #",StringComparison.Ordinal)?T("Unseen source")+" #"+value.Split('#')[^1]:value;
    public static int Language;
    public static int DefaultLanguage(string? culture)=>culture?.ToLowerInvariant() switch
    {"zh" or "zh-cn" or "zh-hans"=>0,"tw" or "zh-tw" or "zh-hk" or "zh-hant"=>1,"de"=>3,"fr"=>4,"ja"=>5,_=>2};
    private static readonly Dictionary<string,string[]> labels=Load();
    private static Dictionary<string,string[]> Load()
    {
        var result=new Dictionary<string,string[]>(StringComparer.Ordinal);
        var assembly=typeof(UiText).Assembly;
        using(var stream=assembly.GetManifestResourceStream("PvpTimeline.ui-localization.txt"))
        {if(stream!=null){using var reader=new StreamReader(stream);Read(reader,result);}}
        return result;
    }
    /// <summary>Optional sidecar lookup uses the host-supplied directory, never Assembly.Location.
    /// A bad translation file must not poison the type initializer or prevent plugin unloading.</summary>
    public static string LoadOverride(string? directory)
    {
        if(string.IsNullOrWhiteSpace(directory))return "embedded:no_directory";
        try
        {
            var path=Path.Combine(directory,"ui-localization.txt");
            if(!File.Exists(path))return "embedded:no_override";
            if(new FileInfo(path).Length>262144)return "embedded:override_too_large";
            var overrides=new Dictionary<string,string[]>(StringComparer.Ordinal);
            using(var reader=File.OpenText(path))Read(reader,overrides);
            foreach(var pair in overrides)labels[pair.Key]=pair.Value;
            translated.Clear();return "override_loaded";
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {return "embedded:"+ex.GetType().Name;}
    }
    private static void Read(TextReader reader,Dictionary<string,string[]> target)
    {
        for(var n=0;n<512 && reader.ReadLine() is {} line;n++)
        {if(line.StartsWith('#'))continue;var columns=line.Split('|');if(columns.Length==6)target[columns[0]]=columns;}
    }
    public static string T(string text)
    {
        if(translated.TryGetValue((Language,text),out var cached))return cached;
        var parts=text.Split(" / ");var key=parts[^1];
        var id=key.IndexOf("##",StringComparison.Ordinal);var suffix=id>=0?key[id..]:"";if(id>=0)key=key[..id];
        if(!labels.TryGetValue(key,out var row))return Language==2?key+suffix:text;
        var column=Language switch{0=>1,1=>2,2=>0,3=>3,4=>4,5=>5,_=>0};
        if(translated.Count==1024)translated.Clear();
        return translated[(Language,text)]=row[column]+suffix;
    }
}

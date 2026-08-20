using System.Text.Json.Serialization;

namespace Starward.Core.GameNotice;

/// <summary>
/// getAnnList 接口响应中的 data 部分
/// </summary>
public class AnnListData
{

    /// <summary>
    /// 按类型分组的公告列表
    /// </summary>
    [JsonPropertyName("list")]
    public List<AnnListTypeGroup> List { get; set; } = [];


    [JsonPropertyName("total")]
    public int Total { get; set; }


    /// <summary>
    /// 服务器时区偏移（小时），如东八区为 8
    /// </summary>
    [JsonPropertyName("timezone")]
    public int Timezone { get; set; } = 8;


    [JsonPropertyName("type_list")]
    public List<AnnTypeInfo> TypeList { get; set; } = [];

}


/// <summary>
/// 按类型分组的一组公告
/// </summary>
public class AnnListTypeGroup
{

    [JsonPropertyName("type_id")]
    public int TypeId { get; set; }


    [JsonPropertyName("type_label")]
    public string TypeLabel { get; set; } = "";


    [JsonPropertyName("list")]
    public List<GameAnnouncement> List { get; set; } = [];

}


/// <summary>
/// 公告类型信息
/// </summary>
public class AnnTypeInfo
{

    [JsonPropertyName("id")]
    public int Id { get; set; }


    [JsonPropertyName("name")]
    public string Name { get; set; } = "";


    [JsonPropertyName("mi18n_name")]
    public string Mi18nName { get; set; } = "";

}


/// <summary>
/// getAnnContent 接口响应中的 data 部分
/// </summary>
public class AnnContentData
{

    [JsonPropertyName("list")]
    public List<AnnContentItem> List { get; set; } = [];

}


public class AnnContentItem
{

    /// <summary>
    /// 公告内容 HTML
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

}

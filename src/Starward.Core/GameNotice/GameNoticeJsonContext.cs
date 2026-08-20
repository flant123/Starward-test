using System.Text.Json.Serialization;

namespace Starward.Core.GameNotice;


[JsonSerializable(typeof(miHoYoApiWrapper<AlertAnn>))]
[JsonSerializable(typeof(miHoYoApiWrapper<AnnListData>))]
[JsonSerializable(typeof(miHoYoApiWrapper<AnnContentData>))]
internal partial class GameNoticeJsonContext : JsonSerializerContext
{

}

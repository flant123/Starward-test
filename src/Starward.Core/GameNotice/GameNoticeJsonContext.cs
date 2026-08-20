using System.Text.Json.Serialization;

namespace Starward.Core.GameNotice;


[JsonSerializable(typeof(miHoYoApiWrapper<AlertAnn>))]
[JsonSerializable(typeof(miHoYoApiWrapper<AnnListData>))]
internal partial class GameNoticeJsonContext : JsonSerializerContext
{

}

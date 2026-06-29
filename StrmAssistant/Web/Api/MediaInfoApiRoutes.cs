using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace StrmAssistant.Web.Api
{
    [Route("/Items/{Id}/MediaInfo", "GET")]
    [Authenticated(Roles = "Admin")]
    public class GetMediaInfo : IReturn<MediaInfoResponse>, IReturn
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path")]
        public string Id { get; set; }

        [ApiMember(Name = "Source", Description = "Media source (optional)", IsRequired = false, DataType = "string", ParameterType = "query")]
        public string Source { get; set; }
    }

    [Route("/Items/{Id}/ExtractMediaInfo", "POST")]
    [Authenticated(Roles = "Admin")]
    public class ExtractMediaInfo : IReturnVoid, IReturn
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path")]
        public string Id { get; set; }

        [ApiMember(Name = "Overwrite", Description = "Overwrite existing", IsRequired = false, DataType = "bool", ParameterType = "query")]
        public bool Overwrite { get; set; }
    }

    [Route("/Items/{Id}/DeleteMediaInfo", "DELETE")]
    [Authenticated(Roles = "Admin")]
    public class DeleteMediaInfoRequest : IReturnVoid, IReturn
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path")]
        public string Id { get; set; }

        [ApiMember(Name = "Source", Description = "Media source (optional)", IsRequired = false, DataType = "string", ParameterType = "query")]
        public string Source { get; set; }
    }

    public class MediaInfoResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool HasMediaInfo { get; set; }
        public string Error { get; set; }
    }
}

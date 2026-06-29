using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Services;

namespace StrmAssistant.Web.Api
{
    [Route("/Items/{Id}/ClearChapterImage", "POST")]
    [Authenticated(Roles = "Admin")]
    public class ClearChapterImage : IReturnVoid, IReturn
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path")]
        public string Id { get; set; }
    }

    [Route("/Items/{Id}/ClearMediaInfo", "POST")]
    [Authenticated(Roles = "Admin")]
    public class ClearMediaInfo : IReturnVoid, IReturn
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path")]
        public string Id { get; set; }
    }

    [Route("/Items/SyncMediaInfo", "POST")]
    [Authenticated(Roles = "Admin")]
    public class SyncMediaInfo : IReturn<List<MediaInfoBundle>>, IReturn
    {
        [ApiMember(Name = "Id", Description = "Preferred Item Id", IsRequired = false, DataType = "string", ParameterType = "query")]
        public string Id { get; set; }

        [ApiMember(Name = "Path", Description = "Optional Item Path", IsRequired = false, DataType = "string", ParameterType = "query")]
        public string Path { get; set; }
    }

    [Route("/Items/{Id}/DeletePerson", "POST")]
    [Authenticated(Roles = "Admin")]
    public class DeletePerson : IReturnVoid, IReturn
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path")]
        public string Id { get; set; }
    }

    public class MediaInfoBundle
    {
        public MediaSourceInfo MediaSourceInfo { get; set; }
        public List<ChapterInfo> Chapters { get; set; }
        public bool? ZeroFingerprintConfidence { get; set; }
        public string EmbeddedImage { get; set; }
    }
}

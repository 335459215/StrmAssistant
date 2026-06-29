using System.Collections.Generic;

namespace StrmAssistant.Dto.Douban
{
    /// <summary>
    /// Douban movie/TV detail API response
    /// </summary>
    public class DoubanDetailResponse
    {
        public RatingDetail Rating { get; set; }
        public List<string> Pubdate { get; set; } = new List<string>();
        public Picture Pic { get; set; }
        public bool IsTv { get; set; }
        public string CardSubtitle { get; set; }
        public string Year { get; set; }
        public string Id { get; set; }
        public List<string> Languages { get; set; } = new List<string>();
        public List<string> Genres { get; set; } = new List<string>();
        public string Title { get; set; }
        public string Intro { get; set; }
        public bool HasLinewatch { get; set; }
        public bool IsReleased { get; set; }
        public List<Celebrity> Actors { get; set; } = new List<Celebrity>();
        public int EpisodesCount { get; set; }
        public string Type { get; set; }
        public List<string> Durations { get; set; } = new List<string>();
        public List<string> Countries { get; set; } = new List<string>();
        public string Url { get; set; }
        public string OriginalTitle { get; set; }
        public string Uri { get; set; }
        public string Subtype { get; set; }
        public List<Celebrity> Directors { get; set; } = new List<Celebrity>();
        public bool IsShow { get; set; }
        public bool InBlacklist { get; set; }
        public List<string> Aka { get; set; } = new List<string>();
        public bool IsRestrictive { get; set; }

        public DoubanDetailResponse() { }
    }

    public class RatingDetail
    {
        public int Count { get; set; }
        public int Max { get; set; }
        public float StarCount { get; set; }
        public float Value { get; set; }

        public RatingDetail() { }
    }

    /// <summary>
    /// Douban error response
    /// </summary>
    public class DoubanErrorResponse
    {
        public string Msg { get; set; }
        public int Code { get; set; }
        public string Request { get; set; }
        public string LocalizedMessage { get; set; }

        public DoubanErrorResponse() { }
    }

    /// <summary>
    /// Douban info API response (older API format)
    /// </summary>
    public class DoubanInfoResponse
    {
        public RatingInfo Rating { get; set; }
        public string AltTitle { get; set; }
        public string Image { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public Attributes Attrs { get; set; }
        public string Id { get; set; }

        public DoubanInfoResponse() { }
    }

    public class RatingInfo
    {
        public int Max { get; set; }
        public string Average { get; set; }
        public int NumRaters { get; set; }
        public int Min { get; set; }

        public RatingInfo() { }
    }

    public class Attributes
    {
        public List<string> Pubdate { get; set; } = new List<string>();
        public List<string> Language { get; set; } = new List<string>();
        public List<string> Country { get; set; } = new List<string>();
        public List<string> Year { get; set; } = new List<string>();
        public List<string> MovieType { get; set; } = new List<string>();
        public List<string> Episodes { get; set; } = new List<string>();
        public List<string> Director { get; set; } = new List<string>();
        public List<string> Cast { get; set; } = new List<string>();
    }

    /// <summary>
    /// Douban seasons API response
    /// </summary>
    public class DoubanSeasonsResponse
    {
        public RatingDetail Rating { get; set; }
        public List<string> Pubdate { get; set; } = new List<string>();
        public Picture Pic { get; set; }
        public bool IsShow { get; set; }
        public string Year { get; set; }
        public string CardSubtitle { get; set; }
        public string Id { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public string Title { get; set; }
        public bool IsReleased { get; set; }
        public string Type { get; set; }
        public bool HasLinewatch { get; set; }
        public string CoverUrl { get; set; }
        public string Url { get; set; }
        public string ReleaseDate { get; set; }
        public string Uri { get; set; }
        public string Subtype { get; set; }

        public DoubanSeasonsResponse() { }
    }

    /// <summary>
    /// Douban celebrity API response
    /// </summary>
    public class DoubanCelebrityResponse
    {
        public List<Celebrity> Directors { get; set; } = new List<Celebrity>();
        public List<Celebrity> Actors { get; set; } = new List<Celebrity>();

        public DoubanCelebrityResponse() { }
    }

    /// <summary>
    /// Douban abstract search response
    /// </summary>
    public class DoubanAbstractResponse
    {
        public List<SubjectResult> Subjects { get; set; } = new List<SubjectResult>();

        public DoubanAbstractResponse() { }
    }

    public class SubjectResult
    {
        public string EpisodesCount { get; set; }
        public float Star { get; set; }
        public string Blacklisted { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string CollectionStatus { get; set; }
        public string Rate { get; set; }
        public ShortComment ShortComment { get; set; }
        public bool IsTv { get; set; }
        public string Subtype { get; set; }
        public List<string> Directors { get; set; } = new List<string>();
        public List<string> Actors { get; set; } = new List<string>();
        public string Duration { get; set; }
        public string Region { get; set; }
        public bool Playable { get; set; }
        public string Id { get; set; }
        public List<string> Types { get; set; } = new List<string>();
        public string ReleaseYear { get; set; }

        public SubjectResult() { }
    }

    public class ShortComment
    {
        public string Content { get; set; }
        public string Author { get; set; }
        public string Rating { get; set; }
        public SubjectResult Subject { get; set; }

        public ShortComment() { }
    }

    // Shared types used across multiple response types

    public class Celebrity
    {
        public string Name { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string Title { get; set; }
        public string Url { get; set; }
        public string Character { get; set; }
        public Picture Avatar { get; set; }
        public int? Id { get; set; }
        public string LatinName { get; set; }
        public int Code { get; set; }
        public string Request { get; set; }
        public string LocalizedMessage { get; set; }

        public Celebrity() { }
    }

    public class Picture
    {
        public string Large { get; set; }
        public string Normal { get; set; }

        public Picture() { }
    }

    // ─── Legacy compatibility ───

    public class DoubanSubject
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public string AltTitle { get; set; }
        public string Year { get; set; }
        public DoubanRating Rating { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public string Countries { get; set; }
        public string Summary { get; set; }
        public string Intro { get; set; }
        public string Subtype { get; set; }
        public List<DoubanCelebrityOld> Directors { get; set; } = new List<DoubanCelebrityOld>();
        public List<DoubanCelebrityOld> Actors { get; set; } = new List<DoubanCelebrityOld>();
        public DoubanImages Images { get; set; }
        public string Pubdate { get; set; }
        public string Duration { get; set; }
    }

    public class DoubanRating
    {
        public double Average { get; set; }
        public int Stars { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public int NumRaters { get; set; }
    }

    public class DoubanCelebrityOld
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Alt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public DoubanImages Avatars { get; set; }
        public string LatinName { get; set; }
    }

    public class DoubanImages
    {
        public string Small { get; set; }
        public string Medium { get; set; }
        public string Large { get; set; }
    }

    public class DoubanSearchResult
    {
        public List<DoubanSubject> Subjects { get; set; } = new List<DoubanSubject>();
        public int Start { get; set; }
        public int Count { get; set; }
        public int Total { get; set; }
        public string Title { get; set; }
    }

    public class DoubanSubjectAttribute
    {
        public string Key { get; set; }
        public List<string> Values { get; set; } = new List<string>();
    }
}

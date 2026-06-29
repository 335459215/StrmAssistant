using System.Collections.Generic;

namespace StrmAssistant.Dto.Douban
{
    public class DoubanSubject
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public string alt_title { get; set; }
        public string Year { get; set; }
        public DoubanRating Rating { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public string countries { get; set; }
        public string summary { get; set; }
        public string Intro { get; set; }
        public string Subtype { get; set; }
        public List<DoubanCelebrity> Directors { get; set; } = new List<DoubanCelebrity>();
        public List<DoubanCelebrity> Actors { get; set; } = new List<DoubanCelebrity>();
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

    public class DoubanCelebrity
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

using System;

namespace StrmAssistant.Dto.IntroDb
{
    public class IntroDbEntry
    {
        public string Id { get; set; }
        public string SeriesName { get; set; }
        public string SeasonNumber { get; set; }
        public string EpisodeNumber { get; set; }
        public long IntroStartTicks { get; set; }
        public long IntroEndTicks { get; set; }
        public long CreditsStartTicks { get; set; }
        public bool IsValid { get; set; }
        public string Source { get; set; }
        public DateTime LastModified { get; set; }
        public double Confidence { get; set; }
    }

    public class IntroDbQueryResult
    {
        public bool Found { get; set; }
        public IntroDbEntry Entry { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class IntroDbSubmitRequest
    {
        public string SeriesName { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public long IntroStartTicks { get; set; }
        public long IntroEndTicks { get; set; }
        public long CreditsStartTicks { get; set; }
        public double Confidence { get; set; }
        public string ApiKey { get; set; }
    }

    public class IntroDbSubmitResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}

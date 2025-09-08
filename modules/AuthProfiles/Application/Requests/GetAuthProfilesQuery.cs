using System;

namespace AuthProfiles.Application.Requests
{
    public class GetAuthProfilesQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 25;
        public bool? Enabled { get; init; }
        public Guid? ProjectId { get; init; }
        public Guid? ServiceId { get; init; }
        public string? Env { get; init; }
        public string? Search { get; init; }
    }
}

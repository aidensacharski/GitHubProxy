namespace GitHubProxy.Proxy
{
    public class GitHubProxyOptions
    {
        public bool UseProxy { get; set; }
        public string? Proxy { get; set; }

        public string? HomeDomain { get; set; }
        public string? BlackholeDomain { get; set; }
        public string? AssetsDomain { get; set; }
        public string? AvatarsDomain { get; set; }
        public string? RawDomain { get; set; }
        public string? CamoDomain { get; set; }
        public string? CodeloadDomain { get; set; }
        public string? ReleasesDomain { get; set; }
        public string? UserImagesDomain { get; set; }
        public string? ObjectsDomain { get; set; }
    }
}

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GitHubProxy.Proxy
{
    public interface IGitHubProxyConfiguration
    {
        string AssetsDomain { get; }
        string AssetsDomainAuthority { get; }
        string AssetsDomainHost { get; }
        string AvatarsDomain { get; }
        string AvatarsDomainAuthority { get; }
        string AvatarsDomainHost { get; }
        string BlackholeDomain { get; }
        string BlackholeDomainAuthority { get; }
        string BlackholeDomainHost { get; }
        string HomeDomain { get; }
        string HomeDomainAuthority { get; }
        string HomeDomainHost { get; }
        string RawDomain { get; }
        string RawDomainAuthority { get; }
        string RawDomainHost { get; }
        string CamoDomain { get; }
        string CamoDomainAuthority { get; }
        string CamoDomainHost { get; }
        string CodeloadDomain { get; }
        string CodeloadDomainAuthority { get; }
        string CodeloadDomainHost { get; }
        string ReleasesDomain { get; }
        string ReleasesDomainAuthority { get; }
        string ReleasesDomainHost { get; }
        string UserImagesDomain { get; }
        string UserImagesDomainAuthority { get; }
        string UserImagesDomainHost { get; }
        string ObjectsDomain { get; }
        string ObjectsDomainAuthority { get; }
        string ObjectsDomainHost { get; }

        bool IsConfigured { get; }
        bool UseProxy { get; }
        string? Proxy { get; }
    }

    public class GitHubProxyConfiguration : IGitHubProxyConfiguration
    {
        private readonly bool _isConfigured;
        private readonly bool _useProxy;
        private readonly string? _proxy;

        private readonly string? _homeDomain;
        private readonly string? _blackholeDomain;
        private readonly string? _assetsDomain;
        private readonly string? _avatarsDomain;
        private readonly string? _rawDomain;
        private readonly string? _camoDomain;
        private readonly string? _codeloadDomain;
        private readonly string? _releasesDomain;
        private readonly string? _userImagesDomain;
        private readonly string? _objectsDomain;

        private readonly Uri? _homeDomainUri;
        private readonly Uri? _blackholeDomainUri;
        private readonly Uri? _assetsDomainUri;
        private readonly Uri? _avatarsDomainUri;
        private readonly Uri? _rawDomainUri;
        private readonly Uri? _camoDomainUri;
        private readonly Uri? _codeloadDomainUri;
        private readonly Uri? _releasesDomainUri;
        private readonly Uri? _userImagesDomainUri;
        private readonly Uri? _objectsDomainUri;

        public bool IsConfigured => _isConfigured;
        public bool UseProxy => _useProxy;
        public string? Proxy => _proxy;

        public string HomeDomain => _homeDomain ?? ThrowInvalidOpearationException();
        public string BlackholeDomain => _blackholeDomain ?? ThrowInvalidOpearationException();
        public string AssetsDomain => _assetsDomain ?? ThrowInvalidOpearationException();
        public string AvatarsDomain => _avatarsDomain ?? ThrowInvalidOpearationException();
        public string RawDomain => _rawDomain ?? ThrowInvalidOpearationException();
        public string CamoDomain => _camoDomain ?? ThrowInvalidOpearationException();
        public string CodeloadDomain => _codeloadDomain ?? ThrowInvalidOpearationException();
        public string ReleasesDomain => _releasesDomain ?? ThrowInvalidOpearationException();
        public string UserImagesDomain => _userImagesDomain ?? ThrowInvalidOpearationException();
        public string ObjectsDomain => _objectsDomain ?? ThrowInvalidOpearationException();

        public string HomeDomainAuthority => _homeDomainUri?.Authority ?? ThrowInvalidOpearationException();
        public string BlackholeDomainAuthority => _blackholeDomainUri?.Authority ?? ThrowInvalidOpearationException();
        public string AssetsDomainAuthority => _assetsDomainUri?.Authority ?? ThrowInvalidOpearationException();
        public string AvatarsDomainAuthority => _avatarsDomainUri?.Authority ?? ThrowInvalidOpearationException();
        public string RawDomainAuthority => _rawDomainUri?.Authority ?? ThrowInvalidOpearationException();
        public string CamoDomainAuthority => _camoDomainUri?.Authority ?? ThrowInvalidOpearationException();
        public string CodeloadDomainAuthority => _codeloadDomainUri?.Authority ?? ThrowInvalidOpearationException();
        public string ReleasesDomainAuthority => _releasesDomainUri?.Authority ?? ThrowInvalidOpearationException();
        public string UserImagesDomainAuthority => _userImagesDomainUri?.Authority ?? ThrowInvalidOpearationException();
        public string ObjectsDomainAuthority => _objectsDomainUri?.Authority ?? ThrowInvalidOpearationException();

        public string HomeDomainHost => _homeDomainUri?.Authority ?? ThrowInvalidOpearationException();
        public string BlackholeDomainHost => _blackholeDomainUri?.Host ?? ThrowInvalidOpearationException();
        public string AssetsDomainHost => _assetsDomainUri?.Host ?? ThrowInvalidOpearationException();
        public string AvatarsDomainHost => _avatarsDomainUri?.Host ?? ThrowInvalidOpearationException();
        public string RawDomainHost => _rawDomainUri?.Host ?? ThrowInvalidOpearationException();
        public string CamoDomainHost => _camoDomainUri?.Host ?? ThrowInvalidOpearationException();
        public string CodeloadDomainHost => _codeloadDomainUri?.Host ?? ThrowInvalidOpearationException();
        public string ReleasesDomainHost => _releasesDomainUri?.Host ?? ThrowInvalidOpearationException();
        public string UserImagesDomainHost => _userImagesDomainUri?.Host ?? ThrowInvalidOpearationException();
        public string ObjectsDomainHost => _objectsDomainUri?.Host ?? ThrowInvalidOpearationException();


        public GitHubProxyConfiguration(IOptions<GitHubProxyOptions> optionAccessor, ILogger<GitHubProxyConfiguration> logger)
        {
            GitHubProxyOptions options = optionAccessor.Value;
            _useProxy = options.UseProxy;
            _proxy = options.Proxy;

            if (string.IsNullOrEmpty(options.HomeDomain) || !Uri.TryCreate(options.HomeDomain, UriKind.Absolute, out Uri? homeDomainUri))
            {
                logger.LogError("HomeDomain is incorrectly configured.");
                return;
            }
            _homeDomain = options.HomeDomain;
            _homeDomainUri = homeDomainUri;

            if (string.IsNullOrEmpty(options.BlackholeDomain) || !Uri.TryCreate(options.BlackholeDomain, UriKind.Absolute, out Uri? blackholeDomainUri))
            {
                logger.LogError("BlackholeDomain is incorrectly configured.");
                return;
            }
            _blackholeDomain = options.BlackholeDomain;
            _blackholeDomainUri = blackholeDomainUri;

            if (string.IsNullOrEmpty(options.AssetsDomain) || !Uri.TryCreate(options.AssetsDomain, UriKind.Absolute, out Uri? assetsDomainUri))
            {
                logger.LogError("AssetsDomain is incorrectly configured.");
                return;
            }
            _assetsDomain = options.AssetsDomain;
            _assetsDomainUri = assetsDomainUri;

            if (string.IsNullOrEmpty(options.AvatarsDomain) || !Uri.TryCreate(options.AvatarsDomain, UriKind.Absolute, out Uri? avatarsDomainUri))
            {
                logger.LogError("AvatarsDomain is incorrectly configured.");
                return;
            }
            _avatarsDomain = options.AvatarsDomain;
            _avatarsDomainUri = avatarsDomainUri;

            if (string.IsNullOrEmpty(options.RawDomain) || !Uri.TryCreate(options.RawDomain, UriKind.Absolute, out Uri? rawDomainUri))
            {
                logger.LogError("RawDomain is incorrectly configured.");
                return;
            }
            _rawDomain = options.RawDomain;
            _rawDomainUri = rawDomainUri;

            if (string.IsNullOrEmpty(options.CamoDomain) || !Uri.TryCreate(options.CamoDomain, UriKind.Absolute, out Uri? camoDomainUri))
            {
                logger.LogError("CamoDomain is incorrectly configured.");
                return;
            }
            _camoDomain = options.CamoDomain;
            _camoDomainUri = camoDomainUri;

            if (string.IsNullOrEmpty(options.CodeloadDomain) || !Uri.TryCreate(options.CodeloadDomain, UriKind.Absolute, out Uri? codeloadDomainUri))
            {
                logger.LogError("CodeloadDomain is incorrectly configured.");
                return;
            }
            _codeloadDomain = options.CodeloadDomain;
            _codeloadDomainUri = codeloadDomainUri;

            if (string.IsNullOrEmpty(options.ReleasesDomain) || !Uri.TryCreate(options.ReleasesDomain, UriKind.Absolute, out Uri? releasesDomainUri))
            {
                logger.LogError("ReleasesDomain is incorrectly configured.");
                return;
            }
            _releasesDomain = options.ReleasesDomain;
            _releasesDomainUri = releasesDomainUri;

            if (string.IsNullOrEmpty(options.UserImagesDomain) || !Uri.TryCreate(options.UserImagesDomain, UriKind.Absolute, out Uri? userImagesDomainUri))
            {
                logger.LogError("UserImagesDomain is incorrectly configured.");
                return;
            }
            _userImagesDomain = options.UserImagesDomain;
            _userImagesDomainUri = userImagesDomainUri;

            if (string.IsNullOrEmpty(options.ObjectsDomain) || !Uri.TryCreate(options.ObjectsDomain, UriKind.Absolute, out Uri? objectsDomainUri))
            {
                logger.LogError("ObjectsDomain is incorrectly configured.");
                return;
            }
            _objectsDomain = options.ObjectsDomain;
            _objectsDomainUri = objectsDomainUri;

            _isConfigured = true;
        }


        private static string ThrowInvalidOpearationException() => throw new InvalidOperationException();
    }
}

namespace Equinox.Infra.CrossCutting.Identity.API
{
    public class AppJwtSettings
    {
        public string SecretKey { get; set; }
        public int Expiration { get; set; } = 1; // Access token expiration in hours
        public int RefreshTokenExpirationDays { get; set; } = 7; // Refresh token expiration in days
        public string Issuer { get; set; } = "Equinox.Api";
        public string Audience { get; set; } = "Api";
        public int MaxActiveRefreshTokensPerUser { get; set; } = 5; // Limit concurrent sessions
    }
}
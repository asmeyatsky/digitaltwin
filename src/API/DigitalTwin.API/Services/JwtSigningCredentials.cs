using Microsoft.IdentityModel.Tokens;

namespace DigitalTwin.API.Services
{
    public class JwtSigningCredentials
    {
        public SigningCredentials Credentials { get; }
        public string Issuer { get; }
        public string Audience { get; }

        public JwtSigningCredentials(SigningCredentials credentials, string issuer, string audience)
        {
            Credentials = credentials;
            Issuer = issuer;
            Audience = audience;
        }
    }
}

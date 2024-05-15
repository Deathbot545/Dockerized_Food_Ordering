using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Food_Ordering_Web.Middleware
{
    public class JwtTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtTokenMiddleware> _logger;

        public JwtTokenMiddleware(RequestDelegate next, ILogger<JwtTokenMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Check if the user is authenticated
                if (context.User.Identity.IsAuthenticated)
                {
                    var token = context.Request.Cookies["jwtCookie"];

                    if (string.IsNullOrEmpty(token))
                    {
                        throw new ArgumentNullException("JWT token is missing.");
                    }

                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);

                    var userIdClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sub);
                    if (userIdClaim == null)
                    {
                        throw new ArgumentNullException("User ID claim is missing in the JWT.");
                    }
                }

                await _next(context);
            }
            catch (ArgumentNullException argEx)
            {
                _logger.LogError(argEx, "A required claim was missing in the JWT. User needs to log in again.");
                context.Response.Redirect("/Account/Login?error=Your session has expired. Please log in again.");
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogError(ex, "JWT token has expired.");
                context.Response.Redirect("/Account/Login?error=Your session has expired. Please log in again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the token validation process.");
                context.Response.Redirect("/Account/Login?error=An error occurred. Please log in again.");
            }
        }
    }
}

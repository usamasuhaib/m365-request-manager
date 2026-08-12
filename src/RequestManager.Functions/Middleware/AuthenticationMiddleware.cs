using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace RequestManager.Functions.Middleware
{
    public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<AuthenticationMiddleware> _logger;
        private readonly string? _tenantId;
        private readonly string? _clientId;
        private ConfigurationManager<OpenIdConnectConfiguration>? _configManager;

        public AuthenticationMiddleware(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AuthenticationMiddleware>();
            _tenantId = Environment.GetEnvironmentVariable("MicrosoftTenantId");
            _clientId = Environment.GetEnvironmentVariable("MicrosoftClientId");

            if (!string.IsNullOrEmpty(_tenantId))
            {
                var stsDiscoveryEndpoint = $"https://login.microsoftonline.com/{_tenantId}/v2.0/.well-known/openid-configuration";
                _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());
            }
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var requestData = await context.GetHttpRequestDataAsync();
            if (requestData == null)
            {
                await next(context);
                return;
            }

            // Exclude public endpoints
            var path = requestData.Url.AbsolutePath.ToLower();
            if (path.EndsWith("/health") || path.EndsWith("/setup"))
            {
                await next(context);
                return;
            }

            if (!requestData.Headers.TryGetValues("Authorization", out var authHeaders))
            {
                _logger.LogWarning("Missing Authorization header.");
                SetUnauthorizedResponse(requestData, context);
                return;
            }

            var authHeader = authHeaders.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid Authorization header format.");
                SetUnauthorizedResponse(requestData, context);
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            try
            {
                ClaimsPrincipal? principal = null;

                if (_configManager != null && !string.IsNullOrEmpty(_clientId))
                {
                    // Full Entra ID signature & claims validation
                    var config = await _configManager.GetConfigurationAsync();
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidAudience = $"api://{_clientId}",
                        ValidateIssuer = true,
                        ValidIssuer = $"https://login.microsoftonline.com/{_tenantId}/v2.0",
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKeys = config.SigningKeys,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };

                    var tokenHandler = new JwtSecurityTokenHandler();
                    principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                }
                else
                {
                    // Fallback: Parse token claims directly for local development without active Entra ID
                    _logger.LogWarning("Entra ID configuration missing. Falling back to claims parsing (local testing mode).");
                    var tokenHandler = new JwtSecurityTokenHandler();
                    if (tokenHandler.CanReadToken(token))
                    {
                        var jwtToken = tokenHandler.ReadJwtToken(token);
                        var identity = new ClaimsIdentity(jwtToken.Claims, "Bearer");
                        principal = new ClaimsPrincipal(identity);
                    }
                }

                if (principal != null)
                {
                    // Extract email and name claims
                    var email = principal.FindFirst(ClaimTypes.Upn)?.Value 
                                ?? principal.FindFirst(ClaimTypes.Email)?.Value 
                                ?? principal.FindFirst("preferred_username")?.Value 
                                ?? "test.user@solvefy.onmicrosoft.com";

                    var name = principal.FindFirst("name")?.Value ?? "Test User";

                    context.Items["UserEmail"] = email;
                    context.Items["UserName"] = name;
                    context.Items["UserPrincipal"] = principal;

                    await next(context);
                }
                else
                {
                    _logger.LogWarning("Token validation failed.");
                    SetUnauthorizedResponse(requestData, context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation exception.");
                SetUnauthorizedResponse(requestData, context);
            }
        }

        private void SetUnauthorizedResponse(Microsoft.Azure.Functions.Worker.Http.HttpRequestData request, FunctionContext context)
        {
            var response = request.CreateResponse(HttpStatusCode.Unauthorized);
            response.StatusCode = HttpStatusCode.Unauthorized;
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString("{\"success\": false, \"message\": \"Unauthorized. Invalid or missing token.\"}");
            context.GetInvocationResult().Value = response;
        }
    }
}

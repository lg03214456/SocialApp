namespace SocialApp.Api.Auth;

public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);
public record AuthResponse(string AccessToken, object User);

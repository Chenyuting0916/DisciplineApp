using DisciplineApp.Models;

namespace DisciplineApp.Services.Interfaces;

public interface IChallengeService
{
    /// <summary>
    /// Creates a new challenge and returns the share token
    /// </summary>
    Task<string> CreateChallengeAsync(string userId, string userName, string challengeType = "7-day-focus");
    
    /// <summary>
    /// Retrieves challenge details by share token
    /// </summary>
    Task<Challenge?> GetChallengeByTokenAsync(string token);
    
    /// <summary>
    /// Marks a challenge as accepted and creates tasks for the acceptor
    /// </summary>
    Task<bool> AcceptChallengeAsync(string token, string? userId = null);
    
    /// <summary>
    /// Gets all challenges created by a user
    /// </summary>
    Task<List<Challenge>> GetUserChallengesAsync(string userId);
    
    /// <summary>
    /// Checks if a challenge is completed
    /// </summary>
    Task<bool> CheckChallengeCompletionAsync(int challengeId);
}

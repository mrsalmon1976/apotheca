using Apotheca.Data;

namespace Apotheca.Api.Features.Auth.Login
{
    public class LoginRepository
    {

        public virtual async Task<string> CreateUserAsync(IDbContext db, User user)
        {
            string userId = Guid.NewGuid().ToString();
            await db.ExecuteAsync(
                "INSERT INTO users (id, email, display_name, photo_url) VALUES (@Id, @Email, @DisplayName, @PhotoUrl)",
                new { Id = userId, Email = user.Email, DisplayName = user.DisplayName, PhotoUrl = user.PhotoUrl });

            return userId;
        }

        public virtual async Task CreateUserIdentityAsync(IDbContext db, User user, string userId)
        {
            await db.ExecuteAsync(
                "INSERT INTO user_firebase_identities (firebase_uid, user_id, provider_id) VALUES (@Uid, @UserId, @ProviderId)",
                new { Uid = user.Uid, UserId = userId, ProviderId = user.ProviderId });
        }


        public virtual async Task<string?> GetUserIdByEmailAsync(IDbContext db, string email)
        {
            var userId = await db.QueryFirstOrDefaultAsync<string>(
                "SELECT id FROM users WHERE email = @Email",
                new { Email = email });

            return userId;
        }


        public virtual async Task<bool> UserFirebaseIdentityExistsAsync(IDbContext db, string uid)
        {
            string? userId = await db.QueryFirstOrDefaultAsync<string>(
                "SELECT user_id FROM user_firebase_identities WHERE firebase_uid = @Uid",
                new { Uid = uid });

            return (userId != null);
        }
    }
}

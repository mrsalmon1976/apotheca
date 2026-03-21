using FirebaseAdmin.Auth;
using System.Threading;

namespace Apotheca.Api.Features.Auth.Login
{
    public class FirebaseService
    {
        public virtual async Task<User> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        {
            FirebaseToken token;
            try
            {
                token = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken, cancellationToken);
            }
            catch (FirebaseAuthException ex)
            {
                throw new UnauthorizedAccessException("Invalid or expired token.", ex);
            }

            UserRecord userRecord;
            try
            {
                userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(token.Uid, cancellationToken);
            }
            catch (FirebaseAuthException ex)
            {
                throw new UnauthorizedAccessException("Failed to retrieve user details.", ex);
            }

            return userRecord.ToUserModel();

        }
    }
}

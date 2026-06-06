using Microsoft.AspNetCore.Identity;
using ProductionApi.Models;

namespace ProductionApi.Services
{
    public class PasswordService
    {
        private readonly PasswordHasher<Person> _hasher = new();

        public string HashPassword(Person person, string password) =>
            _hasher.HashPassword(person, password);

        public bool VerifyPassword(Person person, string password, string? hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            var result = _hasher.VerifyHashedPassword(person, hash, password);
            return result == PasswordVerificationResult.Success
                || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}

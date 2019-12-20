using System;
using System.Threading.Tasks;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class AuthRepository : IAuthRepository
    {
        public DataContext _context { get; set; }
        public AuthRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<User> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x=> x.UserName == username);
            if(user == null)
                return null;
            
            if(!VerifyPasswordHash(password,user.PasswordHash, user.PasswordSalt))
                return null;

            return user;
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using(var ctx = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var hashed = ctx.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                if(passwordHash.Length != hashed.Length)
                    return false;

                for(int i=0;i<passwordHash.Length;i++){
                    if(passwordHash[i] != hashed[i])
                        return false;
                }
            }
            return true;
        }

        public async Task<User> Register(User user, string password)
        {
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using(var ctx = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = ctx.Key;
                passwordHash = ctx.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        public async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(x=> x.UserName == username);
        }
    }
}
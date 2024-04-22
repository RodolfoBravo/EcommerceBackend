using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceBackend.Context;
using EcommerceBackend.Models;
using EcommerceBackend.Utils;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace EcommerceBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }


        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.id)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: Register user
        [HttpPost("RegisterUser")]
        public async Task<ActionResult<User>> RegisterUser(User user)
        {
            // Comprobar si el correo electrónico ya existe en la base de datos
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.email == user.email);
            if (existingUser != null)
            {
                return BadRequest("El correo electrónico ya está registrado");
            }

            user.password = HashFunctions.HashPassword(user.password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generar token con duración de una hora
            var tokenString = TokenFunctions.GenerateToken(user.id);

            // Devolver token en la respuesta
            return Ok(new {
                success = true,
                Token = tokenString,
                User = new
                {
                    Id = user.id,
                    userName = user.userName,
                    email = user.email,
                    firstName = user.firstName,
                    lastName = user.lastName,
                    birthdate = user.birthdate,
                    phone = user.phone,
                    role = user.role
                }
            });
        }

        //POST Loggin user
        [HttpPost("LoginUser")]
        public async Task<ActionResult> LoginUser(LoginUser loginModel)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.email == loginModel.email);
            if (user == null)
            {
                return BadRequest("Correo no registrado");
            }
            if (user == null || !HashFunctions.VerifyPassword(loginModel.password, user.password))
            {
               return BadRequest("Contraseña incorrectos");
            }

            // Generar token con duración de una hora
            var tokenString = TokenFunctions.GenerateToken(user.id);

            // Devolver token y la información del usuario en la respuesta
            return Ok(new {
                success = true,
                Token = tokenString,
                User = new
                {
                    Id = user.id,
                    userName = user.userName,
                    email = user.email,
                    firstName = user.firstName,
                    lastName = user.lastName,
                    birthdate = user.birthdate,
                    phone = user.phone,
                    role = user.role
                }
            });
        }

        [HttpGet("GetUser/{token}")]
        public async Task<ActionResult<User>> GetUser(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token no válido");
            }
            int userId;
            var isValidToken = TokenFunctions.ValidateToken(token, out userId);
            if (isValidToken)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("Usuario no encontrado");
                }
                user.password = "";
                return Ok(user);
            }

            return Unauthorized();
        }
        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.id == id);
        }
    }
}

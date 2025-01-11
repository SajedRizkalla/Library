using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyLibrary.Data;
using System.Linq;
using System.Collections.Generic;

namespace MyLibrary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private readonly DBContext _context;

        // Constructor to inject DBContext
        public APIController(DBContext context)
        {
            _context = context;
        }

        // GET: api/API
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            try
            {
                var users = _context.Users.ToList();
                if (users == null || !users.Any())
                {
                    return NotFound(new { Message = "No users found." });
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"Error retrieving data: {ex.Message}" });
            }
        }

        // GET: api/API/{id}
        [HttpGet("{id}")]
        public IActionResult GetUserById(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { Message = "User ID cannot be empty." });
                }

                var user = _context.Users.FirstOrDefault(u => u.Username == id);
                if (user == null)
                {
                    return NotFound(new { Message = $"User with ID {id} not found." });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"Error retrieving data: {ex.Message}" });
            }
        }

        // POST: api/API
        [HttpPost]
        public IActionResult AddUser([FromBody] User user)
        {
            if (user == null)
            {
                return BadRequest(new { Message = "User data is invalid." });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Email))
                {
                    return BadRequest(new { Message = "Username and Email are required." });
                }

                if (_context.Users.Any(u => u.Username == user.Username))
                {
                    return Conflict(new { Message = $"A user with the username '{user.Username}' already exists." });
                }

                _context.Users.Add(user);
                _context.SaveChanges();

                return CreatedAtAction(nameof(GetUserById), new { id = user.Username }, user);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"Error saving data: {ex.Message}" });
            }
        }

        // PUT: api/API/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateUser(string id, [FromBody] User updatedUser)
        {
            if (updatedUser == null)
            {
                return BadRequest(new { Message = "Updated user data is invalid." });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { Message = "User ID cannot be empty." });
                }

                var user = _context.Users.FirstOrDefault(u => u.Username == id);
                if (user == null)
                {
                    return NotFound(new { Message = $"User with ID {id} not found." });
                }

                user.Username = updatedUser.Username;
                user.Email = updatedUser.Email;
                user.Password = updatedUser.Password;
                user.Gender = updatedUser.Gender;

                _context.SaveChanges();
                return Ok(new { Message = "User updated successfully.", UpdatedUser = user });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"Error updating data: {ex.Message}" });
            }
        }

        // DELETE: api/API/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { Message = "User ID cannot be empty." });
                }

                var user = _context.Users.FirstOrDefault(u => u.Username == id);
                if (user == null)
                {
                    return NotFound(new { Message = $"User with ID {id} not found." });
                }

                _context.Users.Remove(user);
                _context.SaveChanges();

                return Ok(new { Message = $"User with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"Error deleting data: {ex.Message}" });
            }
        }
    }
}
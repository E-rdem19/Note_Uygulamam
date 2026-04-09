using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt; // HATA DÜZELTİLDİ: 'i' harfi silindi
using System.Security.Claims;
using System.Text;

namespace PersonalVault.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login-secure")]
        public IActionResult LoginSecure([FromBody] LoginModel model)
        {
            string connString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connString))
                {
                    string query = "SELECT id, Password, FullName FROM Users WHERE Username = @p1";
                    NpgsqlCommand cmd = new NpgsqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("p1", model.Username ?? "");

                    conn.Open();
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storeHash = reader["Password"]?.ToString()?.Trim() ?? "";
                            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(model.Password, storeHash);

                            if (isPasswordCorrect)
                            {
                                var tokenHandler = new JwtSecurityTokenHandler();
                                // Bunu bul ve parantez içini şöyle değiştir:
                                var key = Encoding.UTF8.GetBytes("Bu_Cok_Gizli_Ve_En_Az_32_Karakter_123!");
                                
                            var tokenDescriptor = new SecurityTokenDescriptor
                            {
                                Subject = new ClaimsIdentity(new[]
                            {
                             new Claim("id", reader["id"].ToString()), // 'id' yerine 'userId' daha standarttır
                            new Claim(ClaimTypes.Name, reader["FullName"].ToString()) 
                            }),
                            Expires = DateTime.UtcNow.AddHours(2),
                            SigningCredentials = new SigningCredentials(
                            new SymmetricSecurityKey(key),
                            SecurityAlgorithms.HmacSha256Signature),
    
                             // Bunların appsettings.json ile BİREBİR aynı olduğundan emin ol:
                            Issuer = _configuration["Jwt:Issuer"], 
                            Audience = _configuration["Jwt:Audience"]
                            };
                                var token = tokenHandler.CreateToken(tokenDescriptor);
                                var tokenString = tokenHandler.WriteToken(token);

                                return Ok(new { 
                                    Token = tokenString, 
                                    User = reader["FullName"], 
                                    UserId = reader["id"] 
                                });
                            }
                        }
                    }
                }
                return Unauthorized(new { Message = "Hatalı kullanıcı adı veya şifre." });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { Error = "Giriş işlemi sırasında hata: " + ex.Message });
            }
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] LoginModel model)
        {
            string connString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            using (NpgsqlConnection conn = new NpgsqlConnection(connString))
            {
                string hashedPass = BCrypt.Net.BCrypt.HashPassword(model.Password);

                string query = "INSERT INTO Users (Username, Password, FullName) VALUES (@u, @p, @f)";
                NpgsqlCommand cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("u", model.Username ?? "");
                cmd.Parameters.AddWithValue("p", hashedPass);
                cmd.Parameters.AddWithValue("f", model.Username ?? ""); 

                try 
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { Message = "Kayıt Başarılı!" }); 
                }
                catch (System.Exception ex)
                {
                    return BadRequest(new { Error = "Kayıt hatası: " + ex.Message });
                }
            }
        }
    }

    public class LoginModel 
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
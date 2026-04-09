using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Specialized; // For key-value pairs (e.g., in the form)
using System.Reflection.Metadata; // For more advanced code analysis 
using System.Net;  // This library is needed for networking and HTTP requests

namespace PersonalVault.Controllers 
{
    [Authorize] // Required for accessing protected controllers
    [ApiController] // The controller will handle API requests (e.g., GET, POST, PUT, DELETE) 
    [Route("api/[controller]")] 
    public class NoteController : ControllerBase  // Base class - handles data and logic for the notes 
    {
        private readonly IConfiguration _configuration;

        public NoteController(IConfiguration configuration) 
        {
            _configuration = configuration; // Get the configuration from your application.
        } 


        [HttpGet("{userId}")] // A route to get notes based on a user ID 
        public IActionResult GetUserNotes(int userId)  // Method to retrieve notes for a specific user
        {
            try //Try block: to catch and handle potential errors
            {
                var loggedInUserIdClaim = User.FindFirst("id")?.Value; // Check if the user is logged in, access their ID 

                if (string.IsNullOrEmpty(loggedInUserIdClaim)) 
                {
                    // Return Unauthorized status if no valid user information found.
                    return Unauthorized(new { Message = "Kullanıcı bilgisi alınamadı." });  
                }

                // 2. Güvenlik Kontrolü
                if (loggedInUserIdClaim != userId.ToString()) 
                { 
                    // If the provided user ID doesn't match, return Forbid response to deny access. 
                    return Forbid(); 
                }

                // Database connection details
                string connString = _configuration.GetConnectionString("DefaultConnection") ?? ""; // Get database connection string from config  
                var notesList = new List<object>();


                using (NpgsqlConnection conn = new NpgsqlConnection(connString)) 
                {

                    string query = "SELECT title, content, createdat, id FROM Notes WHERE userid = @uid ORDER BY createdat DESC"; // SQL query for fetching notes
                    NpgsqlCommand cmd = new NpgsqlCommand(query, conn); 
                    cmd.Parameters.AddWithValue("uid", userId); // Parameterized queries to make the code more secure

                    conn.Open(); // Open the database connection.  
                    using (var reader = cmd.ExecuteReader()) 
                    { 
                        while (reader.Read())
                        {
                            notesList.Add(new
                            { 
                                Title = reader["title"]?.ToString(), 
                                Content = reader["content"]?.ToString(),
                                Createdat = reader["createdat"],
                                Id=reader["id"] // Get the values and add them to your list 
                            }); 
                        }
                    }  // The loop is completed after reading all data from the database
                } 

                return Ok(notesList); // If a successful response, send back the notes.
            } 
            catch (System.Exception ex)  // Error handling block 
            {
               Console.WriteLine("HATA: " + ex.Message); // For debugging, write to the console if there's an error in retrieving data. 
               return StatusCode(500, new { Message = "Sunucu hatası: " + ex.Message });  // Return an HTTP 500 status code (Internal Server Error) with a message explaining that something went wrong
            }  
        } 


        [HttpPost] // A route to create a note - POST request.   
        public IActionResult CreateNote([FromBody] NoteCreateModel model) // From the API body, a 'model' of data for creating notes 
        { 
            try
            {

                var loggedInUserId = User.FindFirst("id")?.Value;  // Check if the user is logged in and get their ID 
                if (string.IsNullOrEmpty(loggedInUserId)) // If the user isn't logged in, return an Unauthorized response
                    return Unauthorized(); 


                string connString = _configuration.GetConnectionString("DefaultConnection") ?? "";  // Get connection string from configuration  
                using (NpgsqlConnection conn = new NpgsqlConnection(connString)) 
                { 

                    string query = "INSERT INTO Notes (userid, title, content) VALUES (@p1, @p2, @p3)"; 
                    NpgsqlCommand cmd = new NpgsqlCommand(query, conn); 
                    cmd.Parameters.AddWithValue("p1", int.Parse(loggedInUserId)); // Add the logged-in user ID as a parameter
                    cmd.Parameters.AddWithValue("p2", model.Title); 
                    cmd.Parameters.AddWithValue("p3", model.Content);  

                    conn.Open(); 
                    int rowsAffected = cmd.ExecuteNonQuery(); 
                    if (rowsAffected == 0) // If the number of affected rows is zero, return a NotFound (404) error to indicate that the note wasn't found
                    {
                        return NotFound(new { Message = "Not Bulunamadı veya yetkiniz yok." });  
                    } 
                } 

                return Ok(new { Message = "Note Başarıyla kaydedildi. " }); 
            } 
            catch (System.Exception ex)  // Error handling block 
            { 
                 Console.WriteLine("ERROR: " + ex.Message); // For debugging, write to the console if there's an error in creating the note 
                return StatusCode(500, new { Error = ex.Message }); // Return a 500 (Internal Server Error) status code with the message describing the issue 
            }  
        }  

        [HttpDelete("{id}")]  // A route to delete notes - DELETE request.   
        public IActionResult DeleteNote(int id) 
        {
            try
            { 
                var loggedInUserId = User.FindFirst("id")?.Value; // Check if the user is logged in and get their ID
                if (string.IsNullOrEmpty(loggedInUserId)) 
                    return Unauthorized();

                // Database connection details
                string connString = _configuration.GetConnectionString("DefaultConnection") ?? ""; // Get database connection string from config  
                using (NpgsqlConnection conn = new NpgsqlConnection(connString)) 
                {

                    string query = "DELETE FROM Notes WHERE id=@p1 AND userid=@p2"; 
                    NpgsqlCommand cmd = new NpgsqlCommand(query, conn); 
                    cmd.Parameters.AddWithValue("p1", id); 
                    cmd.Parameters.AddWithValue("p2", int.Parse(loggedInUserId));  

                    conn.Open(); 
                    int rowsAffected = cmd.ExecuteNonQuery(); // Execute the SQL query, count how many rows were deleted 
                    if (rowsAffected == 0) 
                    { // If no rows were affected, return a Not Found error (404). 
                        return NotFound(new { Message = "Not Bulunamadı veya yetkiniz yok." }); 
                    } 
                }

                return Ok(new { Message = "Note Başarıyla Silindi. " }); // Returns a successful response if the note was deleted, 
            } 
            catch (System.Exception ex)  // Error handling block 
            {
                Console.WriteLine("ERROR: " + ex.Message);  
                return StatusCode(500, new { Error = ex.Message }); // Return a 500 (Internal Server Error) status code with the message describing the issue 
            } 

        }

        [HttpPut("{id}")]  // Update a note - PUT request   
        public IActionResult UpdateNote(int id,[FromBody] NoteCreateModel model)  
        { 
                try 
                { 
                    var loggedInUserId = User.FindFirst("id")?.Value; 
                    if (string.IsNullOrEmpty(loggedInUserId)) // If the user isn't logged in, return an Unauthorized response
                        return Unauthorized();

                    // Database connection details 
                    string connString = _configuration.GetConnectionString("DefaultConnection") ?? "";  
                    using (NpgsqlConnection conn = new NpgsqlConnection(connString)) 
                    { 
                        string query = "UPDATE Notes SET title=@p1, content=@p2 WHERE id=@p3"; // SQL query for updating the notes 
                        NpgsqlCommand cmd = new NpgsqlCommand(query, conn);  
                        cmd.Parameters.AddWithValue("p1", model.Title); 
                        cmd.Parameters.AddWithValue("p2", model.Content); 
                        cmd.Parameters.AddWithValue("p3", id); // Add the ID to the query as parameter

                        conn.Open(); 
                        int rowsAffected = cmd.ExecuteNonQuery(); 
                        if (rowsAffected == 0)  
                        { 
                            return NotFound(new { Message = "Not Bulunamadı veya yetkiniz yok." }); 
                        } 

                    } 

                    return Ok(new { Message = "Note Updated successfully" }); // Returns a successful response if the note was updated,   
                } 
            catch (System.Exception ex)  // Error handling block 
            { 
                Console.WriteLine("ERROR: " + ex.Message); 
                return StatusCode(500, new { Error = ex.Message }); // Return a 500 (Internal Server Error) status code with the message describing the issue 
            }  
        }
    }
} 



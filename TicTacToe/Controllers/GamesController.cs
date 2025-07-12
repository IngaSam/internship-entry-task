using Microsoft.AspNetCore.Mvc;
using TicTacToe.Models;

namespace TicTacToe.Controllers
{
    [ApiController]
    [Route("games")]
    public class GamesController : ControllerBase
    {
        [HttpPost]
        public IActionResult CreateGame()
        {
            return Ok(new { message = "Game created!" });
        }
        [HttpGet("{id}")]
        public IActionResult GetGame(Guid id) 
        {
            return Ok(new { id = id });
        }
        [HttpPost("{id}/moves")]
        public IActionResult MakeMove(Guid id, [FromBody] MoveRequest move)
        {
            return Ok(new { move = move });
        }


    }
}

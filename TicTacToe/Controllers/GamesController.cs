using Microsoft.AspNetCore.Mvc;
using TicTacToe.Models;
using TicTacToe.Services;

namespace TicTacToe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly GameService _gameService;

        public GamesController(GameService gameService) 
        { 
            _gameService = gameService;
        }

        [HttpPost]
        public async Task <IActionResult> CreateGame()
        {
            try
            {
                var size = int.Parse(Environment.GetEnvironmentVariable("BOARD_SIZE") ?? "3");
                var game = await _gameService.CreateGame(size);
                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest(Problem(
                    title: "Filed to create game",
                    detail: ex.Message,
                    statusCode: 400));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGame(Guid id)
        {
            try
            {
                var game = await _gameService.GetGameAsync(id);
                return game != null ? Ok(game) : NotFound();
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "Failed to get game",
                    detail: ex.Message,
                    statusCode: 500);

            }
           
        }

        [HttpPost("{id}/moves")]
        public async Task<IActionResult> MakeMove(Guid id, [FromBody] MoveRequest move)
        {
            try
            {
                if(move == null)
        {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Invalid request",
                        Detail = "Move request cannot be null",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var game = await  _gameService.MakeMove(id, move);
                return Ok(game);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Game not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already finished"))
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Game finished",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid move",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
    }
}

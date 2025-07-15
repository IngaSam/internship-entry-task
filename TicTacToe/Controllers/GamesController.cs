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
                var game = await  _gameService.MakeMove(id, move);
                return Ok(game);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(Problem(
                    title: "Game not found",
                    detail: ex.Message,
                    statusCode: 404));
                
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(Problem(
                    title:"Invalid move",
                    detail:ex.Message,
                    statusCode: 400));
            }
            catch (Exception ex)
            {
                return Problem(
                    title:"File to make move",
                    detail: ex.Message,
                    statusCode: 500);
            }
        }


    }
}

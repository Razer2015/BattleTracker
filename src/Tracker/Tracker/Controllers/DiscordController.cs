using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tracker.Services;
using Tracker.ViewModels;

namespace Tracker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DiscordController : ControllerBase
    {
        private readonly ILogger<DiscordController> _logger;
        private readonly IDiscordService _discordService;

        public DiscordController(ILogger<DiscordController> logger, IDiscordService discordService)
        {
            _logger = logger;
            _discordService = discordService;
        }

        [HttpPost("searchSoldier/")]
        public IActionResult SearchSoldier([FromBody] SearchSoldierInput model)
        {
            _logger.LogInformation($"{model.ApplicationId} - {model.InteractionToken} - {model.SoldierName}");

            _discordService.SearchSoldier(model);

            return Ok();
        }

        [HttpPost("addTracker/")]
        public IActionResult AddTracker([FromBody] AddTrackerInput model)
        {
            _logger.LogInformation($"{model.ApplicationId} - {model.InteractionToken} - {model.SoldierName}");

            _discordService.AddPlayerTracker(model);

            return Ok();
        }

        [HttpPost("getTrackers/")]
        public IActionResult GetTrackers([FromBody] InteractionInput model)
        {
            _logger.LogInformation($"{model.ApplicationId} - {model.InteractionToken}");

            _discordService.ListPlayerTrackers(model);

            return Ok();
        }

        [HttpDelete("removeTracker/")]
        public IActionResult RemoveTracker([FromBody] RemoveTrackerInput model)
        {
            _logger.LogInformation($"{model.ApplicationId} - {model.InteractionToken} - {model.Identifier}");

            _discordService.RemovePlayerTracker(model);

            return Ok();
        }
    }
}

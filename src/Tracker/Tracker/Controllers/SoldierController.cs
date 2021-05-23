using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tracker.Services;
using Tracker.ViewModels;

namespace Tracker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SoldierController : ControllerBase
    {
        private readonly ILogger<SoldierController> _logger;
        private readonly IDiscordService _discordService;

        public SoldierController(ILogger<SoldierController> logger, IDiscordService discordService)
        {
            _logger = logger;
            _discordService = discordService;
        }

        [HttpPost("onJoin/")]
        public IActionResult OnJoin([FromBody] SoldierInput model)
        {
            _logger.LogInformation($"OnJoin | {model.ServerName} - {model.ServerGuid} - {model.EAGuid} - {model.SoldierName}");

            _discordService.OnPlayerJoin(model);

            return Ok();
        }

        [HttpPost("onLeave/")]
        public IActionResult OnLeave([FromBody] SoldierInput model)
        {
            _logger.LogInformation($"OnLeave | {model.ServerName} - {model.ServerGuid} - {model.EAGuid} - {model.SoldierName}");

            _discordService.OnPlayerLeave(model);

            return Ok();
        }
    }
}

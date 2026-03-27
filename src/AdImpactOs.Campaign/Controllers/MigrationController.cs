using Microsoft.AspNetCore.Mvc;
using AdImpactOs.Campaign.Migration;

namespace AdImpactOs.Campaign.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MigrationController : ControllerBase
{
    private readonly CampaignDbMigration _migration;
    private readonly ILogger<MigrationController> _logger;

    public MigrationController(CampaignDbMigration migration, ILogger<MigrationController> logger)
    {
        _migration = migration;
        _logger = logger;
    }

    [HttpPost("run")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RunMigration()
    {
        try
        {
            await _migration.RunMigrationAsync();
            return Ok(new { message = "Campaign migration completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Campaign migration failed");
            return StatusCode(500, new { error = "Migration failed", details = ex.Message });
        }
    }

    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SeedData()
    {
        try
        {
            await _migration.SeedSampleDataAsync();
            return Ok(new { message = "Sample campaign data seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seeding failed");
            return StatusCode(500, new { error = "Seeding failed", details = ex.Message });
        }
    }

    [HttpPost("seed-impressions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SeedImpressions()
    {
        try
        {
            await _migration.SeedImpressionDataAsync();
            return Ok(new { message = "Sample impression data seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Impression seeding failed");
            return StatusCode(500, new { error = "Impression seeding failed", details = ex.Message });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using AdImpactOs.Survey.Migration;

namespace AdImpactOs.Survey.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MigrationController : ControllerBase
{
    private readonly SurveyDbMigration _migration;
    private readonly ILogger<MigrationController> _logger;

    public MigrationController(SurveyDbMigration migration, ILogger<MigrationController> logger)
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
            return Ok(new { message = "Survey migration completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Survey migration failed");
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
            return Ok(new { message = "Sample survey data seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seeding failed");
            return StatusCode(500, new { error = "Seeding failed", details = ex.Message });
        }
    }
}

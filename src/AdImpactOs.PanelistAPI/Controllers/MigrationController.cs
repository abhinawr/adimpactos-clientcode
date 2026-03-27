using Microsoft.AspNetCore.Mvc;
using AdImpactOs.PanelistAPI.Migration;

namespace AdImpactOs.PanelistAPI.Controllers;

/// <summary>
/// Controller for running database migrations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MigrationController : ControllerBase
{
    private readonly PanelistDbMigration _migration;
    private readonly ILogger<MigrationController> _logger;

    public MigrationController(PanelistDbMigration migration, ILogger<MigrationController> logger)
    {
        _migration = migration;
        _logger = logger;
    }

    /// <summary>
    /// Run database migration to create containers and indexes
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RunMigration()
    {
        try
        {
            await _migration.RunMigrationAsync();
            return Ok(new { message = "Migration completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            return StatusCode(500, new { error = "Migration failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Seed sample data for testing
    /// </summary>
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SeedData()
    {
        try
        {
            await _migration.SeedSampleDataAsync();
            return Ok(new { message = "Sample data seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seeding failed");
            return StatusCode(500, new { error = "Seeding failed", details = ex.Message });
        }
    }
}
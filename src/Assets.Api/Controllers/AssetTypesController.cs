using Assets.Domain.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Assets.Api.Controllers;

[ApiController]
[Route("api/asset-types")]
[Authorize]
public sealed class AssetTypesController : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<AssetTypeMetadata>> GetAll()
    {
        return Ok(AssetTypeRegistry.BuildMetadata());
    }
}

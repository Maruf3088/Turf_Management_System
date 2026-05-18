using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using turf_management_system.DTOs.Common;
using turf_management_system.DTOs.Turf;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Controllers
{
    [ApiController]
    [Route("api/turfs")]
    public class TurfsController : ControllerBase
    {
        private readonly ITurfService _turfService;

        public TurfsController(ITurfService turfService)
        {
            _turfService = turfService;
        }

        [HttpPost]
        [Authorize(Roles = "TurfOwner")]
        public async Task<IActionResult> CreateTurf([FromBody] CreateTurfDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _turfService.CreateTurfAsync(dto, ownerId);
            return result.Success ? CreatedAtAction(nameof(GetTurfById), new { id = result.Data!.Id }, result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "TurfOwner")]
        public async Task<IActionResult> UpdateTurf(Guid id, [FromBody] UpdateTurfDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _turfService.UpdateTurfAsync(id, dto, ownerId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "TurfOwner,Admin")]
        public async Task<IActionResult> DeleteTurf(Guid id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var result = await _turfService.DeleteTurfAsync(id, userId, role);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTurfsPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] string? city = null, [FromQuery] string? sportType = null, [FromQuery] bool? isApproved = true)
        {
            // Admins can see all, public only sees approved
            if (isApproved == false && !User.IsInRole("Admin"))
                return Forbid();

            var result = await _turfService.GetAllTurfsPagedAsync(pageNumber, pageSize, search, city, sportType, isApproved);
            return Ok(result);
        }

        [HttpGet("my-turfs")]
        [Authorize(Roles = "TurfOwner")]
        public async Task<IActionResult> GetMyTurfs()
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _turfService.GetMyTurfsAsync(ownerId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTurfById(Guid id)
        {
            var result = await _turfService.GetTurfByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveTurf(Guid id)
        {
            var result = await _turfService.ApproveTurfAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPatch("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectTurf(Guid id, [FromBody] string reason)
        {
            var result = await _turfService.RejectTurfAsync(id, reason);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id}/images")]
        [Authorize(Roles = "TurfOwner")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(Guid id, [FromForm] IFormFile image, [FromQuery] bool isMain = false)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { success = false, message = "No image file provided." });

            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _turfService.UploadTurfImageAsync(id, image, isMain, ownerId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("images/{imageId}")]
        [Authorize(Roles = "TurfOwner")]
        public async Task<IActionResult> DeleteImage(Guid imageId)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _turfService.DeleteTurfImageAsync(imageId, ownerId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id}/slots")]
        [Authorize(Roles = "TurfOwner")]
        public async Task<IActionResult> AddSlot(Guid id, [FromBody] CreateTurfSlotDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _turfService.AddSlotAsync(id, dto, ownerId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("slots/{slotId}")]
        [Authorize(Roles = "TurfOwner")]
        public async Task<IActionResult> UpdateSlot(Guid slotId, [FromBody] UpdateTurfSlotDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _turfService.UpdateSlotAsync(slotId, dto, ownerId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("slots/{slotId}")]
        [Authorize(Roles = "TurfOwner")]
        public async Task<IActionResult> DeleteSlot(Guid slotId)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _turfService.DeleteSlotAsync(slotId, ownerId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id}/booking-config")]
        [Authorize(Roles = "TurfOwner,TurfManager")]
        public async Task<IActionResult> UpdateBookingConfig(Guid id, [FromBody] UpdateBookingConfigDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _turfService.UpdateBookingConfigAsync(id, dto, ownerId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id}/publish")]
        [Authorize(Roles = "TurfOwner,TurfManager")]
        public async Task<IActionResult> PublishTurf(Guid id)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _turfService.PublishTurfAsync(id, ownerId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}

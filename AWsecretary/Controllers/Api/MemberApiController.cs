using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AWsecretary.Services;
using AWsecretary.Models;
using Microsoft.AspNetCore.Http;

namespace AWsecretary.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberApiController : ControllerBase
    {
        private readonly IMemberService _service;

        public MemberApiController(IMemberService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var all = await _service.GetAllAsync();
            return Ok(all);
        }

        [HttpGet("{nid:int}")]
        public async Task<IActionResult> Get(int nid)
        {
            var m = await _service.GetByIdAsync(nid);
            if (m == null) return NotFound();
            return Ok(m);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Member m)
        {
            await _service.CreateAsync(m);
            return CreatedAtAction(nameof(Get), new { nid = m.Nid }, m);
        }

        [HttpPut("{nid:int}")]
        public async Task<IActionResult> Update(int nid, [FromBody] Member m)
        {
            var existing = await _service.GetByIdAsync(nid);
            if (existing == null) return NotFound();
            m.Nid = nid;
            await _service.UpdateAsync(m);
            return NoContent();
        }

        [HttpDelete("{nid:int}")]
        public async Task<IActionResult> Delete(int nid)
        {
            await _service.DeleteAsync(nid);
            return NoContent();
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null) return BadRequest("File required");
            await _service.ImportCsvAsync(file);
            return Ok();
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            var bytes = await _service.ExportCsvAsync();
            return File(bytes, "text/csv", "members.csv");
        }

        [HttpGet("tree")]
        public async Task<IActionResult> Tree()
        {
            var tree = await _service.GetTreeAsync();
            return Ok(tree);
        }
    }
}
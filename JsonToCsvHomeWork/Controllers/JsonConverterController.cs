using JsonToCsvHomeWork.Dtos;
using JsonToCsvHomeWork.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonToCsvHomeWork.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JsonConverterController : ControllerBase
    {
        private readonly ILogger<JsonConverterController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly JsonConverter _jsonToCsv;

        public JsonConverterController(ILogger<JsonConverterController> logger, IWebHostEnvironment hostingEnvironment, JsonConverter jsonToCsv)
        {
            _jsonToCsv = jsonToCsv;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile([FromForm] IFormCollection form)
        {
            try
            {
                var file = form.Files[0];
                _jsonToCsv.ResetWhenNewFileUploaded();
                _jsonToCsv.Convert(file);
                return Ok(_jsonToCsv.GetCsvJsonRepresentation());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("update-json/{key}/{newValue}")]
        public async Task<IActionResult> UpdateJson([FromRoute] string key, string newValue)
        {
            try
            {
                _jsonToCsv.UpdateTable(key, newValue);
                _jsonToCsv.CreateJsonFromCsv();
                return Ok(true);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("download-file")]
        public async Task<IActionResult> DownloadFile()
        {
            try
            {
                var response = _jsonToCsv.CreateJsonFromCsv();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
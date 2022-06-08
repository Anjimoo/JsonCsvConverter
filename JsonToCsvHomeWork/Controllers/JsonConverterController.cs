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
        private readonly ConverterManager _converterManager;

        public JsonConverterController(ConverterManager converterManager)
        {
            _converterManager = converterManager;
        }

        [HttpPost("upload-file")]
        public IActionResult UploadFile([FromHeader] Guid clientId, [FromForm] IFormCollection form)
        {
            try
            {
                var file = form.Files[0];
                using Stream utf8Json = file.OpenReadStream();
                _converterManager.GetOrAdd(clientId).ConvertJsonFileToCsv(utf8Json);

                var reader = _converterManager.GetOrAdd(clientId);

                IEnumerable<DataTableRowDto> dataRows = reader.ReadRows().Select(tuple => new DataTableRowDto
                {
                    Key = tuple.Key,
                    Value = tuple.Value,
                    NewValue = tuple.NewValue
                });

                return Ok(dataRows);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("update-json/{key}/{newValue}")]
        public IActionResult UpdateJson([FromHeader] Guid clientId, [FromRoute] string key, string newValue)
        {
            try
            {
                _converterManager.GetOrAdd(clientId).UpdateTable(key, newValue);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("download-file")]
        public IActionResult DownloadFile([FromHeader] Guid clientId)
        {
            try
            {
                var response = _converterManager.GetOrAdd(clientId).CreateJsonFromCsv();
                _converterManager.Remove(clientId);
                return Ok(response.ToJsonString());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("remove-file")]
        public IActionResult RemoveFile([FromHeader] Guid clientId)
        {
            try
            {
                _converterManager.Remove(clientId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
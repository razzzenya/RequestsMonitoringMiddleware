using Microsoft.AspNetCore.Mvc;

namespace RequestMonitoring.Test.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        [HttpGet(Name = "GetWeatherForecast")]
        public int Get()
        {
            return 1;
        }
    }
}

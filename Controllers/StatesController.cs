using Microsoft.AspNetCore.Mvc;

namespace microservices_dashboard_api.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class StatesController : ControllerBase {

        private readonly ILogger<StatesController> _logger;
        private readonly Dictionary<Guid, ServiceState> states;

        public StatesController(ILogger<StatesController> logger, Dictionary<Guid, ServiceState> states) {
            _logger = logger;
            this.states = states;
        }

        [HttpGet(Name = "getstates")]
        public IEnumerable<ServiceState> Get() {
            return states.Values;
        }
    }
}
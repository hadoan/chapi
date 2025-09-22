using System;

namespace Chapi.AI.Controllers
{
    public class GenerateEndpointRequest
    {
        public Guid AuthProfileId { get; set; }
        public Guid EndpointId { get; set; }
    }

}
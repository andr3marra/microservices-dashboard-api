using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http;

namespace microservices_dashboard_api {
    public class ServiceState {

        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Name { get; set; }
        public Uri Url { get; set; }
        public HealthStatus HealthStatus { get; set; }
        public string Description { get; set; }
        public List<string>? Metadata { get; set; }
    }

    public class ServiceStateRepository : IHostedService {
        private readonly StateDatabase healthCheckConfig;
        private readonly HttpClient httpClient;
        public Dictionary<Guid, ServiceState> States { get; set; }

        public ServiceStateRepository(StateDatabase healthCheckConfig, HttpClient httpClient, Dictionary<Guid, ServiceState> States) {
            this.healthCheckConfig = healthCheckConfig;
            this.httpClient = httpClient;
            this.States = States;
        }
        public async Task StartAsync(CancellationToken cancellationToken) {
            foreach (var item in healthCheckConfig.Services) {
                var serviceState = new ServiceState() {
                    Id = item.Key,
                    Name = item.Value.Name,
                    Url = item.Value.Url
                };

                States.Add(serviceState.Id, serviceState);
            }
            _ = DoWork();
        }



        public async Task DoWork(CancellationToken cancellationToken = default) {
            var bla = new PeriodicTimer(TimeSpan.FromSeconds(30));
            do {

                foreach (var service in healthCheckConfig.Services) {

                    var httpClient1 = new HttpClient();
                    var response = await httpClient1.GetFromJsonAsync<HealthCheckResponse>(service.Value.Url, cancellationToken);

                    if (response == null) { continue; }

                    foreach (var alias in service.Value.Aliases) {
                        if (response.Results.TryGetValue(alias.Key, out var result)) {
                            response.Results.Remove(alias.Key);
                            response.Results.Add(alias.Value, result);
                        }
                    }

                    Update(service.Key, response);
                }

                foreach (var service in healthCheckConfig.Services) {
                    if (service.Value.Aliases?.Any() != true) { continue; }

                }

            } while (await bla.WaitForNextTickAsync(cancellationToken));
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public void AddData(StateDatabase healthCheckConfig) {
            foreach (var serviceConfig in healthCheckConfig.Services) {
                States.Add(serviceConfig.Key, new ServiceState() {
                    Id = serviceConfig.Key,
                    Name = serviceConfig.Value.Name,
                    Url = serviceConfig.Value.Url
                });
            }
        }


        internal void Update(Guid parentId, HealthCheckResponse response) {

            if (!States.TryGetValue(parentId, out var state)) {
                state = new ServiceState() { Id = parentId };
                States[parentId] = state;
            }

            state.HealthStatus = response.Status;


            var childToBeUpdated = States.Values.Where(x => x.ParentId == parentId);

            var newChilds = response.Results.Where(x => !childToBeUpdated.Any(y => y.Name == x.Key));

            var childThatHaveReturned = childToBeUpdated.Where(x => response.Results.ContainsKey(x.Name));

            var childThatHaveNotReturned = childToBeUpdated.Except(childThatHaveReturned);



            // UPDATES EXISTING VALUES
            // NOT FOUND CHILDS
            foreach (var item in childThatHaveNotReturned) {
                item.HealthStatus = HealthStatus.Unhealthy;
            }
            // FOUND CHILDS
            foreach (var item in childThatHaveReturned) {
                if (!response.Results.TryGetValue(item.Name, out var result)) {
                    // LOG UNEXPECTED
                    continue;
                }
                item.HealthStatus = result.Status;
                item.Description = result.Description;
                item.Metadata = result.Data;
            }

            foreach (var item in newChilds) {

                var serviceState = new ServiceState();
                serviceState.Id = Guid.NewGuid();
                serviceState.ParentId = parentId;
                serviceState.Name = item.Key;
                serviceState.HealthStatus = item.Value.Status;
                serviceState.Description = item.Value.Description;
                serviceState.Metadata = item.Value.Data;

                States.Add(serviceState.Id, serviceState);
            }
        }
    }
}


public class Result {
    public HealthStatus Status { get; set; }
    public string Description { get; set; }
    public List<string> Data { get; set; }
}

public class HealthCheckResponse {
    public HealthStatus Status { get; set; }
    public Dictionary<string, Result> Results { get; set; }
}


using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace microservices_dashboard_api {
    public class ServiceState {

        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Name { get; set; }
        public Uri Url { get; set; }
        public HealthStatus HealthStatus { get; set; }
        public string Description { get; set; }
        public IEnumerable<string> Tags { get; set; }
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
                try {
                    foreach (var service in healthCheckConfig.Services) {

                        var httpClient1 = new HttpClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(10);

                        var response1 = await httpClient1.GetAsync(service.Value.Url, cancellationToken);

                        var responseString = await response1.Content.ReadAsStringAsync();

                        var converter = new JsonStringEnumConverter();

                        var options = new JsonSerializerOptions {
                            PropertyNameCaseInsensitive = true
                        };
                        options.Converters.Add(converter);

                        var response = JsonSerializer.Deserialize<MyHealthReport>(responseString, options);

                        if (response == null) { continue; }

                        if (service.Value.Aliases != null) {
                            foreach (var alias in service.Value.Aliases) {
                                if (response.Entries.TryGetValue(alias.Key, out var result)) {
                                    //response.Results.Remove(alias.Key);
                                    //response.Results.Add(alias.Value, result);
                                }
                            }
                        }

                        Update(service.Key, response);
                    }

                    foreach (var service in healthCheckConfig.Services) {
                        if (service.Value.Aliases?.Any() != true) { continue; }

                    }
                }catch(Exception ex) {

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


        internal void Update(Guid parentId, MyHealthReport response) {

            if (!States.TryGetValue(parentId, out var state)) {
                state = new ServiceState() { Id = parentId };
                States[parentId] = state;
            }

            state.HealthStatus = response.Status;


            var childToBeUpdated = States.Values.Where(x => x.ParentId == parentId);

            var newChilds = response.Entries.Where(x => !childToBeUpdated.Any(y => y.Name == x.Key));

            var childThatHaveReturned = childToBeUpdated.Where(x => response.Entries.ContainsKey(x.Name));

            var childThatHaveNotReturned = childToBeUpdated.Except(childThatHaveReturned);



            // UPDATES EXISTING VALUES
            // NOT FOUND CHILDS
            foreach (var item in childThatHaveNotReturned) {
                item.HealthStatus = HealthStatus.Unhealthy;
            }
            // FOUND CHILDS
            foreach (var item in childThatHaveReturned) {
                if (!response.Entries.TryGetValue(item.Name, out var result)) {
                    // LOG UNEXPECTED
                    continue;
                }
                item.HealthStatus = result.Status;
                item.Description = result.Description;
                item.Tags = result.Tags;
                //item.Metadata = result.Data;
            }

            foreach (var item in newChilds) {

                var serviceState = new ServiceState();
                serviceState.Id = Guid.NewGuid();
                serviceState.ParentId = parentId;
                serviceState.Name = item.Key;
                serviceState.HealthStatus = item.Value.Status;
                serviceState.Description = item.Value.Description;
                serviceState.Tags = item.Value.Tags;

                States.Add(serviceState.Id, serviceState);
            }
        }
    }
}


public class MyHealthReport {
    public MyHealthReport() {

    }

    public IReadOnlyDictionary<string, MyHealthReportEntry> Entries { get; set; }
    public HealthStatus Status { get; set; }
    public TimeSpan TotalDuration { get; set; }
}

public class MyHealthReportEntry {
    public MyHealthReportEntry()
    {
        
    }

    private static readonly IReadOnlyDictionary<string, object> _emptyReadOnlyDicionary = new Dictionary<string, object>();
    public MyHealthReportEntry(HealthStatus status, string? description, TimeSpan duration, Exception? exception, IReadOnlyDictionary<string, object>? data):this(status, description, duration, exception,data, null)
    {
        
    }

    public MyHealthReportEntry(HealthStatus status, string? description, TimeSpan duration, Exception? exception, IReadOnlyDictionary<string, object>? data, IEnumerable<string>? tags = null) {
        Status = status;
        Description = description;
        Duration = duration;
        Exception = exception;
        Data = data ?? _emptyReadOnlyDicionary;
        Tags = tags ?? Enumerable.Empty<string>();
    }

    public IReadOnlyDictionary<string, object> Data { get; set; }
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public Exception? Exception { get; set; }
    public HealthStatus Status { get; set; }
    public IEnumerable<string> Tags { get; set; }
}


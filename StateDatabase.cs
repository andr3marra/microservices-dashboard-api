namespace microservices_dashboard_api {
    public class StateDatabase {
        public Dictionary<Guid, ServiceStatePersist> Services { get; set; }
    }

    public class ServiceStatePersist {
        public string Name { get; set; }
        public Uri Url { get; set; }
        //public List<string>? Metadata { get; set; }
        public Dictionary<string,string>? Aliases { get; set; }
    }
}

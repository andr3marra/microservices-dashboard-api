using System.Text.Json;

namespace microservices_dashboard_api {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            StateDatabase items;
            using (StreamReader r = new StreamReader("state.json")) {
                string json = r.ReadToEnd();
                items = JsonSerializer.Deserialize<StateDatabase>(json);
            }

            builder.Services.AddSingleton(items);
            builder.Services.AddHostedService<ServiceStateRepository>();

            builder.Services.AddHttpClient();
            builder.Services.AddSingleton(new Dictionary<Guid, ServiceState>());

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseCors(x => x.AllowAnyOrigin());

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment()) {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
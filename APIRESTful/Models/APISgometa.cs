namespace APIRESTful.Models
{
    public class APISgometa
    {
        public HttpClient Iniciar() { 
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://apis.gometa.org");
            return client;
        }
    }
}

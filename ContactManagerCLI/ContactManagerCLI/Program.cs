using System.Threading.Tasks;
using ContactManagerCLI.Repositories;
using ContactManagerCLI.Services;
using ContactManagerCLI.Core;

namespace ContactManagerCLI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var repository = new JsonContactRepository();
            var service = new ContactService(repository);
            var app = new ContactManagerApp(service);
            await app.Run();
        }
    }
}

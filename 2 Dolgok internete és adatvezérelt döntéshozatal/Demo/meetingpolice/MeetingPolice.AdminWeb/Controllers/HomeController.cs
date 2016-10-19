using Microsoft.Azure.Devices;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MeetingPolice.AdminWeb.Controllers
{
    public class HomeController : Controller
    {
        private const string AzureStorageConnectionString = "YOUR_KEY_HERE";
        private const string IotHubConnectionString = "YOUR_KEY_HERE";
        private const string DeviceId = "meetingpolice-01";

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Index(bool showWarning)
        {
            var messageString = showWarning ? "flashScreen" : "clearScreen";

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(IotHubConnectionString);
            var message = new Message(Encoding.UTF8.GetBytes(messageString));
            await serviceClient.SendAsync(DeviceId, message);

            ViewBag.Message = DateTime.Now.ToString("HH:mm:ss") + ": Command sent ('" + messageString + "')!";
            return View();
        }

        public ActionResult ClearTables()
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(AzureStorageConnectionString);
            var tableClient = cloudStorageAccount.CreateCloudTableClient();
            var rawTable = tableClient.GetTableReference("meetingpoliceraw");
            rawTable.DeleteIfExists();
            var processedTable = tableClient.GetTableReference("meetingpoliceprocessed");
            processedTable.DeleteIfExists();

            ViewBag.Message = "Tables deleted. Wait 1-2 minutes and restart streaming jobs.";
            return View("Index");
        }
    }
}
using CryptoMonitorGUI.Shared;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Authorization;

namespace CryptoMonitorGUI.Server.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
    public class ConfigController : Controller
    {
        private TableClient client;
        //todo: use a better form of secret management
        private const string connectionString = "DefaultEndpointsProtocol=https;AccountName=cryptomonitorskstorage;AccountKey=hWOah4JTmJWJgTYezega0n9oZ5wnZI2/skl4Nt1obkKCuUm4cAH8UuY/cAQRGRTg11MThj0QZiGfebxdC/1xuQ==;BlobEndpoint=https://cryptomonitorskstorage.blob.core.windows.net/;QueueEndpoint=https://cryptomonitorskstorage.queue.core.windows.net/;TableEndpoint=https://cryptomonitorskstorage.table.core.windows.net/;FileEndpoint=https://cryptomonitorskstorage.file.core.windows.net/;";

        public ConfigController()
        {
            this.client = new TableClient(connectionString, "SpotPrices");
        }


        [HttpGet]
        public async Task<ActionResult<CryptoConfig>> Get()
        {
            var config = await client.GetEntityAsync<TableEntity>("CryptoMonitor", "Config");
            return Ok(CryptoConfig.ToCryptoConfig(config));
        }

        [HttpPost]
        public async Task<ActionResult> Post(CryptoConfig config)
        {
            await client.UpsertEntityAsync(CryptoConfig.ToTableEntity(config));
            return Ok();
        }
    }
}

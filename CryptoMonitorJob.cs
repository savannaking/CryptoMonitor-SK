using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Linq;
using System.Collections.Generic;
using Coinbase;
using MimeKit;
using MailKit.Net.Smtp;
using MimeKit.Text;
using MailKit.Security;
using System.Threading.Tasks;


namespace CryptoMonitorJob
{
    public static class CryptoMonitorJob
    {
        [Function("CryptoMonitorJob")]
        public static async Task Run([TimerTrigger("0 */2 * * * *")] MyInfo myTimer, FunctionContext context)
        {
            var logger = context.GetLogger("CryptoMonitorJob");
            List<string> currencies = new List<string>() { "BTC", "ETH", "AMP", "MATIC", "DOGE", "XLM", "ETC", "GRT", "ALGO" };
            var client = new CoinbaseClient();
            var tableClient = new TableClient(
                new Uri("https://cryptomonitorskstorage.table.core.windows.net/"),
                "SpotPrices",
                new TableSharedKeyCredential("cryptomonitorskstorage", "hWOah4JTmJWJgTYezega0n9oZ5wnZI2/skl4Nt1obkKCuUm4cAH8UuY/cAQRGRTg11MThj0QZiGfebxdC/1xuQ=="));

            foreach (var currency in currencies)
            {
                var rowCount = tableClient.Query<TableEntity>(x => x.PartitionKey == currency && x.RowKey == "Count")
                    .SingleOrDefault()?.GetInt32("Count");
                var lastAlert = tableClient.Query<TableEntity>(x => x.PartitionKey == currency && x.RowKey == "LastAlert")
                    .SingleOrDefault()?.GetDateTimeOffset("Timestamp").Value.LocalDateTime;
                var currentPriceData = await client.Data.GetSpotPriceAsync($"{ currency }-USD");
                var currentPriceDouble = Convert.ToDouble(currentPriceData.Data.Amount);
                var previousPricesData = tableClient.Query<TableEntity>(x => x.PartitionKey == currency &&
                    x.RowKey != "Count" && x.RowKey != "LastAlert").Where(x => x.Timestamp.Value.LocalDateTime > DateTime.Now.AddHours(-1));

                if (previousPricesData != null)
                {
                    var previousPriceEntity = previousPricesData
                    .Where(x =>
                        currentPriceDouble > x.GetDouble("Price") * 1.05 ||
                        currentPriceDouble < x.GetDouble("Price") * .95)
                    .OrderByDescending(x => x.Timestamp.Value.LocalDateTime)
                    .FirstOrDefault();

                    if (previousPriceEntity != null && (lastAlert == null || lastAlert < DateTime.Now.AddHours(-1)))
                    {
                        var previousPrice = previousPriceEntity.GetDouble("Price");

                        logger.LogInformation($"{currency} previous price: {previousPrice}; current price: {currentPriceData.Data.Amount}");
                        if (currentPriceDouble > previousPrice)
                        {
                            //send alert price is high
                            Send($"{currency} price is up!", previousPriceEntity, currentPriceDouble, currency, logger);
                            await tableClient.UpsertEntityAsync(new TableEntity(currency, "LastAlert"));

                        }
                        else if (currentPriceDouble < previousPrice)
                        {
                            //send alert price is low
                            Send($"{currency} price is down!", previousPriceEntity, currentPriceDouble, currency, logger);
                            await tableClient.UpsertEntityAsync(new TableEntity(currency, "LastAlert"));
                        }
                    }
                }

                if (rowCount != null && rowCount <= 25000) rowCount++;
                else rowCount = 0;

                await tableClient.UpsertEntityAsync(new TableEntity(currency, rowCount.ToString()) {
                    { "Price",  currentPriceDouble }
                });

                //update row count
                await tableClient.UpsertEntityAsync(new TableEntity(currency, "Count") {
                    { "Count", rowCount }
                });
            }
        }


        public static void Send(string subject, TableEntity from, double to, string currency, ILogger logger)
        {
            logger.LogInformation("Attempting to send email");
            // create message
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse("cryptomonitor@noreply.com"));
            email.To.Add(MailboxAddress.Parse("jessmanman2020@gmail.com"));
            email.To.Add(MailboxAddress.Parse("savanna18king@gmail.com"));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html)
            {
                Text = $"{ currency }'s price has gone from { from.GetDouble("Price") } at { from.GetDateTimeOffset("Timestamp").Value.LocalDateTime } UTC to { to } at { DateTime.Now } UTC."
            };

            // send email
            using var smtp = new SmtpClient();
            smtp.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            smtp.Authenticate("jessmanman2020@gmail.com", "jpcehykxmyoomdkl");
            smtp.Send(email);
            smtp.Disconnect(true);

            logger.LogInformation("sent email");
        }

    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}

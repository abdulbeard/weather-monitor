using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using weather.monitor;

namespace Weather.Monitor
{
    public static class Main
    {
        [FunctionName("Weather-Monitor")]
        public static async Task Run([TimerTrigger("10 30 10,18 * * *")]TimerInfo myTimer, TraceWriter log, ExecutionContext executionContext)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            //log.Info($"message templateId: {message.TemplateId}");
            log.Info($"func invocation id: {executionContext.InvocationId}");
            log.Info($"storage conn string: {GetStorageConnectionString(executionContext)}");
            log.Info($"sendgrid api key: {GetSendGridApiKey(executionContext)}");

            //var requestMessage = new HttpRequestMessage();
            //requestMessage.Headers.Add("User-Agent", "c1b4a00f-606a-4923-a09a-e7e71a590e28");
            //requestMessage.RequestUri = new Uri($"https://api.weather.gov/gridpoints/GSP/61,68/forecast/hourly");
            //log.Info(await (await new HttpClient().SendAsync(requestMessage)).Content.ReadAsStringAsync());


            var subscriberRepository = new SubscriberRepository(GetTableClient(executionContext));
            var imageRepository = new ImageRepository(GetBlobClient(executionContext));
            //await CreateRandomSubscriber(subscriberRepository, log);
            var client = new HttpClient()
            {
                BaseAddress = new Uri("https://api.weather.gov")
            };
            var tasks = new List<Task<Response>>();
            try
            {
                var activeSubscribers = await subscriberRepository.GetActiveSubscribers();
                log.Info($"active subscribers: {JsonConvert.SerializeObject(activeSubscribers)}");

                foreach(var subscriber in activeSubscribers)
                {
                    tasks.Add(await GetForecast(client, subscriber.GridpointWfo, subscriber.GridpointWfoX, subscriber.GridpointWfoY).ContinueWith(x =>
                    {
                        return SendEmailAsync(x.Result, subscriber, executionContext, imageRepository, log);
                    }));                    
                }
                var result = await Task.WhenAll(tasks);
                log.Info($"results: {JsonConvert.SerializeObject(result)}");
            }
            catch(Exception e)
            {
                log.Info(e.ToString());
            }
        }

        public static async Task<Response> SendEmailAsync(ForecastResponse forecastResponse, SubscriberModel subscriber, ExecutionContext executionContext, ImageRepository imageRepository, TraceWriter log = null)
        {
            var forecastNumHours = 48;
            var periods = forecastResponse.properties.periods.Select(x => new DisplayPeriod(x)).ToList();
            if (periods.Take(forecastNumHours).Any(x => x.belowFreezing))
            {
                log?.Info($"Sending freeze-warning email to: {JsonConvert.SerializeObject(subscriber.Subscribers)} for {subscriber.GridpointWfo}/{subscriber.GridpointWfoX},{subscriber.GridpointWfoY}");
                var client = new SendGridClient(GetSendGridApiKey(executionContext));
                var name = "Subscriber";
                var totalForecastHours = periods.Count;

                var googleChartUrl = ChartUtils.GetGoogleChartUri(periods, forecastNumHours, log);
                var chartUrl = await GetBlobUrlFromGoogleChartUrl(imageRepository, googleChartUrl);
                var htmlTableUrl = await imageRepository.UploadHtmlTableResults(TableUtils.GetHtmlForecastResults(periods.ToList()), "text/html");
                //var imageData = ChartUtils.GetChartImageData(chartUrl);
                var payload = new
                {
                    periods,
                    name,
                    chartUrl,
                    htmlTableUrl,
                    forecastNumHours,
                    totalForecastHours
                };
                var emailMsg = CreateEmail(payload,
                                            "d-3d18bbae54914715b085ba3bbdc0628d",
                                            new List<EmailAddress> { new EmailAddress("abdulkhaliqzaheer@gmail.com", "akzgmail") },
                                            "Weather Forecast",
                                            new EmailAddress("Zaheer_The_Weatherman@dialmformystery.com"));
                subscriber.Subscribers.ForEach(x => emailMsg.Personalizations.Add(new Personalization()
                {
                    TemplateData = new { name = x.Name, periods, chartUrl, htmlTableUrl, forecastNumHours, totalForecastHours },
                    Tos = new List<EmailAddress> { new EmailAddress { Email = x.Email, Name = x.Name } },
                }));
                return await client.SendEmailAsync(emailMsg);
            }
            else
            {
                log?.Info($"Not gonna be below freezing in the next {forecastNumHours} hrs for subsriber: {subscriber.GridpointWfo}/{subscriber.GridpointWfoX},{subscriber.GridpointWfoY}");
                return new Response(System.Net.HttpStatusCode.SeeOther, null, null);
            }
        }

        public static async Task<string> GetBlobUrlFromGoogleChartUrl(ImageRepository imageRepository, string googleChartUrl)
        {
            var stream = new MemoryStream();
            var googleHttpResponse = await new HttpClient().GetAsync(googleChartUrl);
            await googleHttpResponse.Content.CopyToAsync(stream);
            var blobUrl = await imageRepository.UploadImage(stream, $"{googleHttpResponse.Content.Headers.ContentType}");
            return blobUrl;
        }

        public static async Task<ForecastResponse> GetForecast(HttpClient client, string wfo, string x, string y)
        {
            var requestMessage = new HttpRequestMessage();
            requestMessage.Headers.Add("User-Agent", "c1b4a00f-606a-4923-a09a-e7e71a590e28");
            requestMessage.RequestUri = new Uri($"{client.BaseAddress.OriginalString}/gridpoints/{wfo}/{x},{y}/forecast/hourly");
            return await (await client.SendAsync(requestMessage).ContinueWith(async task =>
            {
                var stringContent = await task.Result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ForecastResponse>(stringContent);
            }));
        }

        public static IConfigurationRoot GetConfiguration(ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
//                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            return config;
        }

        public static string GetStorageConnectionString(ExecutionContext context)
        {
            var config = GetConfiguration(context);
            return config.GetConnectionString("StorageAppsetting");
        }

        public static string GetSendGridApiKey(ExecutionContext context)
        {
            var config = GetConfiguration(context);
            return config.GetConnectionString("SendGridApiKey");
        }

        private static CloudBlobClient GetBlobClient(ExecutionContext context)
        {
            var tableStorageConnString = GetStorageConnectionString(context);
            var storageAccount = CloudStorageAccount.Parse(tableStorageConnString);
            return storageAccount.CreateCloudBlobClient();
        }

        public static CloudTableClient GetTableClient(ExecutionContext context)
        {
            var tableStorageConnString = GetStorageConnectionString(context);
            var storageAccount = CloudStorageAccount.Parse(tableStorageConnString);
            return storageAccount.CreateCloudTableClient();
        }

        public static async Task CreateRandomSubscriber(SubscriberRepository subscriberRepository, TraceWriter log)
        {
            var subscriber = await subscriberRepository.AddSubscriber(new SubscriberModel()
            {
                Active = true,
                GridpointWfo = "GSP",
                GridpointWfoX = "61",
                GridpointWfoY = "68",
                Subscribers = new List<Subscriber>
                {
                    new Subscriber()
                    {
                        Email = "abdulkhaliqzaheer@hotmail.com",
                        Name = "Pam And Brent"
                    },
                    new Subscriber()
                    {
                        Email = "abdulkhaliqzaheer@gmail.com",
                        Name = "Akz"
                    },
                }
            });
            var subscriber1 = await subscriberRepository.AddSubscriber(new SubscriberModel()
            {
                Active = true,
                GridpointWfo = "HFO",
                GridpointWfoX = "153",
                GridpointWfoY = "144",
                Subscribers = new List<Subscriber>
                {
                    new Subscriber()
                    {
                        Email = "homunculo.genio@gmail.com",
                        Name = "Who and hu?"
                    }
                }
            });
            log.Info($"subscriber: ${JsonConvert.SerializeObject(subscriber)}");
            log.Info($"subscriber: ${JsonConvert.SerializeObject(subscriber1)}");
        }

        public static SendGridMessage CreateEmail(object templateData, string templateId, List<EmailAddress> to, string subject, EmailAddress from)
        {
            var msg = new SendGridMessage();

            //msg.SetFrom(new EmailAddress("abdul@dialmformystery.com", "Abdul The Mystery Man"));
            msg.SetFrom(from);

            //var recipients = new List<EmailAddress>
            //{
            //    new EmailAddress("abdulmacchiato@gmail.com", "The_Real_Boss_AM"),
            //    new EmailAddress("homunculo.genio@gmail.com", "The_Real_Boss_HG"),
            //    new EmailAddress("abdulkhaliqzaheer@gmail.com", "The_Real_Boss_AKZ")
            //};
            //msg.AddTos(recipients);
            msg.AddTos(to);

            //msg.SetSubject("Testing the SendGrid C# Library");
            msg.SetSubject(subject);

            //msg.AddContent(MimeType.Text, "Tryna authenticate with the domain as well");
            //msg.AddContent(MimeType.Html, "<p>Hello World! <b>sending in bold too mayunh!</b> and domain auth-ed email preeze</p>");

            //msg.TemplateId = "d-3a7f067a3fa44f31b931cb001ac760e9";
            msg.TemplateId = templateId;

            msg.SetTemplateData(templateData);

            //msg.SetTemplateData(new
            //{
            //    Sender_Name = "asdf",
            //    Sender_Address = "864 Spring St NW",
            //    Sender_City = "Atlanta",
            //    Sender_State = "GA",
            //    Sender_Zip = "30308",
            //    Items = new List<object> {
            //        new
            //        {
            //            Name= "Primingham Manor",
            //            Price= "$7.66",
            //            Quantity= 4,
            //            Total= "$30.64"
            //        },
            //        new
            //        {
            //            Name= "Charla's Adventures",
            //            Price= "$3.50",
            //            Quantity= 2,
            //            Total= "$7.00"
            //        },
            //        new
            //        {
            //            Name= "Zahela Wedding Book",
            //            Price= "$7.75",
            //            Quantity= 1,
            //            Total= "$7.75"
            //        }
            //    }
            //});
            //msg.AddSubstitutions(new Dictionary<string, string>
            //{
            //    { "Sender_Name", "asdfk"},
            //    { "Sender_Address", "864 Spring St NW"},
            //    { "Sender_City", "Atlanta"},
            //    { "Sender_State", "GA"},
            //    { "Sender_Zip", "30308"}
            //});
            msg.SetClickTracking(true, false);
            msg.SetOpenTracking(true);
            return msg;
        }
    }
}

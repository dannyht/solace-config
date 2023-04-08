using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Text;

namespace SolaceSEMPExample
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostContext, configBuilder) =>
                {
                    configBuilder
                        .SetBasePath(System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..")) // Set base path to the root of the solution
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                });

            var host = builder.Build();
            var config = host.Services.GetRequiredService<IConfiguration>();
            var configSection = config.GetSection("ConfigurationStrings");

            var baseUrl = configSection["BaseUrl"];
            var username = configSection["Username"];
            var password = configSection["Password"];
            var msgVpnName = configSection["VpnName"];
            var requestQueueName = configSection["RequestQueueName"];
            var replyQueueName = configSection["ReplyQueueName"];
            var topicName = configSection["TopicName"];

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));

            var vpnPayload = new
            {
                msgVpnName,
                enabled = true,
                authenticationBasicEnabled = true,
                authenticationBasicProfileName = "default",
                maxMsgSpoolUsage = 100,
                maxConnectionCount = 100,
                maxSubscriptionCount = 1000,
                maxIngressFlowCount = 100,
                maxEgressFlowCount = 100,
            };

            var vpnContent = new StringContent(JsonConvert.SerializeObject(vpnPayload), Encoding.UTF8, "application/json");
            var vpnResponse = await client.PostAsync($"{baseUrl}/msgVpns", vpnContent);

            if (vpnResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("VPN created successfully");
            }
            else
            {
                Console.WriteLine("Error creating VPN:");
                Console.WriteLine(await vpnResponse.Content.ReadAsStringAsync());
            }

            var requestQueuePayload = new
            {
                queueName = requestQueueName,
                accessType = "exclusive",
                permission = "consume",
                maxMsgSize = 100,
                rejectMsgToSenderOnDiscardBehavior = "always",
            };

            var requestQueueContent = new StringContent(JsonConvert.SerializeObject(requestQueuePayload), Encoding.UTF8, "application/json");
            var requestQueueResponse = await client.PostAsync($"{baseUrl}/msgVpns/{msgVpnName}/queues", requestQueueContent);

            if (requestQueueResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Request Queue created successfully");
            }
            else
            {
                Console.WriteLine("Error creating Request Queue:");
                Console.WriteLine(await requestQueueResponse.Content.ReadAsStringAsync());
            }

            var replyQueuePayload = new
            {
                queueName = replyQueueName,
                accessType = "exclusive",
                permission = "consume",
                maxMsgSize = 100,
                rejectMsgToSenderOnDiscardBehavior = "always",
            };

            var replyQueueContent = new StringContent(JsonConvert.SerializeObject(replyQueuePayload), Encoding.UTF8, "application/json");
            var replyQueueResponse = await client.PostAsync($"{baseUrl}/msgVpns/{msgVpnName}/queues", replyQueueContent);

            if (replyQueueResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Reply Queue created successfully");
            }
            else
            {
                Console.WriteLine("Error creating Reply Queue:");
                Console.WriteLine(await replyQueueResponse.Content.ReadAsStringAsync());
            }

            List<string> queueNames = new() { requestQueueName, replyQueueName };

            foreach (var queueName in queueNames)
            {
                var subscriptionQueuePayload = new
                {
                    msgVpnName,
                    queueName,
                    subscriptionTopic = topicName,
                };

                var subscriptionContent = new StringContent(JsonConvert.SerializeObject(subscriptionQueuePayload), Encoding.UTF8, "application/json");

                var subscriptionResponse = await client.PostAsync($"{baseUrl}/msgVpns/{subscriptionQueuePayload.msgVpnName}/queues/{subscriptionQueuePayload.queueName}/subscriptions", subscriptionContent);

                if (subscriptionResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine("Subscription created successfully.");
                }
                else
                {
                    Console.WriteLine("Error creating subscription:");
                    Console.WriteLine(await subscriptionResponse.Content.ReadAsStringAsync());
                }
            }
        }
    }
}

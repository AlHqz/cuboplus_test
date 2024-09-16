using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace cuboplus_tech_test
{
    class tech_test
    {
        //base link and class instantiation to manage http requests, as well as the address of El Salvador's wallet
        private static HttpClient client = new HttpClient();
        static string address = "32ixEdVJWo3kmvJGMTZq5jAQVZZeuwnqzo";
        static string baseUrl = $"https://mempool.space/api/address/{address}";
        //Displays the given address's current sat balance both OnChain and MemPool
        static async Task Main(string[] args)
        {
            //Controlling exception
            try
            {
                //Start request, save the response as a text and parse it into a JsonDoc
                HttpResponseMessage response = await client.GetAsync(baseUrl);
                string responseTxt = await response.Content.ReadAsStringAsync();
                JsonDocument data = JsonDocument.Parse(responseTxt);
                //Get the amount of sats directly from the parsed
                double satsOnChain = double.Parse(data.RootElement.GetProperty("chain_stats").GetProperty("funded_txo_sum").ToString());
                double satsMemPool = double.Parse(data.RootElement.GetProperty("mempool_stats").GetProperty("funded_txo_sum").ToString());
                //Display the amount of sats both in satoshis and BTC
                Console.WriteLine($"Current On-Chain Balance: {satsOnChain:n0} sats, or {satsOnChain/100000000:n} BTC.");
                Console.WriteLine($"Current MemPool Balance: {satsMemPool:n0} sats, or {satsMemPool/100000000:n5} BTC.");
                //Calls the method to calculate the variation of both periods
                await CalculateVariation(baseUrl, 7);
                await CalculateVariation(baseUrl, 30);
            }
            catch (Exception e)
            {
                //Displays the exception's message in case there's one
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        //Calculates and displays the balance variation for the given period
        static async Task CalculateVariation(string url, int days)
        {
            //Initialize and/or modify the needed variables
            string urlTxs = url + "/txs";
            double variation = 0;
            DateTime dtToday = DateTime.UtcNow;
            DateTime dtNDaysAgo = dtToday.AddDays(-days);
            //Makes a call to MemPool's API
            using (HttpResponseMessage response = await client.GetAsync(urlTxs))
            {
                //Saves the message received from the API and parses the data into a JsonDocument
                string responseText = await response.Content.ReadAsStringAsync();
                JsonDocument txs = JsonDocument.Parse(responseText);
                //Navigates the array of JsonElements (tx) in the parsed document
                foreach (JsonElement tx in txs.RootElement.EnumerateArray())
                {
                    //Validates that the transaction is confirmed
                    if (tx.GetProperty("status").GetProperty("confirmed").GetBoolean() == true)
                    {
                        //Converts block_time property from UnixTime to usable DateTime
                        DateTime blockTime = DateTimeOffset.FromUnixTimeSeconds(tx.GetProperty("status").GetProperty("block_time").GetInt64()).DateTime;
                        //Validates that the block was created after the reference date (e.g 7 or 30 days ago)
                        if (blockTime >= dtNDaysAgo)
                        {
                            //Navigates the array of JsonElements inside the property vout in each tx
                            foreach (JsonElement element in tx.GetProperty("vout").EnumerateArray())
                            {
                                //Validates that the receiving address is our given address
                                if (element.GetProperty("scriptpubkey_address").ToString() == address)
                                {
                                    //If so, the variation's value is increased by the amount stored in the property value
                                    variation += element.GetProperty("value").GetInt32();
                                }
                            }
                        }
                    }
                }
            }
            //Displays all the information and returns to the next instruction on the Main method
            Console.WriteLine($"{days} days variaton: {variation:n0} satoshis or {variation / 100000000:n3} BTC.");
        }
    }
}
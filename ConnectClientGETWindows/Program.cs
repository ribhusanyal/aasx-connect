﻿using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Security.Cryptography;
using System.IO;
using System.Threading.Tasks;

/*
Copyright(c) 2020 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
AASX Connect is licensed under the Apache License 2.0 (Apache-2.0, see below).
*/

namespace TestConnect
{
    static class Connect
    {
        // TransmitData collects payloads to publish or subscribe
        // this is all the data put together by one node
        public class TransmitData
        {
            public string source;
            public string destination;
            public string type;
            public string encrypt;
            public string extensions;
            public List<string> publish;
            public TransmitData()
            {
                publish = new List<string> { };
            }
        }

        // TransmitFrame collects all TransmitData to publish or subscribe
        // this is the data collected by one node itself together with the data from other nodes forwareded
        public class TransmitFrame
        {
            public string source;
            public List<TransmitData> data;
            public TransmitFrame()
            {
                data = new List<TransmitData> { };
            }
        }

        // static string connectDomain = "http://localhost:52000"; // connect to AAS Connect on localhost
        static string connectDomain = "http://h2841345.stratoserver.net:52000"; // connect to AAS Connect on external strato testserver
        static string sourceName = "TestConnect"; // e.g. use your unique email address, random will be append below

        static int count = 0;
        static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        static string testRequest = "";

        public static string ContentToString(this HttpContent httpContent)
        {
            var readAsStringAsync = httpContent.ReadAsStringAsync();
            return readAsStringAsync.Result;
        }

        public static async void ThreadLoop()
        {
            while (true)
            {
                // Test data to publish
                string testPublish = "";
                if (testRequest == "")
                {
                    testPublish = "{ \"source\" : \"" + sourceName + "\" , \"count\" : \"" + count + "\" }";
                    count++;
                }
                else
                {
                    testPublish = await getTestData(testRequest);
                }

                TransmitData td = new TransmitData();
                td.source = sourceName; // must be a unique name in the overall node network
                td.type = "test"; // AASX Server publishes type "submodel"
                td.publish.Add(testPublish);

                TransmitFrame tf = new TransmitFrame();
                tf.source = sourceName;
                tf.data.Add(td);

                string publish = JsonConvert.SerializeObject(tf, Formatting.Indented);
                Console.WriteLine("Publish data:\n" + publish + "\n");

                var handler = new HttpClientHandler();
                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                var client = new HttpClient(handler);

                string content = "";
                try
                {
                    var contentJson = new StringContent(publish, System.Text.Encoding.UTF8, "application/json");
                    var result = client.PostAsync(connectDomain + "/publish", contentJson).Result;
                    content = ContentToString(result.Content);
                }
                catch
                {
                    Console.WriteLine("ERROR: Publish Timeout!\n");
                }

                if (content != "")
                {
                    // Received JSON content as TransmitFrame
                    Console.WriteLine("Received Subscribe Data:\n" + content + "\n");
                }

                Thread.Sleep(3000);
            }
        }

        static void Main(string[] args)
        {
            if (!File.Exists("REQUEST.DAT"))
            {
                Console.WriteLine("REQUEST.DAT not found!");
                Console.ReadLine();
                return;
            }

            try
            {
                using (StreamReader sr = new StreamReader("REQUEST.DAT"))
                {
                    connectDomain = sr.ReadLine();
                    sourceName = sr.ReadLine();
                    testRequest = sr.ReadLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("REQUEST.DAT can not be read!");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("connectDomain: " + connectDomain);
            Console.WriteLine("nodeName: " + sourceName);
            Console.WriteLine("testRequest: " + testRequest);
            Console.WriteLine("Press key to continue!");
            Console.ReadLine();

            // Start 3s thread loop
            Thread t = new Thread(new ThreadStart(ThreadLoop));
            t.Start();

            // Wait for CTRL-C
            Console.WriteLine("Press CTRL-C to STOPP");

            Console.ReadLine();
        }

        static async Task<string> getTestData(string request)
        {
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            var client = new HttpClient(handler);

            string content = "";
            try
            {
                // var result = client.GetAsync(request).Wait();
                // content = result + "";
                content = await client.GetStringAsync(request);
            }
            catch
            {
                Console.WriteLine("ERROR: Publish Timeout!\n");
            }

            return content;
        }
    }
}

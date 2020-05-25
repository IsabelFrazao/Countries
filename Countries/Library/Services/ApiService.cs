using Library;
using Library.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ServiceStack.Auth;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Services
{
    public class ApiService
    {
        /// <summary>
        /// Makes an API Call to Get the Countries' Information and insert it into a List. 
        /// The call is made in the First Initialization and with every new Update Request.
        /// </summary>
        /// <param name="urlBase"></param>
        /// <param name="controller"></param>
        /// <param name="progress"></param>
        /// <returns>Task</returns>
        public async Task<Response> GetCountries(string urlBase, string controller, IProgress<ProgressReport> progress)
        {
            ProgressReport report = new ProgressReport();

            try
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(urlBase)
                };

                var response = await client.GetAsync(controller);

                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new Response
                    {
                        IsSuccess = false,
                        Message = result
                    };
                }

                var countries = JsonConvert.DeserializeObject<List<Country>>(result);

                report.SaveCountries = countries;
                report.PercentageComplete = (report.SaveCountries.Count * 100) / countries.Count;
                progress.Report(report);

                return new Response
                {
                    IsSuccess = true,
                    Result = countries
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Makes an API Call to Get the Inserted Text Translation and display it. 
        /// The call is made with every new Inserted Text Translation Request.
        /// </summary>
        /// <param name="urlBase"></param>
        /// <param name="controller"></param>
        /// <returns>Task</returns>
        public async Task<Response> GetTranslation(string urlBase, string controller)
        {
            try
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(urlBase)
                };

                HttpRequestMessage request = new HttpRequestMessage();
                request.RequestUri = new Uri(urlBase + controller);
                request.Method = HttpMethod.Get;
                request.Headers.Add("X-RapidAPI-Key", "337f620cb7msh7dc6d59fdb87e83p16d1adjsn0a4bf90f6810");

                HttpResponseMessage response = await client.SendAsync(request);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new Response
                    {
                        IsSuccess = false,
                        Message = result
                    };
                }

                var output = result.Split('"')[5];

                return new Response
                {
                    IsSuccess = true,
                    Result = output
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Makes an API Call to Get the Countries' Information and insert it into a List.
        /// The call is made in the First Initialization and with every new Update Request.
        /// </summary>
        /// <param name="urlBase"></param>
        /// <param name="controller"></param>
        /// <param name="progress"></param>
        /// <returns>Task</returns>
        public async Task<Response> GetRates(string urlBase, string controller, IProgress<ProgressReport> progress)
        {
            ProgressReport report = new ProgressReport();

            try
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(urlBase)
                };

                var response = await client.GetAsync(controller);

                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new Response
                    {
                        IsSuccess = false,
                        Message = result
                    };
                }

                var rates = JsonConvert.DeserializeObject<List<Rate>>(result);

                report.SaveRates = rates;
                report.PercentageComplete = (report.SaveRates.Count * 100) / rates.Count;
                progress.Report(report);

                return new Response
                {
                    IsSuccess = true,
                    Result = rates
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Makes an API Call to Get the Countries' Information Text. 
        /// The call is made in the First Initialization and with every new Update Request.
        /// </summary>
        /// <param name="urlBase"></param>
        /// <param name="controller"></param>
        /// <param name="countryName"></param>
        /// <returns>Task</returns>
        public async Task<Response> GetWikiText(string urlBase, string controller, string alpha2Code)
        {
            try
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(urlBase)
                };

                var response = await client.GetAsync(controller);

                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new Response
                    {
                        IsSuccess = false,
                        Message = result
                    };
                }

                string[] parts = result.Split(new string[] { "&lt;/p&gt;" }, StringSplitOptions.None); //Split the string by paragraphs (closing paragraph tag)

                var output = string.Empty;

                if (parts[1].Contains(alpha2Code))
                    output = parts[1];
                else
                    output = parts[2];

                output = Regex.Replace(output, @"(&lt;[\s\S]+?&gt;)", string.Empty); //Remove Tags from the XML

                output = output.Replace("listen", string.Empty);

                return new Response
                {
                    IsSuccess = true,
                    Result = output
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
    }
}

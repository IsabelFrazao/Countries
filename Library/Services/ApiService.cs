using Library;
using Library.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RestSharp;
using ServiceStack.Auth;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Services
{
    public class ApiService //Trabalhar com a Api - CRUD
    {
        public async Task<Response> GetCountries(string urlBase, string controller)
        {
            try
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(urlBase)//Onde está o endereço base da API
                }; //Criar um Http para fazer a ligação externa via http

                var response = await client.GetAsync(controller);//Onde está o Controlador da API

                var result = await response.Content.ReadAsStringAsync();//Carregar os resultados em forma de string para dentro do result

                if (!response.IsSuccessStatusCode)
                {
                    return new Response
                    {
                        IsSuccess = false,
                        Message = result
                    };
                }

                var countries = JsonConvert.DeserializeObject<List<Country>>(result);
                
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

        public async Task<Response> GetTranslation(string urlBase, string controller)
        {
            try
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(urlBase)//Onde está o endereço base da API
                }; //Criar um Http para fazer a ligação externa via http

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
                //result = {"outputs":[{"output":"good bye","stats":{"elapsed_time":19,"nb_characters":5,"nb_tokens":1,"nb_tus":1,"nb_tus_failed":0}}]}

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

        public async Task<Response> GetRates(string urlBase, string controller)
        {
            try
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(urlBase)//Onde está o endereço base da API
                }; //Criar um Http para fazer a ligação externa via http

                var response = await client.GetAsync(controller);//Onde está o Controlador da API

                var result = await response.Content.ReadAsStringAsync();//Carregar os resultados em forma de string para dentro do result

                if (!response.IsSuccessStatusCode)
                {
                    return new Response
                    {
                        IsSuccess = false,
                        Message = result
                    };
                }

                var rates = JsonConvert.DeserializeObject<List<Rate>>(result);

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
    }
}

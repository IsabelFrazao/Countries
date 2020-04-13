using Library;
using Library.Models;
using Newtonsoft.Json;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
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

                foreach (var ct in countries)
                {
                    string flagNameAbrev = ct.Flag.Split('/')[4];
                    await GetFlags("https://restcountries.eu", $"/data/{flagNameAbrev}");
                }

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
        public async Task<Response> GetFlags(string urlBase, string controller)
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

                //Se a pasta Flags não existir, criar
                if (!Directory.Exists("Flags"))
                {
                    Directory.CreateDirectory("Flags");
                }

                //Criar um caminho dentro da pasta Flags para gravar a bandeira com o nome abreviado de cada país
                string p = controller.Split('/')[2];//Separar a string do Controller para extrair apenas o texto a seguir à última barra (/)
                var path = @"Flags\" + $"{p}.svg";//Caminho para gravar a imagem da bandeira em SVG

                //Gravar a imagem em SVG proveniente da Api
                string svgFileName = $"{urlBase}" + $"{controller}";
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(svgFileName, path);
                }

                //Abrir a imagem SVG e gravar na mesma pasta Flags em JPEG
                string p2 = p.Split('.')[0];//Separar a string do path para extrair apenas o texto antes do ponto (.)
                var path2 = @"Flags\" + $"{p2}.jpg";//Caminho para gravar a imagem em JPEG

                //read svg document from file system
                var svgDocument = SvgDocument.Open(path);
                var bitmap = svgDocument.Draw();
                //save converted svg to file system                 
                if (!File.Exists(path2))
                {
                    bitmap.Save(path2, ImageFormat.Jpeg);
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return new Response
                {
                    IsSuccess = true,
                    Result = path2
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

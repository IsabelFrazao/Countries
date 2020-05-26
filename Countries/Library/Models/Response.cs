using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library
{
    public class Response //Respostas pré-definidas
    {
        public bool IsSuccess { get; set; } //Testa se tudo correu bem

        public string Message { get; set; } //Se houver erro, saber qual foi o erro

        public object Result { get; set; } //object = qualquer objecto. Se tudo correr bem devolve um objecto independentemente do tipo
    }
}

﻿namespace Sopra.Responses
{
    public class Response<T>
    {
        public T Data { get; set; }
        public Response() { }
        public Response(T data)
        {
            Data = data;
        }
    }
}



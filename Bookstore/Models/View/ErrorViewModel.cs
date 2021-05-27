using System;

namespace Bookstore.Models.View
{
    public class ErrorViewModel
    {
        public ErrorViewModel(string message, string controller, string action)
        {
            Message = message;
            Controller = controller;
            Action = action;
        }
        
        public string Message { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
    }
}